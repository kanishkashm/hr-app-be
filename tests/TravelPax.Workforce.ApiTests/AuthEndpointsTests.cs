using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TravelPax.Workforce.ApiTests;

public sealed class AuthEndpointsTests : IClassFixture<TravelPaxApiWebApplicationFactory>
{
    private readonly TravelPaxApiWebApplicationFactory _factory;

    public AuthEndpointsTests(TravelPaxApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_And_Refresh_ReturnSuccess()
    {
        using var client = _factory.CreateClient();
        await _factory.SeedDefaultAuthDataAsync();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            emailOrUsername = "superadmin@travelpax.lk",
            password = "TravelPax@123",
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var accessToken = loginJson.RootElement.GetProperty("accessToken").GetString();
        var refreshToken = loginJson.RootElement.GetProperty("refreshToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken,
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        using var refreshJson = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        var nextAccessToken = refreshJson.RootElement.GetProperty("accessToken").GetString();
        var nextRefreshToken = refreshJson.RootElement.GetProperty("refreshToken").GetString();

        Assert.False(string.IsNullOrWhiteSpace(nextAccessToken));
        Assert.False(string.IsNullOrWhiteSpace(nextRefreshToken));
        Assert.NotEqual(refreshToken, nextRefreshToken);
    }

    [Fact]
    public async Task AuthMe_RequiresAuth_And_ReturnsOk_WithValidToken()
    {
        using var client = _factory.CreateClient();
        await _factory.SeedDefaultAuthDataAsync();

        var unauthorized = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            emailOrUsername = "superadmin@travelpax.lk",
            password = "TravelPax@123",
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var accessToken = loginJson.RootElement.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var authorized = await client.GetAsync("/api/v1/auth/me");

        if (authorized.StatusCode != HttpStatusCode.OK)
        {
            var responseBody = await authorized.Content.ReadAsStringAsync();
            var authHeader = string.Join(" | ", authorized.Headers.WwwAuthenticate.Select(x => x.ToString()));
            throw new Xunit.Sdk.XunitException(
                $"Expected 200 but got {(int)authorized.StatusCode} {authorized.StatusCode}. " +
                $"WWW-Authenticate: {authHeader}. Body: {responseBody}");
        }

        using var meJson = JsonDocument.Parse(await authorized.Content.ReadAsStringAsync());
        Assert.Equal("superadmin@travelpax.lk", meJson.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task OutboxHealth_ReturnsOk_ForAuthorizedAuditUser()
    {
        using var client = _factory.CreateClient();
        await _factory.SeedDefaultAuthDataAsync();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            emailOrUsername = "superadmin@travelpax.lk",
            password = "TravelPax@123",
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var accessToken = loginJson.RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/v1/notifications/outbox/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _ = json.RootElement.GetProperty("pendingCount").GetInt32();
        _ = json.RootElement.GetProperty("sentCount").GetInt32();
        _ = json.RootElement.GetProperty("failedCount").GetInt32();
    }
}
