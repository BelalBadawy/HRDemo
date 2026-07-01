namespace HrDemo.Application.Common.Results;

public class ResponseResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public ResultStatus Status { get; init; }
    public int StatusCode { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    public ResponseResult()
    {
    }

    protected ResponseResult(bool success, ResultStatus status, int statusCode, string? message = null, IDictionary<string, string[]>? errors = null)
    {
        Success = success;
        Status = status;
        StatusCode = statusCode;
        Message = message;
        Errors = errors;
    }

    public static ResponseResult SuccessResult(string? message = null, int statusCode = 200) 
        => new(true, ResultStatus.Success, statusCode, message);

    public static ResponseResult FailureResult(ResultStatus status, string? message, int statusCode = 400, IDictionary<string, string[]>? errors = null) 
        => new(false, status, statusCode, message, errors);
}

public class ResponseResult<T> : ResponseResult
{
    public T? Data { get; init; }

    public ResponseResult() : base()
    {
    }

    private ResponseResult(bool success, T? data, ResultStatus status, int statusCode, string? message = null, IDictionary<string, string[]>? errors = null)
        : base(success, status, statusCode, message, errors)
    {
        Data = data;
    }

    public static ResponseResult<T> SuccessResult(T data, string? message = null, int statusCode = 200) 
        => new(true, data, ResultStatus.Success, statusCode, message);

    public static new ResponseResult<T> FailureResult(ResultStatus status, string? message, int statusCode = 400, IDictionary<string, string[]>? errors = null) 
        => new(false, default, status, statusCode, message, errors);
}
