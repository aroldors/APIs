using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DecriptPfx.Models;
using DecriptPfx.JwtBearer;
using DecriptPfx.ExternalApi;

var builder = WebApplication.CreateBuilder(args);

// Registrar o serviço de validação de API Key
//builder.Services.AddSingleton<IApiKeyValidatorService, ApiKeyValidatorService>();

// Configuração da autenticação por API Key
//builder.Services.AddAuthentication("ApiKeyScheme").AddScheme<AuthenticationSchemeOptions, ApiKeyMiddleware>("ApiKeyScheme", null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddHttpClient<ExternalApiService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "DecriptPfx", Version = "v1" });

    // Definir o esquema de segurança API Key no Swagger
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Chave de API necessária para autenticação. Adicione 'X-API-KEY' no campo abaixo.",
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey (Encoding.UTF8.GetBytes(AppSettingsService.JwtSettings.Key)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseMiddleware<ApiKeyMiddleware>();
app.UseHttpsRedirection();

// Instanciar o serviço de descriptografia
var certificadoService = new CertificadoService();

app.MapPost("/autenticar", (User user) =>
{
    return Results.Ok(JwtBearerService.GenerateToken(user));
});


app.MapGet("/teste", () => "Teste de autenticação!").RequireAuthorization();

//app.MapPost("/descriptografar", [Authorize(AuthenticationSchemes = "ApiKeyScheme")] async (string path, string certificado, string senha) =>
//app.MapPost("/descriptografar", [Authorize(AuthenticationSchemes = "ApiKeyScheme")] async (IFormFile certificado, string senha) =>
app.MapPost("/descriptografar", async (IFormFile certificado, string senha) =>
{
    //if ((certificado == null) || (string.IsNullOrEmpty(senha)))
    if ((certificado == null) || (string.IsNullOrEmpty(senha)))
    {
        return Results.BadRequest("Certificado e senha são obrigatórios.");
    }

    try
    {
        // Combinar o caminho e o nome do arquivo para obter o caminho completo
        //var fullPath = Path.Combine(path, certificado);        
        
        // Ler o arquivo como um array de bytes
        //var certificadoBytes = File.ReadAllBytes(fullPath);

        
        // Converter o arquivo recebido em um array de bytes
        using var memoryStream = new MemoryStream();
        await certificado.CopyToAsync(memoryStream);
        var certificadoBytes = memoryStream.ToArray();
        
        // Utilizar o serviço para descriptografar o certificado
        var info = certificadoService.DescriptografarCertificado(certificadoBytes, senha);

        // Retornar as informações em JSON
        return Results.Json(info);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao processar o certificado: {ex.Message}");
    }
}).DisableAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
