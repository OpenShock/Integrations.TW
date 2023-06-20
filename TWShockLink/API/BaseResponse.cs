namespace ShockLink.Integrations.TW.API;

public class BaseResponse<T>
{
    public string? Message { get; set; }
    public T? Data { get; set; }

    public BaseResponse(string? message = null, T? data = default)
    {
        Message = message;
        Data = data;
    }
}