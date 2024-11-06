using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DecriptPfx.ExternalApi;

public class ExternalApiService
{
    private readonly HttpClient _httpClient;

    public ExternalApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IActionResult> CallExternalApiAsync(string parameter, string jwtToken, string customHeaderValue)
    {
        // URL da API externa, incluindo o parâmetro
        string requestUrl = $"https://externalapi.com/api/resource?param={parameter}";

        // Configura o cabeçalho Authorization com o token JWT
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", jwtToken);
        
        // Adiciona o segundo cabeçalho personalizado
        _httpClient.DefaultRequestHeaders.Add("apiKey", customHeaderValue);
        
        // Faz a chamada GET
        HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

        if (response.IsSuccessStatusCode)
        {
            // Lê e converte a resposta em um objeto JSON
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var resultObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

            // Retorna o objeto JSON ou processa conforme necessário
            return new JsonResult(resultObject);
        }
        else
        {
            return new StatusCodeResult((int)response.StatusCode);
        }
    }
}
