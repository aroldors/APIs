using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace DecriptPfx.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string ApiKeyHeaderName = "x-api-key";
        private readonly string _validApiKey;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _validApiKey = configuration.GetValue<string>("ApiKey") ?? throw new ArgumentNullException(nameof(_validApiKey), "API Key cannot be null");
        }

        public async Task InvokeAsync(HttpContext context)
        {            
            if (!context.Request.Headers.TryGetValue((ApiKeyHeaderName), out var extractedApiKey))
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
    }
}

