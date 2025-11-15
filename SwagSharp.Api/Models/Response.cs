namespace SwagSharp.Api.Models;

public class Response<T>
{
    public bool Issucceed { get; set; }
    public Error? Error { get; set; }
    public T? Result { get; set; }
}