var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Instanciar o serviço de descriptografia
var certificadoService = new CertificadoService();

app.MapPost("/descriptografar", async (IFormFile certificado, string senha) =>
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
