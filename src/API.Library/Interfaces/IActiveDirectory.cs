using System.DirectoryServices.AccountManagement;
using System.Dynamic;

namespace API
{
    public interface IActiveDirectory
    {
        IDictionary<string, UserDetail> List<T>() where T : UserPrincipal;
        IDictionary<string, UserDetail> List();
        dynamic Search<T>(string username) where T : UserPrincipal;
        dynamic Search(string username);
        [Obsolete]
        bool IsPasswordValid(dynamic userPrincipal, string password);
        PrincipalContext adContext { get; }
        public Dictionary<string, UserDetail> CreateAPIUserPrincipalObject(dynamic userPrincipal);
        
    }
}
