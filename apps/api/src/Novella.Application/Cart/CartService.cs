using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Checkout;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Services;

namespace Novella.Application.Cart;

/// <summary>
/// Customer cart. Only active, purchasable variants may be added; quantity is capped to available
/// stock. Prices are always recomputed by the backend — client prices are never trusted.
/// </summary>
public sealed class CartService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly PricingAssembler _assembler;

    public CartService(IAppDbContext db, IClock clock, PricingAssembler assembler)
    {
        _db = db;
        _clock = clock;
        _assembler = assembler;
    }

    public async Task<CartDto> GetCartAsync(Guid customerId, CancellationToken ct)
    {
        var cart = await LoadCartAsync(customerId, ct);
        return await BuildDtoAsync(cart, ct);
    }

    public async Task<CartDto> AddItemAsync(Guid customerId, AddCartItemRequest req, CancellationToken ct)
    {
        if (req.Quantity < 1) throw AppException.Validation("Quantity must be at least 1.");

        var cart = await LoadCartAsync(customerId, ct);
        var variant = await _db.ProductVariants.Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == req.ProductVariantId, ct)
            ?? throw AppException.NotFound("Product variant not found.");

        if (variant.Product is null || !variant.Product.IsActive || !variant.IsActive || variant.StockQuantity <= 0)
            throw new AppException(ErrorCodes.ProductUnavailable, "Product is unavailable.", 409);

        var item = cart.Items.FirstOrDefault(i => i.ProductVariantId == req.ProductVariantId);
        var desired = (item?.Quantity ?? 0) + req.Quantity;
        if (desired > variant.StockQuantity)
            throw new AppException(ErrorCodes.VariantOutOfStock, "Requested quantity exceeds available stock.", 409);

        if (item is null)
        {
            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = variant.ProductId,
                ProductVariantId = variant.Id,
                Quantity = req.Quantity,
                CreatedAt = _clock.UtcNow
            };
            // Add to the DbSet so EF tracks it as Added (a client-set Guid key on a navigation-only
            // add would otherwise be treated as Modified). Relationship fixup populates cart.Items.
            _db.CartItems.Add(newItem);
        }
        else
        {
            item.Quantity = desired;
            item.UpdatedAt = _clock.UtcNow;
        }
        cart.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await BuildDtoAsync(cart, ct);
    }

    public async Task<CartDto> UpdateItemAsync(Guid customerId, Guid itemId, UpdateCartItemRequest req, CancellationToken ct)
    {
        if (req.Quantity < 1) throw AppException.Validation("Quantity must be at least 1.");
        var cart = await LoadCartAsync(customerId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId) ?? throw AppException.NotFound("Cart item not found.");

        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, ct)
            ?? throw AppException.NotFound("Product variant not found.");
        if (req.Quantity > variant.StockQuantity)
            throw new AppException(ErrorCodes.VariantOutOfStock, "Requested quantity exceeds available stock.", 409);

        item.Quantity = req.Quantity;
        item.UpdatedAt = _clock.UtcNow;
        cart.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await BuildDtoAsync(cart, ct);
    }

    public async Task<CartDto> RemoveItemAsync(Guid customerId, Guid itemId, CancellationToken ct)
    {
        var cart = await LoadCartAsync(customerId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId) ?? throw AppException.NotFound("Cart item not found.");
        _db.CartItems.Remove(item);
        cart.Items.Remove(item);
        cart.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await BuildDtoAsync(cart, ct);
    }

    public async Task<CartDto> ClearAsync(Guid customerId, CancellationToken ct)
    {
        var cart = await LoadCartAsync(customerId, ct);
        _db.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await BuildDtoAsync(cart, ct);
    }

    /// <summary>Re-prices the cart against current catalog state, clamping over-stock quantities.</summary>
    public async Task<CartDto> RepriceAsync(Guid customerId, CancellationToken ct)
    {
        var cart = await LoadCartAsync(customerId, ct);
        var changed = false;
        foreach (var item in cart.Items.ToList())
        {
            var variant = await _db.ProductVariants.Include(v => v.Product).FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, ct);
            if (variant is null) continue;
            var maxQty = (variant.Product?.IsActive == true && variant.IsActive) ? variant.StockQuantity : 0;
            if (maxQty <= 0) continue; // keep but flagged unavailable in DTO
            if (item.Quantity > maxQty)
            {
                item.Quantity = maxQty;
                item.UpdatedAt = _clock.UtcNow;
                changed = true;
            }
        }
        if (changed)
        {
            cart.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return await BuildDtoAsync(cart, ct);
    }

    private async Task<Domain.Entities.Cart> LoadCartAsync(Guid customerId, CancellationToken ct)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        if (cart is null)
        {
            cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), CustomerId = customerId, CreatedAt = _clock.UtcNow };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync(ct);
        }
        return cart;
    }

    private async Task<CartDto> BuildDtoAsync(Domain.Entities.Cart cart, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        if (cart.Items.Count == 0)
            return new CartDto(cart.Id, Array.Empty<CartItemDto>(), 0, 0, 0, false);

        var lineRequests = cart.Items.Select(i => new LineRequest(i.ProductVariantId, i.Quantity)).ToList();
        var resolved = await _assembler.ResolveAsync(lineRequests, strict: false, ct);

        var items = new List<CartItemDto>(resolved.Count);
        decimal subtotalBefore = 0, subtotalAfter = 0;

        foreach (var r in resolved)
        {
            var item = cart.Items.First(i => i.ProductVariantId == r.Variant.Id);
            var original = PricingAssembler.CatalogPrice(r.Product, r.Variant);
            var final = CatalogProjectionPrice(r, now, original);
            var qty = r.EffectiveQuantity > 0 ? r.EffectiveQuantity : r.RequestedQuantity;

            if (r.Available)
            {
                subtotalBefore += original * r.EffectiveQuantity;
                subtotalAfter += final * r.EffectiveQuantity;
            }

            items.Add(new CartItemDto(
                item.Id, r.Product.Id, r.Variant.Id,
                r.Product.NameAr, r.Product.NameEn, r.Variant.NameAr, r.Variant.NameEn, r.Variant.Sku,
                qty, original, final, final * qty,
                r.Available, r.QuantityAdjusted));
        }

        return new CartDto(cart.Id, items,
            PricingCalculator.Round(subtotalBefore),
            PricingCalculator.Round(subtotalBefore - subtotalAfter),
            PricingCalculator.Round(subtotalAfter),
            items.Any(i => !i.IsAvailable));
    }

    private static decimal CatalogProjectionPrice(ResolvedLine r, DateTime now, decimal original)
    {
        if (!PricingCalculator.IsProductDiscountActive(r.Product.ProductDiscountPercentage,
                r.Product.ProductDiscountStartAt, r.Product.ProductDiscountEndAt, now))
            return original;
        var discount = PricingCalculator.Round(original * r.Product.ProductDiscountPercentage!.Value / 100m);
        var final = original - discount;
        return final < 0 ? 0 : final;
    }
}
