using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Features.EmailTemplates;
using SaasApi.Domain.Entities;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class EmailTemplateTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);

    private async Task<(Guid tenantId, string adminToken, string slug)> CreateStoreWithAdminAsync(
        string name, string slug, string adminEmail)
    {
        var tenantResp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name, slug });
        var tenant = await tenantResp.Content.ReadFromJsonAsync<TenantResult>();
        var token = await CreateAdminAsync(tenant!.TenantId, slug, adminEmail);
        return (tenant.TenantId, token, slug);
    }

    private HttpClient AdminClient(string token)
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    [Fact]
    public async Task List_ReturnsEveryTemplateType_WithDefaults()
    {
        var (_, token, _) = await CreateStoreWithAdminAsync("Tmpl Co", "tmpl-list", "admin@tmpl-list.com");

        var resp = await AdminClient(token).GetAsync("/api/v1/email-templates");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await resp.Content.ReadFromJsonAsync<List<EmailTemplateListItemDto>>(JsonOpts);
        list!.Count.Should().Be(Enum.GetValues<EmailTemplateType>().Length);
        list.Should().OnlyContain(x => !string.IsNullOrEmpty(x.DefaultSubject));
        list.Should().OnlyContain(x => x.CustomSubject == null);
    }

    [Fact]
    public async Task Upsert_ThenGet_ReturnsCustom()
    {
        var (_, token, _) = await CreateStoreWithAdminAsync("Tmpl Up", "tmpl-up", "admin@tmpl-up.com");
        var client = AdminClient(token);

        var put = await client.PutAsJsonAsync("/api/v1/email-templates/CustomerVerification", new
        {
            subject = "Hello {{ customer_first_name }} from {{ store_name }}",
            bodyHtml = "<p>Click: <a href=\"{{ verification_link }}\">verify</a></p>",
            enabled = true
        });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await client.GetAsync("/api/v1/email-templates/CustomerVerification");
        var detail = await get.Content.ReadFromJsonAsync<EmailTemplateDetailDto>(JsonOpts);
        detail!.IsCustom.Should().BeTrue();
        detail.Subject.Should().Contain("{{ customer_first_name }}");
    }

    [Fact]
    public async Task Delete_RevertsToDefault()
    {
        var (_, token, _) = await CreateStoreWithAdminAsync("Tmpl Del", "tmpl-del", "admin@tmpl-del.com");
        var client = AdminClient(token);

        await client.PutAsJsonAsync("/api/v1/email-templates/CustomerPasswordReset", new
        {
            subject = "custom reset subject",
            bodyHtml = "<p>custom</p>",
            enabled = true
        });

        var del = await client.DeleteAsync("/api/v1/email-templates/CustomerPasswordReset");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await client.GetAsync("/api/v1/email-templates/CustomerPasswordReset");
        var detail = await get.Content.ReadFromJsonAsync<EmailTemplateDetailDto>(JsonOpts);
        detail!.IsCustom.Should().BeFalse();
        detail.Subject.Should().NotContain("custom reset subject");
    }

    [Fact]
    public async Task Preview_RendersPlaceholders()
    {
        var (_, token, _) = await CreateStoreWithAdminAsync("Preview Co", "tmpl-prev", "admin@tmpl-prev.com");
        var client = AdminClient(token);

        var resp = await client.PostAsJsonAsync("/api/v1/email-templates/OrderPaid/preview", new
        {
            subject = "Order {{ order_number }} paid at {{ store_name }}",
            bodyHtml = "<p>Total: {{ order_total }} {{ currency }}</p><p>Hi {{ customer_first_name }}!</p>",
            enabled = true
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var preview = await resp.Content.ReadFromJsonAsync<EmailTemplatePreviewDto>();
        preview!.Subject.Should().Contain("ORD-DEMO12345");
        preview.Subject.Should().Contain("Preview Co");
        preview.BodyHtml.Should().Contain("Alex");
        preview.BodyHtml.Should().Contain("42.50");
        preview.BodyHtml.Should().Contain("USD");
    }

    [Fact]
    public async Task NonAdmin_Cannot_List()
    {
        var (tenantId, _, slug) = await CreateStoreWithAdminAsync("NonAdm", "tmpl-nonadm", "admin@tmpl-nonadm.com");

        // Create a member user in the same tenant.
        var memberToken = await GetAuthTokenAsync(Client, tenantId, slug, email: "member@tmpl-nonadm.com");
        var memberClient = Factory.CreateClient();
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var resp = await memberClient.GetAsync("/api/v1/email-templates");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CustomTemplate_IsUsed_WhenRendering()
    {
        // Sanity: renderer picks the merchant's override over the default.
        var (tenantId, token, _) = await CreateStoreWithAdminAsync("Rend Co", "tmpl-rend", "admin@tmpl-rend.com");

        await AdminClient(token).PutAsJsonAsync("/api/v1/email-templates/CustomerVerification", new
        {
            subject = "Welcome to {{ store_name }}, custom!",
            bodyHtml = "<p>{{ verification_link }}</p>",
            enabled = true
        });

        using var scope = Factory.Services.CreateScope();
        var renderer = scope.ServiceProvider.GetRequiredService<SaasApi.Application.Common.Interfaces.IEmailTemplateRenderer>();

        // Seed a tenant scope so the query filter matches — test helper directly tracks it.
        var tenantService = scope.ServiceProvider.GetRequiredService<SaasApi.Application.Common.Interfaces.ICurrentTenantService>();
        ((SaasApi.Infrastructure.Services.CurrentTenantService)tenantService).SetTenant(tenantId);

        var model = new SaasApi.Application.Common.Interfaces.CustomerVerificationModel(
            StoreName: "Rend Co",
            StoreUrl: "https://tmpl-rend.shop.demetrioq.com",
            CustomerFirstName: "Sam",
            CustomerEmail: "sam@example.com",
            VerificationLink: "https://tmpl-rend.shop.demetrioq.com/verify-email?token=X");

        var rendered = await renderer.RenderAsync(tenantId, EmailTemplateType.CustomerVerification, model);
        rendered.Subject.Should().Be("Welcome to Rend Co, custom!");
        rendered.HtmlBody.Should().Contain("https://tmpl-rend.shop.demetrioq.com/verify-email?token=X");
    }
}
