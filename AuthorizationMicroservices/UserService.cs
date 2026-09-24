using AuthorizationMicroservices.Models;
using FluentResults;

namespace AuthorizationMicroservices;

public class UserService(Hasher hasher)
{
    //TODO: сделать бд
    private readonly List<User> _users = [];
    private readonly Hasher _hasher = hasher;
    
    //TODO: добавить метод delete user 
    public Result<Guid> AddUser(
        string username,
        string email,
        string password)
    {
        // Проверяем данные, которые пришли от пользователя
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email)
                                                || string.IsNullOrWhiteSpace(password))
            return Result.Fail("data cannot be null");
        
        // Проверяем уникальность email
        if (_users.Any(u =>
                u.Email.Equals(
                    email,
                    //TODO: когда будет value object Email перенести это туда
                    StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail<Guid>(
                new ConflictError(
                    "User with this email already exists"));
        }

        // Пароль не храним в открытом виде
        //TODO: пока что эта логика остается тут, но когда будут Value Object перенести валидацию в Password 
        var passwordHash = _hasher.Hash(password);

        // User.Create возвращает Result<User>
        var userResult = User.Create(
            username,
            email,
            passwordHash);

        // Если модель не прошла свою валидацию
        if (userResult.IsFailed)
            return Result.Fail<Guid>(userResult.Errors);

        // Получаем самого User из Result
        var user = userResult.Value;

        _users.Add(user);

        return Result.Ok(user.Id);
    }

    public Result<Guid> Login(
        string email,
        string password)
    {
        // Проверяем обязательные поля
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return Result.Fail<Guid>(
                new ValidationError(
                    "Email and password are required"));
        }

        // Ищем пользователя по email
        var user = _users.FirstOrDefault(u =>
            u.Email.Equals(
                email,
                StringComparison.OrdinalIgnoreCase));

        // Не раскрываем, существует ли такой email.
        // Для неправильного email и неправильного пароля
        // возвращаем одну и ту же ошибку.
        if (user is null ||
            !_hasher.Verify(
                user.PasswordHash,
                password))
        {
            return Result.Fail<Guid>(
                new AuthError(
                    "Invalid email or password"));
        }

        return Result.Ok(user.Id);
    }
    
}