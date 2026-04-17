using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class UserProfileTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetMyProfile_AfterRegister_ReturnsPrefilledProfile()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Profile Co 1", slug = "profile-co-1" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var token = await GetAuthTokenAsync(Client, tenant!.TenantId, "profile-co-1", "user1@profile.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.GetAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserProfileResult>();
        body!.Email.Should().Be("user1@profile.com");
        body.FirstName.Should().Be("Test");
        body.LastName.Should().Be("User");
        body.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateMyProfile_SetsFields_AndReturnsUpdatedData()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Profile Co 2", slug = "profile-co-2" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var token = await GetAuthTokenAsync(Client, tenant!.TenantId, "profile-co-2", "user2@profile.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var putResponse = await Client.PutAsJsonAsync("/api/v1/users/me", new
        {
            firstName = "Alice",
            lastName = "Smith",
            avatarUrl = (string?)null,
            bio = "Hello world"
        });
        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync("/api/v1/users/me");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<UserProfileResult>();
        body!.FirstName.Should().Be("Alice");
        body.LastName.Should().Be("Smith");
        body.Bio.Should().Be("Hello world");
        body.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyProfile_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/v1/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMyProfile_Unauthenticated_Returns401()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/users/me", new { firstName = "X" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record TenantResult(Guid TenantId);
    private record UserProfileResult(Guid UserId, string Email, string Role, string? FirstName, string? LastName, string? AvatarUrl, string? Bio, bool IsComplete);
}
