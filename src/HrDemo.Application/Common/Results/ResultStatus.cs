using System.Text.Json.Serialization;

namespace HrDemo.Application.Common.Results;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResultStatus
{
    Success,
    ValidationError,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Error
}
