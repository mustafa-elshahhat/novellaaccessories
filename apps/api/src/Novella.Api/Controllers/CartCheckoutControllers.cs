using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novella.Api.Auth;
using Novella.Application.Abstractions;
using Novella.Application.Cart;
using Novella.Application.Checkout;
using Novella.Application.Orders;

namespace Novella.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Policy = "Customer")]
public sealed class CartController : ControllerBase
{
    private readonly CartService _cart;
    private readonly ICurrentUser _user;

    public CartController(CartService cart, ICurrentUser user)
    {
        _cart = cart;
        _user = user;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _cart.GetCartAsync(_user.RequireCustomerId(), ct));

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest req, CancellationToken ct)
        => Ok(await _cart.AddItemAsync(_user.RequireCustomerId(), req, ct));

    [HttpPatch("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateCartItemRequest req, CancellationToken ct)
        => Ok(await _cart.UpdateItemAsync(_user.RequireCustomerId(), itemId, req, ct));

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken ct)
        => Ok(await _cart.RemoveItemAsync(_user.RequireCustomerId(), itemId, ct));

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct) => Ok(await _cart.ClearAsync(_user.RequireCustomerId(), ct));

    [HttpPost("reprice")]
    public async Task<IActionResult> Reprice(CancellationToken ct) => Ok(await _cart.RepriceAsync(_user.RequireCustomerId(), ct));
}

[ApiController]
[Route("api/checkout")]
[Authorize(Policy = "Customer")]
public sealed class CheckoutController : ControllerBase
{
    private readonly CheckoutService _checkout;
    private readonly ICurrentUser _user;

    public CheckoutController(CheckoutService checkout, ICurrentUser user)
    {
        _checkout = checkout;
        _user = user;
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] CheckoutPreviewRequest req, CancellationToken ct)
        => Ok(await _checkout.PreviewAsync(_user.RequireCustomerId(), req, ct));
}

[ApiController]
[Route("api/orders")]
[Authorize(Policy = "Customer")]
public sealed class OrdersController : ControllerBase
{
    private readonly CheckoutService _checkout;
    private readonly OrderService _orders;
    private readonly ICurrentUser _user;

    public OrdersController(CheckoutService checkout, OrderService orders, ICurrentUser user)
    {
        _checkout = checkout;
        _orders = orders;
        _user = user;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req, CancellationToken ct)
    {
        var result = await _checkout.CreateOrderAsync(_user.RequireCustomerId(), req, ct);
        return Ok(new { orderId = result.OrderId, orderNumber = result.OrderNumber });
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyOrders(CancellationToken ct)
        => Ok(await _orders.GetMyOrdersAsync(_user.RequireCustomerId(), ct));

    [HttpGet("my/{orderNumber}")]
    public async Task<IActionResult> MyOrder(string orderNumber, CancellationToken ct)
        => Ok(await _orders.GetMyOrderAsync(_user.RequireCustomerId(), orderNumber, ct));

    [HttpPost("my/{orderNumber}/cancel")]
    public async Task<IActionResult> CancelMyOrder(string orderNumber, [FromBody] CancelOrderRequest req, CancellationToken ct)
        => Ok(await _orders.CancelMyOrderAsync(_user.RequireCustomerId(), orderNumber, req, ct));
}
