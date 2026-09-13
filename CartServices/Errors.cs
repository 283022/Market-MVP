using FluentResults;

namespace CartServices;

public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message) { }
}

public class ValidationError : Error
{
    public ValidationError(string message) : base(message) { }
}

public class ExternalServiceError : Error
{
    public ExternalServiceError(string message) : base(message) { }
}

public class CartStateError : Error
{
    public CartStateError(string message) : base(message) { }
}