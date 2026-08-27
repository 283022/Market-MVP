namespace AuthorizationMicroservices;

public class Hasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string hash, string password)
    {
        var result = BCrypt.Net.BCrypt.Verify(password, hash);
        return result;
    }
}