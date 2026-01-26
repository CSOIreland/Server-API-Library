
using Microsoft.Graph;

namespace API
{
    public interface IExternalUserData
    {
        IDictionary<string, UserDetail> GetGroupMembers();
        UserDetail GetUserByEmail(string email);

        UserDetail Search(string upn);

        GraphServiceClient getGraphClient();

    }
}