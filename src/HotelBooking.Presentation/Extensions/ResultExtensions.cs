using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HotelBooking.Application.Common.Results;

namespace HotelBooking.Presentation.Common.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return ToProblem(result.Error);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return ToProblem(result.Error);
    }

    public static IActionResult ToCreatedResult<T>(this Result<T> result, string location)
    {
        if (result.IsSuccess)
            return new CreatedResult(location, result.Value);

        return ToProblem(result.Error);
    }

    private static IActionResult ToProblem(Error error)
    {
        var (statusCode, title) = error.Type switch
        {
            ErrorType.Validation   => (StatusCodes.Status400BadRequest,          "Validation failed"),
            ErrorType.NotFound     => (StatusCodes.Status404NotFound,            "Resource not found"),
            ErrorType.Conflict     => (StatusCodes.Status409Conflict,            "Conflict"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized,        "Unauthorized"),
            ErrorType.Forbidden    => (StatusCodes.Status403Forbidden,           "Forbidden"),
            _                      => (StatusCodes.Status500InternalServerError, "Server error"),
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = error.Message,
            Type = error.Code,
        };

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}