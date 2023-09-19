using System.DirectoryServices.AccountManagement;

namespace API
{
    public interface IActiveDirectory
    {
        IDictionary<string, dynamic> List<T>() where T : UserPrincipal;
        IDictionary<string, dynamic> List();
        dynamic Search<T>(string username) where T : UserPrincipal;
        dynamic Search(string username);
        bool IsPasswordValid(dynamic userPrincipal, string password);
        PrincipalContext adContext { get; }
    }
}
