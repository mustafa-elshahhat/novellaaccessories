namespace Novella.Application.Common;

/// <summary>
/// A domain/application error that maps to the standard API error model
/// <c>{ code, message, details }</c> with an HTTP status code.
/// </summary>
public class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public IDictionary<string, object?> Details { get; }

    public AppException(string code, string message, int statusCode = 400, IDictionary<string, object?>? details = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details ?? new Dictionary<string, object?>();
    }

    public static AppException NotFound(string message = "Resource not found.", string code = ErrorCodes.NotFound)
        => new(code, message, 404);

    public static AppException Validation(string message, string code = ErrorCodes.ValidationError, IDictionary<string, object?>? details = null)
        => new(code, message, 400, details);

    public static AppException Unauthorized(string message = "Unauthorized.", string code = ErrorCodes.Unauthorized)
        => new(code, message, 401);

    public static AppException Forbidden(string message = "Forbidden.", string code = ErrorCodes.Forbidden)
        => new(code, message, 403);

    public static AppException Conflict(string message, string code = ErrorCodes.Conflict)
        => new(code, message, 409);
}
