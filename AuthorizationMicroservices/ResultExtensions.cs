namespace AuthorizationMicroservices;
using FluentResults;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this ResultBase result)
    {
        if (result.IsSuccess)
            return Results.Ok();

        return Results.Problem(
            title: "Ошибка аутентификации",
            detail: string.Join("; ", result.Errors.Select(e => e.Message)),
            statusCode: MapStatusCode(result.Errors)
        );
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return Results.Problem(
            title: "Ошибка аутентификации",
            detail: string.Join("; ", result.Errors.Select(e => e.Message)),
            statusCode: MapStatusCode(result.Errors)
        );
    }

    public static IResult ToHttpResult(this Result result, Func<IResult> onSuccess)
        => result.IsSuccess ? onSuccess() : result.ToHttpResult();

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
        => result.IsSuccess ? onSuccess(result.Value) : result.ToHttpResult();

    private static int MapStatusCode(IEnumerable<IError> errors)
    {
        if (errors.OfType<NotFoundError>().Any())
            return StatusCodes.Status404NotFound;
        if (errors.OfType<ConflictError>().Any())
            return StatusCodes.Status409Conflict;
        if (errors.OfType<AuthError>().Any())
            return StatusCodes.Status401Unauthorized;
        if (errors.OfType<ValidationError>().Any())
            return StatusCodes.Status400BadRequest;

        return StatusCodes.Status400BadRequest;
    }
}