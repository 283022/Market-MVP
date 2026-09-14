using FluentResults;

namespace OrderServices;

public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message) { }
}

public class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message) { }
}

public class ValidationError : Error
{
    public ValidationError(string message) : base(message) { }
}