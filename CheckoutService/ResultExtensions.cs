namespace CheckoutService;

using FluentResults;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this ResultBase result)
    {
        if (result.IsSuccess)
            return Results.Ok();

        return Results.Problem(
            title: "Ошибка оформления заказа",
            detail: string.Join("; ", result.Errors.Select(e => e.Message)),
            statusCode: MapStatusCode(result.Errors)
        );
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return Results.Problem(
            title: "Ошибка оформления заказа",
            detail: string.Join("; ", result.Errors.Select(e => e.Message)),
            statusCode: MapStatusCode(result.Errors)
        );
    }

    private static int MapStatusCode(IEnumerable<IError> errors)
    {
        if (errors.OfType<CartError>().Any())
            return StatusCodes.Status409Conflict;
        if (errors.OfType<ValidationError>().Any())
            return StatusCodes.Status400BadRequest;
        if (errors.OfType<ExternalServiceError>().Any())
            return StatusCodes.Status502BadGateway;
        if (errors.OfType<PaymentError>().Any())
            return StatusCodes.Status502BadGateway;

        return StatusCodes.Status400BadRequest;
    }
}