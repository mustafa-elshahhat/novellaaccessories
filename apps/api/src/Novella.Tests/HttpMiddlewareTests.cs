using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Novella.Tests;

public class HttpMiddlewareTests
{
    [Fact]
    public async Task Health_is_public_and_does_not_expose_private_cache_headers()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.IsSuccessStatusCode.Should().BeTrue();
        (response.Headers.CacheControl?.Public == true).Should().BeFalse();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("novella-api");
    }

    [Fact]
    public async Task Protected_customer_endpoint_requires_bearer_token()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders/my");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/public/seo/product/any-slug")]
    [InlineData("/api/public/seo/category/any-slug")]
    [InlineData("/api/public/seo/page/any-slug")]
    public async Task Removed_public_entity_seo_endpoints_return_not_found(string path)
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Removed_admin_seo_endpoint_returns_not_found()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/seo/content");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "novella-api",
                    ["Jwt:Audience"] = "novella-clients",
                    ["Jwt:SigningKey"] = "test-signing-key-for-http-middleware-tests-123456",
                    ["Cors:StorefrontOrigin"] = "https://storefront.test",
                    ["Cors:AdminOrigin"] = "https://admin.test"
                });
            });
        }
    }
}
