using DecriptPfx.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


// Registrar o serviço de validação de API Key
builder.Services.AddSingleton<IApiKeyValidatorService, ApiKeyValidatorService>();

// Configuração da autenticação por API Key
builder.Services.AddAuthentication("ApiKeyScheme").AddScheme<AuthenticationSchemeOptions,ApiKeyMiddleware>("ApiKeyScheme", null);


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddAuthorization();


builder.Services.AddEndpointsApiExplorer();
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

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Instanciar o serviço de descriptografia
var certificadoService = new CertificadoService();

//app.MapPost("/descriptografar", [Authorize(AuthenticationSchemes = "ApiKeyScheme")] async (IFormFile certificado, string senha) =>
app.MapPost("/descriptografar", [Authorize(AuthenticationSchemes = "ApiKeyScheme")] async (string path, string certificado, string senha) =>
{
    if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(certificado) || string.IsNullOrEmpty(senha))
    {
        return Results.BadRequest("Certificado e senha são obrigatórios.");
    }

    try
    {
        // Combinar o caminho e o nome do arquivo para obter o caminho completo
        var fullPath = Path.Combine(path, certificado);        
        
        /*
        // Converter o arquivo recebido em um array de bytes
        using var memoryStream = new MemoryStream();
        await certificado.CopyToAsync(memoryStream);
        var certificadoBytes = memoryStream.ToArray();
        */

        // Ler o arquivo como um array de bytes
        var certificadoBytes = File.ReadAllBytes(fullPath);

        // Utilizar o serviço para descriptografar o certificado
        var info = certificadoService.DescriptografarCertificado(certificadoBytes, senha);

        // Retornar as informações em JSON
        return Results.Json(info);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao processar o certificado: {ex.Message}");
    }
});

app.Run();
