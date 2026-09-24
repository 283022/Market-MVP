using FluentResults;

namespace AuthorizationMicroservices;

public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message) { }
}

public class ValidationError : Error
{
    public ValidationError(string message) : base(message) { }
}

public class ConflictError : Error
{
    public ConflictError(string message) : base(message) { }
}

public class AuthError : Error
{
    public AuthError(string message) : base(message) { }
}