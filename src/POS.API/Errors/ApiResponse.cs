namespace POS.API.Errors;

public class ApiResponse
{
    private readonly int StatusCode;
    private readonly string? Message;

    public ApiResponse(int statusCode, string? message = null)
    {
        StatusCode = statusCode;
        Message = message??GetMessageByStatusCode(statusCode);
    }

    private string? GetMessageByStatusCode(int statusCode)
    {
        return statusCode switch
        {
            400 => "A Bad Request, You have made",
            401 => "Authorized, you are not",
            404 => "Resource was not found",
            500 => "Errors are the path to the dark side. Errors kead to anger. Anger leads to hate. Hate leads to career change.",
            _ => null
        };
    }
}
