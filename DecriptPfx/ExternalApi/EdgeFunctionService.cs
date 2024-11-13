using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DecriptPfx.ExternalApi;

public class EdgeFunctionService
{
    private readonly HttpClient _httpClient;

    public EdgeFunctionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IActionResult> CallEdgeFunctionAsync(string jwtToken)
    {
        // URL da API externa, incluindo o parâmetro
        string requestUrl = $"https://sasqneudlipdhxmdonli.supabase.co/functions/v1/authentication";

        // Configura o cabeçalho Authorization com o token JWT
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", jwtToken);
               
        // Faz a chamada POST
        HttpResponseMessage response = await _httpClient.PostAsync(requestUrl,null);

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
