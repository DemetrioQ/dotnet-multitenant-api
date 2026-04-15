using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class TenantTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateTenant_Returns201()
    {
        var response = await Client.PostAsJsonAsync("/api/tenants", new
        {
            name = "Acme Corp",
            slug = "acme"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTenant_DuplicateSlug_Returns409()
    {
        await Client.PostAsJsonAsync("/api/tenants", new { name = "Alpha", slug = "alpha" });

        var response = await Client.PostAsJsonAsync("/api/tenants", new { name = "Alpha 2", slug = "alpha" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
