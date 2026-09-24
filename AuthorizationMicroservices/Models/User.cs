using FluentResults;

namespace AuthorizationMicroservices.Models;


//TODO:сделать Value Object прежде чем засовывать в бд 
public class User
{
    public Guid Id { get; private set; }

    public string Username { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }

    //for ef core
    private User()
    {
    }

    private User(
        string username,
        string email,
        string passwordHash)
    {
        //Todo: удалить этого, когда будет реализация через EF core или заменить на UUID
        Id = Guid.NewGuid();
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
    }

    public static Result<User> Create(
        string username,
        string email,
        string passwordHash)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(username))
            errors.Add(new ValidationError("Username is required"));

        if (string.IsNullOrWhiteSpace(email))
            errors.Add(new ValidationError("Email is required"));
        else if (!email.Contains('@'))
            errors.Add(new ValidationError("Invalid email"));

        if (string.IsNullOrWhiteSpace(passwordHash))
            errors.Add(new ValidationError("Password hash is required"));

        if (errors.Count != 0)
            return Result.Fail<User>(errors);

        return Result.Ok(
            new User(
                username,
                email,
                passwordHash));
    }
}