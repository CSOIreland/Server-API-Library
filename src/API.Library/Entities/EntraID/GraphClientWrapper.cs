using System.Threading.Tasks;
using System.Threading;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using Azure;

namespace API
{
    /// <summary>
    /// GraphClientWrapper is a concrete implementation of IGraphClientWrapper.
    /// This class wraps the Microsoft GraphServiceClient, encapsulating the details of making API calls
    /// (such as handling pagination) so that the GraphAPI class does not need to know these details.
    /// This abstraction improves testability by allowing a mock implementation to be injected in unit tests.
    /// </summary>
    public class GraphClientWrapper : IGraphClientWrapper
    {
        /// <summary>
        /// Retrieves all User objects from Microsoft Graph, handling pagination automatically.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A Task that returns a complete List of User objects.</returns>
        public async Task<List<User>> GetGroupMembersAsync(string[] groupNames)
        {

            // Create a list to accumulate users from multiple pages.
            var users = new List<User>();

            GraphServiceClient _client = ApiServicesHelper.GraphApi.getGraphClient();

            try
            {
                           // Step 1: Find all target groups in a single query
                var groupsResponse = await _client.Groups.GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Filter = string.Join(" or ",
                        groupNames.Select(name => $"(displayName eq '{name}')"));
                });

                if (groupsResponse?.Value == null || !groupsResponse.Value.Any())
                {
                    Log.Instance.Info("No matching groups found.");
                    return users; // Return empty list instead of null
                }

                // Step 2: Process each group and fetch members
                foreach (var group in groupsResponse.Value)
                {
                    Log.Instance.Info($"Fetching members for group: {group.DisplayName}");

                    // Initialize pagination for group members
                    var membersPage = await _client.Groups[group.Id].Members.GraphUser.GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Select = APIConstants.API_ENTRA_QUERY.Split(',');
                        requestConfig.QueryParameters.Expand = new[] { "manager" };
                        requestConfig.QueryParameters.Top = 999; // Max page size
                        requestConfig.Headers.Add("ConsistencyLevel", "eventual"); // Required for some queries
                    });

                    while (membersPage != null)
                    {
                        // Step 3: Extract users from current page
                        foreach (var user in membersPage.Value)
                        {
                                users.Add(user);
                        }

                        // Step 4: Handle pagination
                        if (!string.IsNullOrEmpty(membersPage.OdataNextLink))
                        {
                            membersPage = await _client.Groups[group.Id].Members.GraphUser.WithUrl(membersPage.OdataNextLink)
                                .GetAsync();
                        }
                        else
                        {
                            break; // Exit pagination loop
                        }
                    }
                }
                return users;
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
                return users;
            }
        }

        /// <summary>
        /// Retrieves a User object by email from Microsoft Graph.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A Task that returns the User object if found, otherwise null.</returns>
        public async Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {

            // Directly index into the Users property to retrieve the user.
            // The Graph SDK supports this pattern for fetching individual user details.

            // Directly index into the Users property to retrieve the user.
            // The Graph SDK supports this pattern for fetching individual user details.
            GraphServiceClient _client = ApiServicesHelper.GraphApi.getGraphClient();

            return await _client?.Users?[email]?.GetAsync((requestConfig) =>
            {
                requestConfig.QueryParameters.Select = APIConstants.API_ENTRA_QUERY.Split(',');
                requestConfig.QueryParameters.Expand = new[] { "manager" };

            }, cancellationToken: cancellationToken);
        }
    }
}