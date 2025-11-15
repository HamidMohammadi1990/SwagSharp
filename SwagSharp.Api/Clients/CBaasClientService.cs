using System.Text;
using System.Text.Json;
using SwagSharp.Api.Enums;
using SwagSharp.Api.Models;
using SwagSharp.Api.Contracts.Services;

namespace SwagSharp.Api.Clients;

public class CBaasClientService(ILogger logerService, IHttpClientFactory httpClientFactory) : ICBaasClientService
{
    public Task<Response<TResponse>> GetAsync<TResponse>(string url, CBaasClientType clientType = CBaasClientType.CoreService, Dictionary<string, string> headers = null, JsonSerializerOptions options = null)
            => SendAsync<string, TResponse>(HttpMethod.Get, url, default, clientType, headers, options);

    public Task<Response<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest request, CBaasClientType clientType = CBaasClientType.CoreService, Dictionary<string, string> headers = null, JsonSerializerOptions options = null)
        => SendAsync<TRequest, TResponse>(HttpMethod.Post, url, request, clientType, headers, options);

    public Task<Response<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest request, CBaasClientType clientType = CBaasClientType.CoreService, Dictionary<string, string> headers = null, JsonSerializerOptions options = null)
        => SendAsync<TRequest, TResponse>(HttpMethod.Put, url, request, clientType, headers, options);

    public Task<Response<TResponse>> DeleteAsync<TRequest, TResponse>(string url, CBaasClientType clientType = CBaasClientType.CoreService, Dictionary<string, string> headers = null, JsonSerializerOptions options = null)
        => SendAsync<TRequest, TResponse>(HttpMethod.Delete, url, default, clientType, headers, options);


    private async Task<Response<TResponse>> SendAsync<TRequest, TResponse>(HttpMethod method, string url, TRequest request = default, CBaasClientType clientType = CBaasClientType.CoreService, Dictionary<string, string> headers = default, JsonSerializerOptions options = null)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(method, url);

            if (headers is not null)
                foreach (var header in headers)
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);

            if (request is not null && (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH"))
            {
                var json = JsonSerializer.Serialize(request, options ?? new JsonSerializerOptions());
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var httpClient = httpClientFactory.CreateClient($"{clientType}");
            using var response = await httpClient.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new Response<TResponse>
                {
                    Issucceed = false,
                    Error = string.IsNullOrWhiteSpace(content)
                        ? null
                        : JsonSerializer.Deserialize<Error>(content)
                };

            return new Response<TResponse>
            {
                Issucceed = true,
                Result = typeof(TResponse) switch
                {
                    Type t when t == typeof(string) => (TResponse)(object)content,
                    Type t when t == typeof(bool) && string.IsNullOrWhiteSpace(content) => (TResponse)(object)true,
                    _ => string.IsNullOrWhiteSpace(content) ? default : JsonSerializer.Deserialize<TResponse>(content)
                }
            };
        }
        catch (Exception ex)
        {
            var message = $"{url} data: {(request is null ? "" : JsonSerializer.Serialize(request))}";
            logerService.LogCritical(ex.Message, message);
            return new Response<TResponse> { Issucceed = false };
        }
    }
}