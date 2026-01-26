using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace API
{
    /// <summary>
    /// IGraphClientWrapper defines an abstraction for interacting with Microsoft Graph.
    /// By using this interface, the GraphAPI business logic does not need to directly depend on the Graph SDK.
    /// This makes the code more testable since a mock or fake implementation of this interface can be used during unit tests.
    /// </summary>
    public interface IGraphClientWrapper
    {
        /// <summary>
        /// Retrieves a complete list of group objects containing group memberships from Microsoft Graph.
        /// The implementation is responsible for handling pagination internally.
        /// </summary>
        /// <param name=""></param>
        /// <returns>A Task that returns a List of User objects.</returns>
        Task<List<User>> GetGroupMembersAsync(string[] groupNames);



        /// <summary>
        /// Retrieves a User object by email.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A Task that returns the User object, or null if not found.</returns>
        Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);       

    }
}