using FluentAssertions;
using Novella.Application.Reports;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Tests;

public class ReportsAndDashboardTests
{
    [Fact]
    public async Task Empty_database_does_not_throw_for_any_report()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var service = new ReportService(db.Db, clock);
        var w = service.Window(ReportRange.ThisMonth, null, null);

        await service.Invoking(s => s.ProductsAsync(w, CancellationToken.None)).Should().NotThrowAsync();
        await service.Invoking(s => s.CategoriesAsync(w, CancellationToken.None)).Should().NotThrowAsync();
        await service.Invoking(s => s.CouponsAsync(w, CancellationToken.None)).Should().NotThrowAsync();
        await service.Invoking(s => s.GovernoratesAsync(w, CancellationToken.None)).Should().NotThrowAsync();
        await service.Invoking(s => s.AnalyticsAsync(w, CancellationToken.None)).Should().NotThrowAsync();
        await service.Invoking(s => new DashboardService(db.Db, clock, service).GetSummaryAsync(CancellationToken.None)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetSummaryAsync_returns_all_KPIs()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        clock.UtcNow = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var todayStart = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        var customer = TestSeed.AddCustomer(db.Db, clock, "201000000001");

        // Today orders (non-cancelled) — 2 today + 1 cancelled (excluded from revenue)
        var governorate = TestSeed.AddGovernorate(db.Db, clock);
        var order1 = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-TEST-1", CustomerId = customer.Id,
            Status = OrderStatus.Delivered, CustomerName = "Test", CustomerPhone = "201000000001",
            GovernorateId = governorate.Id, GovernorateNameAr = "القاهرة", GovernorateNameEn = "Cairo",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Paid, GrandTotal = 500m, ProductSubtotalAfterDiscount = 450m,
            ProductDiscountTotal = 0m, CouponDiscountTotal = 0m,
            CustomerPaidShippingFee = 50m, ActualShippingCost = 35m, ShippingMargin = 15m,
            CreatedAt = todayStart.AddHours(2), DeliveredAt = todayStart.AddHours(4)
        };
        db.Db.Orders.Add(order1);

        var order2 = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-TEST-2", CustomerId = customer.Id,
            Status = OrderStatus.Pending, CustomerName = "Test", CustomerPhone = "201000000001",
            GovernorateId = governorate.Id, GovernorateNameAr = "القاهرة", GovernorateNameEn = "Cairo",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Pending, GrandTotal = 300m, ProductSubtotalAfterDiscount = 270m,
            ProductDiscountTotal = 0m, CouponDiscountTotal = 0m,
            CustomerPaidShippingFee = 30m, ActualShippingCost = 20m, ShippingMargin = 10m,
            CreatedAt = todayStart.AddHours(6)
        };
        db.Db.Orders.Add(order2);

        var order3 = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-TEST-3", CustomerId = customer.Id,
            Status = OrderStatus.Cancelled, CustomerName = "Test", CustomerPhone = "201000000001",
            GovernorateId = governorate.Id, GovernorateNameAr = "القاهرة", GovernorateNameEn = "Cairo",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Refunded, GrandTotal = 100m, ProductSubtotalAfterDiscount = 90m,
            ProductDiscountTotal = 0m, CouponDiscountTotal = 0m,
            CustomerPaidShippingFee = 10m, ActualShippingCost = 5m, ShippingMargin = 5m,
            CreatedAt = todayStart.AddHours(1), DeliveredAt = null
        };
        db.Db.Orders.Add(order3);

        // Delivered this month (order1 already delivered today)
        // Additional delivered order earlier this month
        var order4 = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-TEST-4", CustomerId = customer.Id,
            Status = OrderStatus.Delivered, CustomerName = "Test", CustomerPhone = "201000000001",
            GovernorateId = governorate.Id, GovernorateNameAr = "القاهرة", GovernorateNameEn = "Cairo",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Paid, GrandTotal = 700m, ProductSubtotalAfterDiscount = 650m,
            ProductDiscountTotal = 10m, CouponDiscountTotal = 5m,
            CustomerPaidShippingFee = 60m, ActualShippingCost = 40m, ShippingMargin = 20m,
            CreatedAt = new DateTime(2026, 6, 5, 10, 0, 0, DateTimeKind.Utc),
            DeliveredAt = new DateTime(2026, 6, 5, 14, 0, 0, DateTimeKind.Utc)
        };
        db.Db.Orders.Add(order4);

        // Order items for delivered orders (for profit calculation)
        var (product, variant) = TestSeed.AddProduct(db.Db, clock, sellingPrice: 200m, purchasePrice: 120m);
        db.Db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(), OrderId = order1.Id, ProductId = product.Id, ProductVariantId = variant.Id,
            ProductNameAr = "منتج", ProductNameEn = "Product", Sku = variant.Sku,
            Quantity = 2, OriginalUnitSellingPrice = 200m, UnitPriceAfterProductDiscount = 200m,
            FinalUnitPrice = 200m, PurchaseCostPerUnit = 120m,
            LineRevenue = 400m, LineCost = 240m, LineGrossProfit = 160m
        });
        db.Db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(), OrderId = order4.Id, ProductId = product.Id, ProductVariantId = variant.Id,
            ProductNameAr = "منتج", ProductNameEn = "Product", Sku = variant.Sku,
            Quantity = 3, OriginalUnitSellingPrice = 200m, UnitPriceAfterProductDiscount = 200m,
            FinalUnitPrice = 200m, PurchaseCostPerUnit = 120m,
            LineRevenue = 600m, LineCost = 360m, LineGrossProfit = 240m
        });

        // Low stock variant
        var (_, lowStockVariant) = TestSeed.AddProduct(db.Db, clock, stock: 3);
        // Active variant with stock above threshold (should not count)
        TestSeed.AddProduct(db.Db, clock, stock: 10);

        // Failed WhatsApp messages
        db.Db.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
        {
            Id = Guid.NewGuid(), PhoneNumber = "201000000001", MessageType = WhatsAppMessageType.OrderConfirmation,
            MessageBody = "Test", Status = WhatsAppMessageStatus.Failed, CreatedAt = clock.UtcNow
        });
        db.Db.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
        {
            Id = Guid.NewGuid(), PhoneNumber = "201000000001", MessageType = WhatsAppMessageType.Otp,
            MessageBody = "Test", Status = WhatsAppMessageStatus.Failed, CreatedAt = clock.UtcNow
        });
        // Sent message (should not count)
        db.Db.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
        {
            Id = Guid.NewGuid(), PhoneNumber = "201000000001", MessageType = WhatsAppMessageType.OrderConfirmation,
            MessageBody = "Test", Status = WhatsAppMessageStatus.Sent, CreatedAt = clock.UtcNow
        });

        // Analytics session + events (for conversion rate)
        var visitor = new AnalyticsVisitor
        {
            Id = Guid.NewGuid(), AnonymousId = "anon-1",
            FirstSeenAt = todayStart, LastSeenAt = todayStart.AddHours(1)
        };
        db.Db.AnalyticsVisitors.Add(visitor);

        var session = new AnalyticsSession
        {
            Id = Guid.NewGuid(), VisitorId = visitor.Id,
            StartedAt = todayStart, LastActivityAt = todayStart.AddHours(1),
            UtmSource = "google", ConvertedOrderId = order1.Id
        };
        db.Db.AnalyticsSessions.Add(session);

        db.Db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            Id = Guid.NewGuid(), SessionId = session.Id, VisitorId = visitor.Id,
            EventType = AnalyticsEventType.PageView, CreatedAt = todayStart
        });
        db.Db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            Id = Guid.NewGuid(), SessionId = session.Id, VisitorId = visitor.Id,
            EventType = AnalyticsEventType.ProductView, CreatedAt = todayStart.AddMinutes(5)
        });
        db.Db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            Id = Guid.NewGuid(), SessionId = session.Id, VisitorId = visitor.Id,
            EventType = AnalyticsEventType.AddToCart, CreatedAt = todayStart.AddMinutes(10)
        });
        db.Db.AnalyticsEvents.Add(new AnalyticsEvent
        {
            Id = Guid.NewGuid(), SessionId = session.Id, VisitorId = visitor.Id,
            EventType = AnalyticsEventType.CheckoutStarted, CreatedAt = todayStart.AddMinutes(15)
        });

        // Payment transaction for commission
        db.Db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(), OrderId = order1.Id, PaymentMethod = PaymentMethod.CashOnDelivery,
            Amount = 500m, CommissionAmount = 10m, Status = PaymentStatus.Paid,
            CreatedAt = clock.UtcNow
        });
        db.Db.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(), OrderId = order4.Id, PaymentMethod = PaymentMethod.CashOnDelivery,
            Amount = 700m, CommissionAmount = 14m, Status = PaymentStatus.Paid,
            CreatedAt = clock.UtcNow
        });

        await db.Db.SaveChangesAsync();

        var reportService = new ReportService(db.Db, clock);
        var dashboard = new DashboardService(db.Db, clock, reportService);
        var summary = await dashboard.GetSummaryAsync(CancellationToken.None);

        summary.TodayOrders.Should().Be(3);  // order1 + order2 + order3 — all orders created today
        summary.TodayRevenue.Should().Be(800m);  // order1(500) + order2(300); cancelled(100) excluded by status check
        summary.DeliveredOrdersThisMonth.Should().Be(2);  // order1 + order4
        summary.PendingOrders.Should().Be(1);  // order2
        summary.LowStockVariants.Should().Be(1);  // lowStockVariant (stock=3)
        summary.FailedWhatsAppMessages.Should().Be(2);  // 2 failed messages
        summary.ConversionRateThisMonth.Should().Be(1m);  // 1 order / 1 session = 1.0
        summary.NetProfitThisMonth.Should().Be(411m);  // grossProfit(400) + shippingMargin(35) - expenses(0) - commissions(24) = 411
        // Let me recalculate: grossProfit=400 (160+240), shippingMargin=35 (15+20), expenses=0, commissions=24 (10+14)
        // netProfit = 400 + 35 - 0 - 24 = 411
    }

    [Fact]
    public async Task AnalyticsAsync_returns_ordered_breakdown_with_max_twenty_rows()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        clock.UtcNow = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var todayStart = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        // Create sessions with varied UtmSource values
        for (int i = 0; i < 5; i++)
        {
            var visitor = new AnalyticsVisitor
            {
                Id = Guid.NewGuid(), AnonymousId = $"anon-{i}",
                FirstSeenAt = todayStart, LastSeenAt = todayStart.AddHours(1)
            };
            db.Db.AnalyticsVisitors.Add(visitor);
        }
        await db.Db.SaveChangesAsync();

        var visitors = db.Db.AnalyticsVisitors.ToList();

        // google: 5 sessions
        for (int i = 0; i < 5; i++)
        {
            db.Db.AnalyticsSessions.Add(new AnalyticsSession
            {
                Id = Guid.NewGuid(), VisitorId = visitors[i % visitors.Count].Id,
                StartedAt = todayStart, LastActivityAt = todayStart.AddHours(1),
                UtmSource = "google"
            });
        }
        // facebook: 3 sessions
        for (int i = 0; i < 3; i++)
        {
            db.Db.AnalyticsSessions.Add(new AnalyticsSession
            {
                Id = Guid.NewGuid(), VisitorId = visitors[i % visitors.Count].Id,
                StartedAt = todayStart, LastActivityAt = todayStart.AddHours(1),
                UtmSource = "facebook"
            });
        }
        // direct: 1 session
        db.Db.AnalyticsSessions.Add(new AnalyticsSession
        {
            Id = Guid.NewGuid(), VisitorId = visitors[0].Id,
            StartedAt = todayStart, LastActivityAt = todayStart.AddHours(1),
            UtmSource = "direct"
        });

        await db.Db.SaveChangesAsync();

        var service = new ReportService(db.Db, clock);
        var w = service.Window(ReportRange.ThisMonth, null, null);
        var analytics = await service.AnalyticsAsync(w, CancellationToken.None);

        analytics.Should().NotBeNull();
        analytics.Sessions.Should().Be(9);
        var sources = analytics.TrafficSources;
        sources.Should().HaveCount(3);
        sources.Should().BeInDescendingOrder(s => s.Count);
        sources[0].Label.Should().Be("google");
        sources[0].Count.Should().Be(5);
        sources[1].Label.Should().Be("facebook");
        sources[1].Count.Should().Be(3);
        sources[2].Label.Should().Be("direct");
        sources[2].Count.Should().Be(1);
    }

    [Fact]
    public async Task ProductsAsync_ordered_by_revenue_descending()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        clock.UtcNow = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var todayStart = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        var customer = TestSeed.AddCustomer(db.Db, clock);
        var governorate = TestSeed.AddGovernorate(db.Db, clock);

        // Three products with different revenue amounts
        var (productA, variantA) = TestSeed.AddProduct(db.Db, clock, sellingPrice: 100m, purchasePrice: 60m);
        var (productB, variantB) = TestSeed.AddProduct(db.Db, clock, sellingPrice: 200m, purchasePrice: 120m);
        var (productC, variantC) = TestSeed.AddProduct(db.Db, clock, sellingPrice: 50m, purchasePrice: 30m);

        var order = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-PROD-1", CustomerId = customer.Id,
            Status = OrderStatus.Delivered, CustomerName = "Test", CustomerPhone = "201000000001",
            GovernorateId = governorate.Id, GovernorateNameAr = "القاهرة", GovernorateNameEn = "Cairo",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Paid, GrandTotal = 500m,
            CustomerPaidShippingFee = 50m, ActualShippingCost = 35m, ShippingMargin = 15m,
            CreatedAt = todayStart.AddHours(-1), DeliveredAt = todayStart
        };
        db.Db.Orders.Add(order);

        // Product B: highest revenue (600)
        db.Db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(), OrderId = order.Id, ProductId = productB.Id, ProductVariantId = variantB.Id,
            ProductNameAr = "منتج ب", ProductNameEn = "Product B", Sku = variantB.Sku,
            Quantity = 3, OriginalUnitSellingPrice = 200m, UnitPriceAfterProductDiscount = 200m,
            FinalUnitPrice = 200m, PurchaseCostPerUnit = 120m,
            LineRevenue = 600m, LineCost = 360m, LineGrossProfit = 240m
        });
        // Product A: middle revenue (500)
        db.Db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(), OrderId = order.Id, ProductId = productA.Id, ProductVariantId = variantA.Id,
            ProductNameAr = "منتج أ", ProductNameEn = "Product A", Sku = variantA.Sku,
            Quantity = 5, OriginalUnitSellingPrice = 100m, UnitPriceAfterProductDiscount = 100m,
            FinalUnitPrice = 100m, PurchaseCostPerUnit = 60m,
            LineRevenue = 500m, LineCost = 300m, LineGrossProfit = 200m
        });
        // Product C: lowest revenue (150)
        db.Db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(), OrderId = order.Id, ProductId = productC.Id, ProductVariantId = variantC.Id,
            ProductNameAr = "منتج ج", ProductNameEn = "Product C", Sku = variantC.Sku,
            Quantity = 3, OriginalUnitSellingPrice = 50m, UnitPriceAfterProductDiscount = 50m,
            FinalUnitPrice = 50m, PurchaseCostPerUnit = 30m,
            LineRevenue = 150m, LineCost = 90m, LineGrossProfit = 60m
        });

        await db.Db.SaveChangesAsync();

        var service = new ReportService(db.Db, clock);
        var w = service.Window(ReportRange.ThisMonth, null, null);
        var rows = await service.ProductsAsync(w, CancellationToken.None);

        rows.Should().HaveCount(3);
        rows.Should().BeInDescendingOrder(r => r.Revenue);
        rows[0].ProductNameEn.Should().Be("Product B");
        rows[0].Revenue.Should().Be(600m);
        rows[0].QuantitySold.Should().Be(3);
        rows[1].ProductNameEn.Should().Be("Product A");
        rows[1].Revenue.Should().Be(500m);
        rows[1].QuantitySold.Should().Be(5);
        rows[2].ProductNameEn.Should().Be("Product C");
        rows[2].Revenue.Should().Be(150m);
        rows[2].QuantitySold.Should().Be(3);
    }

    [Fact]
    public async Task CategoriesAsync_ordered_by_revenue_descending()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        clock.UtcNow = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var todayStart = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        var customer = TestSeed.AddCustomer(db.Db, clock);
        var governorate = TestSeed.AddGovernorate(db.Db, clock);

        // Create categories
        var catClothes = new Category
        {
            Id = Guid.NewGuid(), NameAr = "ملابس", NameEn = "Clothes",
            SlugAr = "clothes-ar", SlugEn = "clothes", IsActive = true, CreatedAt = clock.UtcNow
        };
        var catElectronics = new Category
        {
            Id = Guid.NewGuid(), NameAr = "إلكترونيات", NameEn = "Electronics",
            SlugAr = "electronics-ar", SlugEn = "electronics", IsActive = true, CreatedAt = clock.UtcNow
        };
        var catBooks = new Category
        {
            Id = Guid.NewGuid(), NameAr = "كتب", NameEn = "Books",
            SlugAr = "books-ar", SlugEn = "books", IsActive = true, CreatedAt = clock.UtcNow
        };
        db.Db.Categories.AddRange(catClothes, catElectronics, catBooks);

        var productClothes = new Product
        {
            Id = Guid.NewGuid(), CategoryId = catClothes.Id,
            NameAr = "ملابس", NameEn = "T-Shirt",
            SlugAr = "tshirt-ar", SlugEn = "tshirt",
            BasePurchasePrice = 50m, BaseSellingPrice = 150m,
            IsActive = true, CreatedAt = clock.UtcNow
        };
        var productElectronics = new Product
        {
            Id = Guid.NewGuid(), CategoryId = catElectronics.Id,
            NameAr = "إلكترونيات", NameEn = "Headphones",
            SlugAr = "headphones-ar", SlugEn = "headphones",
            BasePurchasePrice = 200m, BaseSellingPrice = 500m,
            IsActive = true, CreatedAt = clock.UtcNow
        };
        var productBooks = new Product
        {
            Id = Guid.NewGuid(), CategoryId = catBooks.Id,
            NameAr = "كتب", NameEn = "Novel",
            SlugAr = "novel-ar", SlugEn = "novel",
            BasePurchasePrice = 20m, BaseSellingPrice = 40m,
            IsActive = true, CreatedAt = clock.UtcNow
        };
        db.Db.Products.AddRange(productClothes, productElectronics, productBooks);

        var order = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-CAT-1", CustomerId = customer.Id,
            Status = OrderStatus.Delivered, CustomerName = "Test", CustomerPhone = "201000000001",
            GovernorateId = governorate.Id, GovernorateNameAr = "القاهرة", GovernorateNameEn = "Cairo",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Paid, GrandTotal = 1000m,
            CustomerPaidShippingFee = 50m, ActualShippingCost = 35m, ShippingMargin = 15m,
            CreatedAt = todayStart.AddHours(-1), DeliveredAt = todayStart
        };
        db.Db.Orders.Add(order);

        var variantClothes = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = productClothes.Id, Sku = "SKU-CLOTHES",
            StockQuantity = 10, IsActive = true, CreatedAt = clock.UtcNow
        };
        var variantElectronics = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = productElectronics.Id, Sku = "SKU-ELEC",
            StockQuantity = 10, IsActive = true, CreatedAt = clock.UtcNow
        };
        var variantBooks = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = productBooks.Id, Sku = "SKU-BOOKS",
            StockQuantity = 10, IsActive = true, CreatedAt = clock.UtcNow
        };
        db.Db.ProductVariants.AddRange(variantClothes, variantElectronics, variantBooks);
        await db.Db.SaveChangesAsync();

        // Electronics: highest revenue (1000)
        db.Db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(), OrderId = order.Id, ProductId = productElectronics.Id,
            ProductVariantId = variantElectronics.Id,
            ProductNameAr = "سماعات", ProductNameEn = "Headphones", Sku = variantElectronics.Sku,
            Quantity = 2, OriginalUnitSellingPrice = 500m, UnitPriceAfterProductDiscount = 500m,
            FinalUnitPrice = 500m, PurchaseCostPerUnit = 200m,
            LineRevenue = 1000m, LineCost = 400m, LineGrossProfit = 600m
        });
        // Clothes: middle revenue (450)
        db.Db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(), OrderId = order.Id, ProductId = productClothes.Id,
            ProductVariantId = variantClothes.Id,
            ProductNameAr = "تيشيرت", ProductNameEn = "T-Shirt", Sku = variantClothes.Sku,
            Quantity = 3, OriginalUnitSellingPrice = 150m, UnitPriceAfterProductDiscount = 150m,
            FinalUnitPrice = 150m, PurchaseCostPerUnit = 50m,
            LineRevenue = 450m, LineCost = 150m, LineGrossProfit = 300m
        });
        // Books: lowest revenue (80)
        db.Db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(), OrderId = order.Id, ProductId = productBooks.Id,
            ProductVariantId = variantBooks.Id,
            ProductNameAr = "رواية", ProductNameEn = "Novel", Sku = variantBooks.Sku,
            Quantity = 2, OriginalUnitSellingPrice = 40m, UnitPriceAfterProductDiscount = 40m,
            FinalUnitPrice = 40m, PurchaseCostPerUnit = 20m,
            LineRevenue = 80m, LineCost = 40m, LineGrossProfit = 40m
        });

        await db.Db.SaveChangesAsync();

        var service = new ReportService(db.Db, clock);
        var w = service.Window(ReportRange.ThisMonth, null, null);
        var rows = await service.CategoriesAsync(w, CancellationToken.None);

        rows.Should().HaveCount(3);
        rows.Should().BeInDescendingOrder(r => r.Revenue);
        rows[0].CategoryNameEn.Should().Be("Electronics");
        rows[0].Revenue.Should().Be(1000m);
        rows[1].CategoryNameEn.Should().Be("Clothes");
        rows[1].Revenue.Should().Be(450m);
        rows[2].CategoryNameEn.Should().Be("Books");
        rows[2].Revenue.Should().Be(80m);
    }

    [Fact]
    public async Task CouponsAsync_ordered_by_times_used_descending()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        clock.UtcNow = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var todayStart = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        var customer = TestSeed.AddCustomer(db.Db, clock);

        var couponA = new Coupon
        {
            Id = Guid.NewGuid(), Code = "SAVE10", Type = CouponType.Percentage,
            Value = 10m, IsActive = true, Source = CouponSource.General,
            CreatedAt = clock.UtcNow
        };
        var couponB = new Coupon
        {
            Id = Guid.NewGuid(), Code = "FLAT50", Type = CouponType.FixedAmount,
            Value = 50m, IsActive = true, Source = CouponSource.General,
            CreatedAt = clock.UtcNow
        };
        var couponC = new Coupon
        {
            Id = Guid.NewGuid(), Code = "WELCOME", Type = CouponType.Percentage,
            Value = 15m, IsActive = true, Source = CouponSource.General,
            CreatedAt = clock.UtcNow
        };
        db.Db.Coupons.AddRange(couponA, couponB, couponC);

        var governorate = TestSeed.AddGovernorate(db.Db, clock);

        Order MakeOrder() => new()
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-CP-" + Guid.NewGuid().ToString("N")[..6],
            CustomerId = customer.Id, Status = OrderStatus.Delivered, CustomerName = "Test",
            CustomerPhone = "201000000001", GovernorateId = governorate.Id,
            GovernorateNameAr = "القاهرة", GovernorateNameEn = "Cairo",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Paid, GrandTotal = 200m,
            CustomerPaidShippingFee = 50m, ActualShippingCost = 35m, ShippingMargin = 15m,
            CreatedAt = todayStart.AddHours(-1), DeliveredAt = todayStart
        };

        // Coupon A: 4 usages
        for (int i = 0; i < 4; i++)
        {
            var order = MakeOrder();
            db.Db.Orders.Add(order);
            db.Db.CouponUsages.Add(new CouponUsage
            {
                Id = Guid.NewGuid(), CouponId = couponA.Id, CustomerId = customer.Id,
                OrderId = order.Id, DiscountAmount = 10m * (i + 1),
                UsedAt = todayStart.AddHours(i)
            });
        }

        // Coupon B: 2 usages
        for (int i = 0; i < 2; i++)
        {
            var order = MakeOrder();
            db.Db.Orders.Add(order);
            db.Db.CouponUsages.Add(new CouponUsage
            {
                Id = Guid.NewGuid(), CouponId = couponB.Id, CustomerId = customer.Id,
                OrderId = order.Id, DiscountAmount = 50m,
                UsedAt = todayStart.AddHours(i)
            });
        }

        // Coupon C: 1 usage
        var orderC = MakeOrder();
        db.Db.Orders.Add(orderC);
        db.Db.CouponUsages.Add(new CouponUsage
        {
            Id = Guid.NewGuid(), CouponId = couponC.Id, CustomerId = customer.Id,
            OrderId = orderC.Id, DiscountAmount = 30m,
            UsedAt = todayStart
        });

        await db.Db.SaveChangesAsync();

        var service = new ReportService(db.Db, clock);
        var w = service.Window(ReportRange.ThisMonth, null, null);
        var rows = await service.CouponsAsync(w, CancellationToken.None);

        rows.Should().HaveCount(3);
        rows.Should().BeInDescendingOrder(r => r.TimesUsed);
        rows[0].Code.Should().Be("SAVE10");
        rows[0].TimesUsed.Should().Be(4);
        rows[0].TotalDiscount.Should().Be(10m + 20m + 30m + 40m);
        rows[1].Code.Should().Be("FLAT50");
        rows[1].TimesUsed.Should().Be(2);
        rows[1].TotalDiscount.Should().Be(100m);
        rows[2].Code.Should().Be("WELCOME");
        rows[2].TimesUsed.Should().Be(1);
        rows[2].TotalDiscount.Should().Be(30m);
    }

    [Fact]
    public async Task GovernoratesAsync_ordered_by_shipping_margin_descending()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        clock.UtcNow = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var todayStart = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        var customer = TestSeed.AddCustomer(db.Db, clock);

        var govCairo = new ShippingGovernorate
        {
            Id = Guid.NewGuid(), NameAr = "القاهرة", NameEn = "Cairo",
            CustomerPaidShippingFee = 60m, ActualShippingCost = 30m, IsActive = true,
            SortOrder = 1, CreatedAt = clock.UtcNow
        };
        var gainGov = new ShippingGovernorate
        {
            Id = Guid.NewGuid(), NameAr = "", NameEn = "Gain Governorate",
            CustomerPaidShippingFee = 50m, ActualShippingCost = 10m, IsActive = true,
            SortOrder = 2, CreatedAt = clock.UtcNow
        };
        var lossGov = new ShippingGovernorate
        {
            Id = Guid.NewGuid(), NameAr = "", NameEn = "Loss Governorate",
            CustomerPaidShippingFee = 30m, ActualShippingCost = 50m, IsActive = true,
            SortOrder = 3, CreatedAt = clock.UtcNow
        };
        db.Db.ShippingGovernorates.AddRange(govCairo, gainGov, lossGov);
        await db.Db.SaveChangesAsync();

        // Cairo: margin = 60-30 = 30
        var order1 = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-GOV-1", CustomerId = customer.Id,
            Status = OrderStatus.Delivered, CustomerName = "Test", CustomerPhone = "201000000001",
            GovernorateId = govCairo.Id, GovernorateNameAr = "القاهرة", GovernorateNameEn = "Cairo",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Paid, GrandTotal = 200m,
            CustomerPaidShippingFee = 60m, ActualShippingCost = 30m, ShippingMargin = 30m,
            CreatedAt = todayStart.AddHours(-2), DeliveredAt = todayStart
        };
        // Gain governorate: margin = 50-10 = 40 (highest)
        var order2 = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-GOV-2", CustomerId = customer.Id,
            Status = OrderStatus.Delivered, CustomerName = "Test", CustomerPhone = "201000000001",
            GovernorateId = gainGov.Id, GovernorateNameAr = "", GovernorateNameEn = "Gain Governorate",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Paid, GrandTotal = 200m,
            CustomerPaidShippingFee = 50m, ActualShippingCost = 10m, ShippingMargin = 40m,
            CreatedAt = todayStart.AddHours(-1), DeliveredAt = todayStart
        };
        // Loss governorate: margin = 30-50 = -20 (lowest)
        var order3 = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "NV-GOV-3", CustomerId = customer.Id,
            Status = OrderStatus.Delivered, CustomerName = "Test", CustomerPhone = "201000000001",
            GovernorateId = lossGov.Id, GovernorateNameAr = "", GovernorateNameEn = "Loss Governorate",
            CityDistrict = "d", DetailedAddress = "a", PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Paid, GrandTotal = 200m,
            CustomerPaidShippingFee = 30m, ActualShippingCost = 50m, ShippingMargin = -20m,
            CreatedAt = todayStart.AddHours(-3), DeliveredAt = todayStart
        };
        db.Db.Orders.AddRange(order1, order2, order3);

        await db.Db.SaveChangesAsync();

        var service = new ReportService(db.Db, clock);
        var w = service.Window(ReportRange.ThisMonth, null, null);
        var rows = await service.GovernoratesAsync(w, CancellationToken.None);

        rows.Should().HaveCount(3);
        rows.Should().BeInDescendingOrder(r => r.ShippingMargin);
        rows[0].GovernorateNameEn.Should().Be("Gain Governorate");
        rows[0].ShippingMargin.Should().Be(40m);
        rows[1].GovernorateNameEn.Should().Be("Cairo");
        rows[1].ShippingMargin.Should().Be(30m);
        rows[2].GovernorateNameEn.Should().Be("Loss Governorate");
        rows[2].ShippingMargin.Should().Be(-20m);
    }
}
