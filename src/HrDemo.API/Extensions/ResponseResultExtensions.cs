using Microsoft.AspNetCore.Http;
using HrDemo.Application.Common.Results;

namespace HrDemo.API.Extensions;

public static class ResponseResultExtensions
{
    public static IResult ToHttpResult(this ResponseResult result)
    {
        if (result == null)
        {
            return Results.BadRequest();
        }

        return Results.Json(result, statusCode: result.StatusCode);
    }
}
