using AuthorizationMicroservices.Models;

namespace AuthorizationMicroservices;

public class UserService(Hasher hasher)
{
    private readonly List<User> _users = [];
    private readonly Hasher _hasher = hasher;
    
    public void AddUser(string username, string email, string password)
    {
        _users.Add(User.Create(username, email, _hasher.Hash(password)));
    }

    public bool Login(string email, string password)
    {
        var user= _users.FirstOrDefault(x => x.Email == email);
        if (user is null)
            throw new Exception("User is not founded");
        if(!_hasher.Verify(user.HashPasswd, password))
            return false;
        return true;
    }

    public Guid? GetUserId(string email)
    {
        return _users.FirstOrDefault(x => x.Email == email).Id;
    }
}