using AuthorizationMicroservices.Models;
using FluentResults;

namespace AuthorizationMicroservices;

public class UserService(Hasher hasher)
{
    private readonly List<User> _users = [];
    private readonly Hasher _hasher = hasher;

    public Result<Guid> AddUser(string username, string email, string password)
    {
        var errors = Validate(username, email, password);
        if (errors.Count == 0)
            return Result.Fail<Guid>(errors);

        if (_users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return Result.Fail<Guid>(new ConflictError("User with this email already exists"));

        var user = User.Create(username, email, _hasher.Hash(password));
        _users.Add(user);

        return Result.Ok(user.Id);
    }

    public Result<Guid> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result.Fail<Guid>(new ValidationError("Email and password are required"));

        var user = _users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        // Не раскрываем, что именно неверно (email или пароль)
        if (user is null || !_hasher.Verify(user.HashPasswd, password))
            return Result.Fail<Guid>(new AuthError("Invalid email or password"));

        return Result.Ok(user.Id);
    }

    private static List<IError> Validate(string username, string email, string password)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(username))
            errors.Add(new ValidationError("Username is required"));

        if (string.IsNullOrWhiteSpace(email))
            errors.Add(new ValidationError("Email is required"));

        if (string.IsNullOrWhiteSpace(password))
            errors.Add(new ValidationError("Password is required"));
        else if (password.Length < 6)
            errors.Add(new ValidationError("Password must be at least 6 characters"));

        return errors;
    }
}