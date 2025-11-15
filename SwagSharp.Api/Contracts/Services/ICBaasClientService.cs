using System.Text.Json;
using SwagSharp.Api.Enums;
using SwagSharp.Api.Models;

namespace SwagSharp.Api.Contracts.Services;

public interface ICBaasClientService
{
    public Task<Response<TResponse>> GetAsync<TResponse>(string url, CBaasClientType clientType = CBaasClientType.CoreService, Dictionary<string, string> headers = null, JsonSerializerOptions options = null);
    public Task<Response<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest body, CBaasClientType clientType = CBaasClientType.CoreService, Dictionary<string, string> headers = null, JsonSerializerOptions options = null);
    public Task<Response<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest body, CBaasClientType clientType = CBaasClientType.CoreService, Dictionary<string, string> headers = null, JsonSerializerOptions options = null);
    public Task<Response<TResponse>> DeleteAsync<TRequest, TResponse>(string url, CBaasClientType clientType = CBaasClientType.CoreService, Dictionary<string, string> headers = null, JsonSerializerOptions options = null);
}