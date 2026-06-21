namespace Novella.Api.Common;

/// <summary>The standard API error model: <c>{ code, message, details }</c>.</summary>
public sealed class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object Details { get; set; } = new Dictionary<string, object?>();
}
