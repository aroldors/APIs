using DecriptPfx.Services;

var builder = WebApplication.CreateBuilder(args);


// Registrar o serviço de validação de API Key
builder.Services.AddSingleton<IApiKeyValidatorService, ApiKeyValidatorService>();

// Configuração da autenticação por API Key
builder.Services.AddAuthentication("ApiKeyScheme")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKeyScheme", null);


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Adiciona o middleware de autenticação com API Key
app.UseMiddleware<ApiKeyMiddleware>();

app.UseHttpsRedirection();

// Instanciar o serviço de descriptografia
var certificadoService = new CertificadoService();

app.MapPost("/descriptografar", [Authorize(AuthenticationSchemes = "ApiKeyScheme")] async (IFormFile certificado, string senha) =>
{
    if (certificado == null || string.IsNullOrEmpty(senha))
    {
        return Results.BadRequest("Certificado e senha são obrigatórios.");
    }

    try
    {
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
});

app.Run();
