using FluentResults;

namespace CheckoutService;

public class CartError : Error
{
    public CartError(string message) : base(message) { }
}

public class ValidationError : Error
{
    public ValidationError(string message) : base(message) { }
}

public class ExternalServiceError : Error
{
    public ExternalServiceError(string message) : base(message) { }
}

public class PaymentError : Error
{
    public PaymentError(string message) : base(message) { }
}