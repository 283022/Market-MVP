namespace AuthorizationMicroservices.Models;

public class User
{
    public Guid Id { get; set; } = new Guid();
    public string Username { get; set; }
    public string Email { get; set; }
    public string HashPasswd { get; set; }

    private User(string username, string email, string hashPasswd)
    {
        Username = username;
        Email = email;
        HashPasswd = hashPasswd;
    }
    
    public static User Create(string username, string email, string hashPasswd)
    {
        return new User(username, email, hashPasswd);
    }
}