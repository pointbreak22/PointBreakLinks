using System.Net;
using System.Net.Http.Json;

namespace WebAPI.IntegrationTests;

// Exercises the real HTTP pipeline end to end — DI wiring, EF Core against a real Postgres,
// JWT issuance/validation, the exception-handling middleware, CORS — none of which
// Application.Tests' mocked-repository unit tests can catch if something is mis-registered.
[Collection("Integration")]
public class AuthFlowTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ThenAccessProtectedEndpoint_Succeeds()
    {
        var email = $"integration-{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Integration Test",
            email,
            password = "Passw0rd1",
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var body = await registerResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var accessToken = body!["access_token"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        using var authedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        authedRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
        var meResponse = await _client.SendAsync(authedRequest);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsProblemDetailsNotA500()
    {
        // RegisterCommandHandler throws AuthenticationException for a duplicate email (not
        // ConflictException) — DomainExceptionHandler maps that to 401, matching how the same
        // "account already/not right" family of errors behaves on /login.
        var email = $"integration-dup-{Guid.NewGuid():N}@example.com";
        var payload = new { name = "First", email, password = "Passw0rd1" };

        var first = await _client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.True(problem!.ContainsKey("detail"));
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedNotInternalServerError()
    {
        var email = $"integration-login-{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new { name = "Login Test", email, password = "Passw0rd1" });
        // Guards against this test passing for the wrong reason: a broken register would leave
        // no user behind, and "wrong password for a user that doesn't exist" also 401s — making
        // this assertion pass even though it isn't testing what its name says.
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
