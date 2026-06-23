using FluentAssertions;
using Novella.Application.Abstractions;
using Novella.Application.Catalog;
using Novella.Application.Customers;
using Novella.Application.Payments;
using Novella.Application.Reports;
using Novella.Application.WhatsApp;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
using Xunit;

namespace Novella.Tests;

public class AdminCompatibilityTests
{
    [Fact]
    public async Task Admin_customer_read_model_does_not_expose_hashes_or_tokens()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var customer = TestSeed.AddCustomer(db.Db, clock);

        var result = await new CustomerAdminService(db.Db, clock).GetAsync(customer.Id, CancellationToken.None);

        result.FullName.Should().Be(customer.FullName);
        result.GetType().GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Password") || n.Contains("Hash") || n.Contains("Token") || n.Contains("Code"));
    }

    [Fact]
    public async Task Admin_can_activate_and_deactivate_customer_without_sensitive_fields()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var customer = TestSeed.AddCustomer(db.Db, clock);
        var service = new CustomerAdminService(db.Db, clock);

        var inactive = await service.SetStatusAsync(customer.Id, false, CancellationToken.None);
        var active = await service.SetStatusAsync(customer.Id, true, CancellationToken.None);

        inactive.IsActive.Should().BeFalse();
        active.IsActive.Should().BeTrue();
        active.GetType().GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Password") || n.Contains("Hash") || n.Contains("Token") || n.Contains("Code"));
    }

    [Fact]
    public async Task Product_filters_and_inventory_movements_are_available_to_admin()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var (product, variant) = TestSeed.AddProduct(db.Db, clock, active: true);
        var admin = TestSeed.AddAdmin(db.Db, clock);
        var service = new CatalogAdminService(db.Db, clock);

        await service.AdjustStockAsync(variant.Id, new StockAdjustRequest(7, "cycle count"), admin.Id, CancellationToken.None);
        var page = await service.GetProductsAsync(new Application.Common.PageQuery { Page = 1, PageSize = 20 }, null, product.CategoryId, true, null, CancellationToken.None);
        var movements = await service.GetInventoryMovementsAsync(variant.Id, CancellationToken.None);

        page.Items.Should().ContainSingle(p => p.Id == product.Id);
        movements.Should().Contain(m => m.Reason == "cycle count" && m.Quantity == -3);
    }

    [Fact]
    public void Payment_readiness_returns_status_only()
    {
        var service = new PaymentAdminService(new FakePaymentFactory(), new PaymentRuntimeOptions("webhook-secret", "CashOnDelivery"));

        var rows = service.GetReadiness("https://api.example.com");

        rows.Should().Contain(r => r.Method == PaymentMethod.CashOnDelivery.ToString() && r.IsActive);
        rows.GetType().GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Secret", StringComparison.OrdinalIgnoreCase) && n.Contains("Value", StringComparison.OrdinalIgnoreCase));
        rows.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.ReadinessStatus));
    }

    [Fact]
    public async Task WhatsApp_admin_logs_redact_otp_body()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        db.Db.WhatsAppMessageLogs.Add(new WhatsAppMessageLog
        {
            Id = Guid.NewGuid(), PhoneNumber = "201000000001", MessageType = WhatsAppMessageType.Otp,
            MessageBody = "Your code is 123456", Status = WhatsAppMessageStatus.Sent, CreatedAt = clock.UtcNow
        });
        await db.Db.SaveChangesAsync();
        var messenger = new WhatsAppMessenger(db.Db, new FakeWhatsAppClient(), clock);
        var service = new WhatsAppAdminService(db.Db, clock, new FakeWhatsAppClient(), messenger, new FakeWhatsAppConfigStatus());

        var logs = await service.GetMessagesAsync(new WhatsAppMessageQuery(), CancellationToken.None);

        logs.Items.Single().MessageBody.Should().BeNull();
    }

    [Fact]
    public async Task Expense_report_uses_backend_values()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        db.Db.Expenses.Add(new Expense { Id = Guid.NewGuid(), Category = ExpenseCategory.Packaging, Amount = 25m, ExpenseDate = clock.UtcNow, CreatedAt = clock.UtcNow });
        await db.Db.SaveChangesAsync();

        var report = await new ReportService(db.Db, clock).ExpensesAsync(new DateWindow(clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(1)), CancellationToken.None);

        report.Should().ContainSingle(r => r.Category == ExpenseCategory.Packaging && r.Amount == 25m && r.Count == 1);
    }

    private sealed class FakePaymentFactory : IPaymentProviderFactory
    {
        public IReadOnlyList<IPaymentProvider> All { get; } = new IPaymentProvider[] { new FakePaymentProvider(PaymentMethod.CashOnDelivery, true), new FakePaymentProvider(PaymentMethod.BankCard, false) };
        public IPaymentProvider? ForMethod(PaymentMethod method) => All.FirstOrDefault(p => p.Method == method);
        public IPaymentProvider? ForProvider(string providerName) => All.FirstOrDefault(p => p.ProviderName == providerName);
    }

    private sealed class FakePaymentProvider : IPaymentProvider
    {
        public FakePaymentProvider(PaymentMethod method, bool isActive) { Method = method; IsActive = isActive; }
        public PaymentMethod Method { get; }
        public string ProviderName => Method.ToString();
        public bool IsActive { get; }
        public Task<PaymentInitiationResult> InitiatePaymentAsync(Order order, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PaymentCallbackResult> HandleCallbackAsync(string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PaymentStatus> GetPaymentStatusAsync(string providerReference, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
