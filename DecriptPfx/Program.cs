using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DecriptPfx.Models;
using DecriptPfx.JwtBearer;
using DecriptPfx.ExternalApi;
using Supabase;
using Supabase.Storage;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;


var builder = WebApplication.CreateBuilder(args);

// Registrar o serviço de validação de API Key
//builder.Services.AddSingleton<IApiKeyValidatorService, ApiKeyValidatorService>();

// Configuração da autenticação por API Key
//builder.Services.AddAuthentication("ApiKeyScheme").AddScheme<AuthenticationSchemeOptions, ApiKeyMiddleware>("ApiKeyScheme", null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

builder.Services.AddHttpClient<EdgeFunctionService>();

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

app.MapPost("/autenticar", async (User user) =>
{                 
    // Cria uma instância de HttpClient
    var httpClient = new HttpClient();

    // Cria uma instância do ExternalApiService passando o HttpClient
    var edgeFunctionService = new EdgeFunctionService(httpClient);    
    
    // Chama o método CallExternalApiAsync e obtém o resultado
    var result = await edgeFunctionService.CallEdgeFunctionAsync(user.Token);
    
    // Recupera o objeto JSON da resposta
    var jsonResult = (result as JsonResult)?.Value;

    if (jsonResult != null)
    {
        // Parse o objeto JSON para um JObject
        var jsonObject = JObject.FromObject(jsonResult);

        // Acesse a propriedade que deseja (no caso, "isLoggedIn")
        string isLoggedIn = jsonObject["isLoggedIn"]?.ToString() ?? "false";        
        
        if (isLoggedIn == "True")
            return Results.Ok(JwtBearerService.GenerateToken(user));
    }

    return Results.Unauthorized();
});

app.MapGet("/teste", () => "Teste de autenticação!").RequireAuthorization();

//app.MapPost("/descriptografar", [Authorize(AuthenticationSchemes = "ApiKeyScheme")] async (string path, string certificado, string senha) =>
//app.MapPost("/descriptografar", [Authorize(AuthenticationSchemes = "ApiKeyScheme")] async (IFormFile certificado, string senha) =>
app.MapPost("/descriptografar", async (Certificado certificado) =>
{
    if ((certificado == null)) 
        return Results.BadRequest("Certificado e senha são obrigatórios.");

    try
    {
        var url = "https://sasqneudlipdhxmdonli.supabase.co"; //Environment.GetEnvironmentVariable("SUPABASE_URL");
        var key = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");        
              
        // Inicializar o cliente Supabase
        var supabase = new Supabase.Client(url, key);
        await supabase.InitializeAsync();

        var storage = supabase.Storage;
        var bucket = storage.From(certificado.Bucket);

        // Realizar a requisição de download do arquivo
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        
        // Construir a URL completa para o arquivo
        var fileUrl = $"https://sasqneudlipdhxmdonli.supabase.co/storage/v1/object/public/{certificado.Bucket}/{certificado.FileName}";

        // Fazer a solicitação HTTP para baixar o arquivo
        var response = await httpClient.GetAsync(fileUrl);
        if (!response.IsSuccessStatusCode)
        {
            return Results.BadRequest("O arquivo do certificado não foi encontrado ou houve um erro na solicitação.");
        }    
        
        var certificadoBytes = await response.Content.ReadAsByteArrayAsync();
                
        // Converter o arquivo recebido em um array de bytes
        //using var memoryStream = new MemoryStream();
        //await certificado.CopyToAsync(memoryStream);
        //var certificadoBytes = memoryStream.ToArray();
             
        // Utilizar o serviço para descriptografar o certificado
        var info = certificadoService.DescriptografarCertificado(certificadoBytes, certificado.Password);

        // Retornar as informações em JSON
        return Results.Json(info);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao processar o certificado: {ex.Message}");
    }
})
.DisableAntiforgery()
.RequireAuthorization();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
