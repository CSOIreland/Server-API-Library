namespace API
{
    public interface IFirebase
    {
        bool Authenticate(string Uid, string AccessToken);
        bool Logout(string Uid, string AccessToken);
        IDictionary<string, dynamic> GetAllUsers();
        bool DeleteUser(string uid);
    }
}
