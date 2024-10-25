using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DecriptPfx.Services;
//using System.Runtime.CompilerServices;    

public class ApiKeyMiddleware : AuthenticationHandler<AuthenticationSchemeOptions>
{  
    private const string API_KEY_HEADER_NAME = "X-API-KEY";
    private readonly IApiKeyValidatorService _apiKeyValidatorService;

    public ApiKeyMiddleware(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidatorService apiKeyValidatorService)
        : base(options, logger, encoder)
    {
        _apiKeyValidatorService = apiKeyValidatorService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out StringValues apiKeyHeaderValues) ||
            string.IsNullOrEmpty(apiKeyHeaderValues.FirstOrDefault()))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key não encontrada"));
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedApiKey) || !_apiKeyValidatorService.IsValidApiKey(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key inválida"));
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "ApiUser") };
        var identity = new ClaimsIdentity(claims, "ApiKey");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
    
    
    /*
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly string _validApiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
       _validApiKey = configuration.GetValue<string>("ApiKey") ?? throw new ArgumentNullException(nameof(_validApiKey), "API Key cannot be null");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Chave de API não informada!");
            return;
        }

        if (!_validApiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Acesso não autorizado!");
            return;
        }

        await _next(context);
    }
    */
