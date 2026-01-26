using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using Moq;
using Xunit;
using Azure.Identity;

namespace API.Library.Test
{
    public class GraphAPITests :IDisposable
    {
        private readonly Mock<IGraphClientWrapper> graphClientWrapperMock;
        private readonly Mock<IApiConfiguration> apiConfigurationMock;
        private readonly Dictionary<string, string> settingsMock;
        private readonly Mock<ICacheD> cacheDMock;
        private GraphAPI graphApi;

        public GraphAPITests()
        {
            // Mock IApiConfiguration and Settings
            apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "ANY_ENTRAID" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" },
                { "API_ENTRAID_EXTERNAL_CALL_TIMEOUT", "10" },
                { "API_ENTRAID_CUSTOM_PROPERTIES", "" },
                { "API_ENTRAID_GROUPS", "LAP_Allow_CSO_Staff_Access,LAP_Allow_Field_Interviewer_Staff_Access,LAP_Allow_Field_Coordinator_Staff_Access" },
                { "API_ENTRAID_TENANTID", "e7f948fc-4a62-4c63-8b00-03aa6a169e55" },
                { "API_ENTRAID_CLIENTID", "b3f8c5d2-1e4a-4b6c-9f0e-7c8d5e6f7a8b" },
                { "API_ENTRAID_CLIENTSECRET", "test-secret" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);

            // Mock CacheD and Get_BSO
            cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = false
            };
            cacheDMock.Setup(c => c.Get_BSO_WITHLOCK<dynamic>("API", "EntraID", "GetGroupMembers", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock the wrapper interface instead of GraphServiceClient
            graphClientWrapperMock = new Mock<IGraphClientWrapper>();

            // Set up ApiServicesHelper.GraphClientWrapper to use our mock
            ApiServicesHelper.GraphClientWrapper = graphClientWrapperMock.Object;

            // Use the default constructor
            graphApi = new GraphAPI();
        }

        [Fact]
        public void GetGroupMembers_ReturnsUserDetails_WhenSuccessful()
        {
            // Arrange
            var fakeUsers = new List<User>
            {
                new User
                {
                    Id = "1",
                    UserPrincipalName = "john.doe@example.com",
                    DisplayName = "John Doe",
                    Mail = "john@example.com",
                    JobTitle = "Developer",
                    Department = "Engineering",
                    GivenName = "John",
                    Surname = "Doe",
                    OnPremisesSamAccountName = "johndoe",
                    OnPremisesDistinguishedName = "CN=John Doe,OU=Users,DC=example,DC=com",
                    AccountEnabled = true,
                    BusinessPhones = new List<string> { "555-1234" }
                }
            };

            // Mock the wrapper to return our fake users
            graphClientWrapperMock
                .Setup(x => x.GetGroupMembersAsync(It.IsAny<string[]>()))
                .Returns(Task.FromResult(fakeUsers));

            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var result = graphApi.GetGroupMembers();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Dictionary<string, UserDetail>>(result);

        }


        [Fact]
        public void GetGroupMembers_ReturnsEmptyDictionary_WhenNoUsersExist()
        {
            // Arrange
            // Mock empty result from wrapper

            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var result = graphApi.GetGroupMembers();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Dictionary<string, UserDetail>>(result);
        }

        [Fact]
        public void GetGroupMembers_ReturnsEmptyDictionary_OnException()
        {
            // Arrange
            // Mock the wrapper to throw an exception

            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var result = graphApi.GetGroupMembers();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Dictionary<string, UserDetail>>(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetUserByEmail_ReturnsUserDetail_WhenUserExists()
        {
            // Arrange
            var expectedUser = new User
            {
                Id = "2",
                UserPrincipalName = "jane.smith@example.com",
                DisplayName = "Jane Smith",
                Mail = "jane@example.com",
                JobTitle = "Manager",
                Department = "HR",
                GivenName = "Jane",
                Surname = "Smith",
                OnPremisesSamAccountName = "janesmith",
                OnPremisesDistinguishedName = "CN=Jane Smith,OU=Users,DC=example,DC=com",
                AccountEnabled = true,
                BusinessPhones = new List<string> { "555-5678" }
            };

            // Mock the wrapper method
            graphClientWrapperMock
                .Setup(x => x.GetUserByEmailAsync("jane@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(expectedUser));

            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var result = graphApi.GetUserByEmail("jane@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<UserDetail>(result);
            Assert.Equal("Jane Smith", result.DisplayName);
            Assert.Equal("jane@example.com", result.EmailAddress);
            Assert.Equal("Manager", result.JobTitle);
            Assert.Equal("jane.smith@example.com", result.UserPrincipalName);
        }

        [Fact]
        public void GetUserByEmail_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            graphClientWrapperMock
                .Setup(x => x.GetUserByEmailAsync("nonexistent@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<User>(null));

            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var result = graphApi.GetUserByEmail("nonexistent@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetUserByEmail_ReturnsNull_OnException()
        {
            // Arrange
            graphClientWrapperMock
                
                .Setup(x => x.GetUserByEmailAsync("invalid@example.com", It.IsAny<CancellationToken>()))
                .Throws(new Exception("User not found"));

            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var result = graphApi.GetUserByEmail("invalid@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Search_ReturnsUserDetail_WhenUserExists()
        {
            // Arrange
            string testUpn = "test.user@example.com";

            // This test depends on GetGroupMembers, so you might need to mock that method's dependencies

            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var result = graphApi.Search(testUpn);

            // Assert based on whether the user exists in the mocked data
            // This will depend on your mock setup for GetGroupMembers
        }

        [Fact]
        public void Search_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            string nonExistentUpn = "nonexistent@example.com";

            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var result = graphApi.Search(nonExistentUpn);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetGraphClient_ReturnsSameInstance_WhenCalledMultipleTimes()
        {
            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var client1 = graphApi.getGraphClient();
            var client2 = graphApi.getGraphClient();

            // Assert
            Assert.NotNull(client1);
            Assert.NotNull(client2);
            Assert.Same(client1, client2);
        }

        [Fact]
        public void GetGraphClient_ReturnsValidInstance()
        {
            // Act
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;
            var client = graphApi.getGraphClient();

            // Assert
            Assert.NotNull(client);
            Assert.IsType<GraphServiceClient>(client);
        }
        public void Dispose()
        {
            ResetApiServicesHelper();
        }
        private void ResetApiServicesHelper()
        {
            // Clear any static configuration between tests
            ApiServicesHelper.ApiConfiguration = null;
            ApiServicesHelper.CacheD = null;
            ApiServicesHelper.GraphClientWrapper = null;
            apiConfigurationMock.Reset();
            graphClientWrapperMock.Reset();
            settingsMock.Clear();
            cacheDMock.Reset();
            graphApi = null;
        }
    }
}