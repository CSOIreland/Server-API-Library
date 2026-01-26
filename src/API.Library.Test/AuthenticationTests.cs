using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text;
using System.DirectoryServices.AccountManagement;
using Castle.Components.DictionaryAdapter.Xml;


namespace API.Library.Test
{
    public class AuthTests : IDisposable
    {
        private readonly Mock<HttpContext> contextMock = new Mock<HttpContext>();
        private readonly Mock<ClaimsPrincipal> userMock = new Mock<ClaimsPrincipal>();
        private readonly new Mock<System.Security.Principal.IIdentity> identityMock = new Mock<System.Security.Principal.IIdentity>();
        private readonly Mock<ApiServicesHelper> apiServicesHelperMock = new Mock<ApiServicesHelper>();
        private Dictionary<string, string> settingsMock = new Dictionary<string, string>{};

        public AuthTests()
        {
            // Initialize common mocks
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);
            settingsMock = new Dictionary<string, string>{};

            // Reset static state before each test
            ResetApiServicesHelper();
        }

        [Fact]
        public void Authenticate_UserIsAuthenticatedWithEntraid_ReturnsTrue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            var headersMock = new Mock<IHeaderDictionary>();
            headersMock.Setup(h => h.ContainsKey("MSAL")).Returns(true);

            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Headers).Returns(headersMock.Object);

            contextMock.Setup(c => c.Request).Returns(requestMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session).Returns(sessionMock.Object);
            contextMock.Setup(c => c.Session.Id).Returns("12345678");
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);

            // Serialize the userPrincipal to JSON and then to a byte array
            string userPrincipal = Utility.JsonSerialize_IgnoreLoopingReference(GetUserPrincipal());
            byte[] value = Encoding.UTF8.GetBytes(userPrincipal);
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(true); // Returns true to indicate value is found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "ENTRAID" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication> { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                                        .Returns("mockToken");
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.True(result);
            Assert.Equal("DOMAIN\\username", auth.NetworkIdentity);
            Assert.Equal("username", auth.NetworkUsername);
        }

        [Fact]
        public void Authenticate_UserIsNotAuthenticatedWithEntraid_ReturnsFalse()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            var headersMock = new Mock<IHeaderDictionary>();
            headersMock.Setup(h => h.ContainsKey("MSAL")).Returns(true);

            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Headers).Returns(headersMock.Object);

            contextMock.Setup(c => c.Request).Returns(requestMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);
            contextMock.Setup(c => c.User.Identity.IsAuthenticated).Returns(false);

            // Serialize the userPrincipal to JSON and then to a byte array
            byte[] value = null;
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(false); // Returns false to indicate value is not found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "ENTRAID" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication> { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                                        .Returns("mockToken");
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.False(result);
            Assert.Null(auth.NetworkIdentity);
            Assert.Null(auth.NetworkUsername);
        }

        [Fact]
        public void Authenticate_UserIsAuthenticatedWithWindows_ReturnsTrue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session).Returns(sessionMock.Object);
            contextMock.Setup(c => c.Session.Id).Returns("12345678");
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);

            // Serialize the userPrincipal to JSON and then to a byte array
            string userPrincipal = Utility.JsonSerialize_IgnoreLoopingReference(GetUserPrincipal());
            byte[] value = Encoding.UTF8.GetBytes(userPrincipal);
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(true); // Returns true to indicate value is found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "WINDOWS" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication> { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                                        .Returns("mockToken");
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.True(result);
            Assert.Equal("DOMAIN\\username", auth.NetworkIdentity);
            Assert.Equal("username", auth.NetworkUsername);
        }

        [Fact]
        public void Authenticate_UserIsNotAuthenticatedWithWindows_ReturnsFalse()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);
            contextMock.Setup(c => c.User.Identity.IsAuthenticated).Returns(false);

            // Serialize the userPrincipal to JSON and then to a byte array
            byte[] value = null;
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(false); // Returns false to indicate value is not found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "WINDOWS" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication> { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                                        .Returns("mockToken");
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.False(result);
            Assert.Null(auth.NetworkIdentity);
            Assert.Null(auth.NetworkUsername);
        }

        [Fact]
        public void Authenticate_UserIsAuthenticatedWithAnyWindows_ReturnsTrue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session).Returns(sessionMock.Object);
            contextMock.Setup(c => c.Session.Id).Returns("12345678");
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);

            // Serialize the userPrincipal to JSON and then to a byte array
            string userPrincipal = Utility.JsonSerialize_IgnoreLoopingReference(GetUserPrincipal());
            byte[] value = Encoding.UTF8.GetBytes(userPrincipal);
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(true); // Returns true to indicate value is found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "ANY_WINDOWS" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication> { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                                        .Returns("mockToken");
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.True(result);
            Assert.Equal("DOMAIN\\username", auth.NetworkIdentity);
            Assert.Equal("username", auth.NetworkUsername);
        }

        [Fact]
        public void Authenticate_UserIsNotAuthenticatedWithAnyWindows_ReturnsFalse()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);
            contextMock.Setup(c => c.User.Identity.IsAuthenticated).Returns(true);

            // Serialize the userPrincipal to JSON and then to a byte array
            byte[] value = null;
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(false); // Returns false to indicate value is not found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "ANY_WINDOWS" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" },
                { "API_AD_DOMAIN", "TEST" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication> { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                                        .Returns("mockToken");
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Authenticate_UserIsNotAuthenticatedWithAnyWindows_ReturnsNull()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);
            contextMock.Setup(c => c.User.Identity.IsAuthenticated).Returns(false);

            // Serialize the userPrincipal to JSON and then to a byte array
            byte[] value = null;
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(false); // Returns false to indicate value is not found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "ANY_WINDOWS" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication> { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                                        .Returns("mockToken");
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.Null(result);
            Assert.Null(auth.NetworkIdentity);
            Assert.Null(auth.NetworkUsername);
        }

        [Fact]
        public void Authenticate_UserIsAuthenticatedWithAnyEntraid_ReturnsTrue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session).Returns(sessionMock.Object);
            contextMock.Setup(c => c.Session.Id).Returns("12345678");
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);
            contextMock.Setup(c => c.Request.Headers.ContainsKey("MSAL")).Returns(true);

            // Serialize the userPrincipal to JSON and then to a byte array
            string userPrincipal = Utility.JsonSerialize_IgnoreLoopingReference(GetUserPrincipal());
            byte[] value = Encoding.UTF8.GetBytes(userPrincipal);
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(true); // Returns true to indicate value is found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "ANY_ENTRAID" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" },
                { "API_ENTRAID_EXTERNAL_CALL_TIMEOUT", "10" },
                { "API_ENTRAID_TENANTID", "e7f948fc-4a62-4c63-8b00-03aa6a169e55" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication>(); // { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                .Returns("mockToken");
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.True(result);
            Assert.Equal("DOMAIN\\username", auth.NetworkIdentity);
            Assert.Equal("username", auth.NetworkUsername);
        }

        [Fact]
        public void Authenticate_UserIsNotAuthenticatedWithAnyEntraid_ReturnsFalse()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            var headersMock = new Mock<IHeaderDictionary>();
            headersMock.Setup(h => h.ContainsKey("MSAL")).Returns(true);

            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Headers).Returns(headersMock.Object);

            contextMock.Setup(c => c.Request).Returns(requestMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);
            contextMock.Setup(c => c.User.Identity.IsAuthenticated).Returns(false);

            // Serialize the userPrincipal to JSON and then to a byte array
            byte[] value = null;
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(false); // Returns false to indicate value is not found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "ANY_ENTRAID" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication> { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                                        .Returns("mockToken");
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.False(result);
            Assert.Null(auth.NetworkIdentity);
            Assert.Null(auth.NetworkUsername);
        }

        [Fact]
        public void Authenticate_UserIsNotAuthenticatedWithAnyEntraid_ReturnsNull()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            identityMock.Setup(i => i.IsAuthenticated).Returns(true);
            identityMock.Setup(i => i.Name).Returns("DOMAIN\\username");
            userMock.Setup(u => u.Identity).Returns(identityMock.Object);
            contextMock.Setup(c => c.User).Returns(userMock.Object);

            var headersMock = new Mock<IHeaderDictionary>();
            headersMock.Setup(h => h.ContainsKey("MSAL")).Returns(true);

            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Headers).Returns(headersMock.Object);

            contextMock.Setup(c => c.Request).Returns(requestMock.Object);

            // Mock ActiveDirectory
            var activeDirectoryMock = new Mock<IActiveDirectory>();
            activeDirectoryMock.Setup(a => a.CreateAPIUserPrincipalObject(It.IsAny<object>()))
                               .Returns(GetUserDetailDictionary());
            ApiServicesHelper.ActiveDirectory = activeDirectoryMock.Object;

            var sessionMock = new Mock<ISession>();
            contextMock.Setup(c => c.Session.IsAvailable).Returns(true);
            contextMock.Setup(c => c.User.Identity.IsAuthenticated).Returns(false);

            // Serialize the userPrincipal to JSON and then to a byte array
            byte[] value = null;
            sessionMock.Setup(s => s.TryGetValue(It.Is<string>(str => str == "UserPrincipal"), out value))
                .Returns(false); // Returns false to indicate value is not found

            var auth = new Auth();

            // Mock IApiConfiguration and Settings
            var apiConfigurationMock = new Mock<IApiConfiguration>();
            settingsMock = new Dictionary<string, string>
            {
                { "API_AUTHENTICATION_TYPE", "ANY_ENTRAID" },
                { "API_STATELESS", "false" },
                { "API_MASK_PARAMETERS", "MtrInput,Lgn1Fa,Totp" }
            };
            apiConfigurationMock.SetupGet(ac => ac.Settings).Returns(settingsMock);
            ApiServicesHelper.ApiConfiguration = apiConfigurationMock.Object;

            // Mock CacheD and Get_BSO
            var cacheDMock = new Mock<ICacheD>();
            var memCachedDValueMock = new MemCachedD_Value
            {
                hasData = true,
                data = new UserDetail { Identity = "DOMAIN\\username" }
            };
            cacheDMock.Setup(c => c.Get_BSO<dynamic>("API", "Common", "Authenticate", It.IsAny<string>()))
                      .Returns(memCachedDValueMock);
            ApiServicesHelper.CacheD = cacheDMock.Object;

            // Mock EntraTokenAuthentication and GetToken using partial mocking
            var entraTokenAuthenticationMock = new Mock<ITokenAuthentication> { CallBase = true };
            entraTokenAuthenticationMock.Setup(e => e.GetToken(It.IsAny<HttpContext>()))
                                        .Returns((string)null);
            ApiServicesHelper.EntraTokenAuthentication = entraTokenAuthenticationMock.Object;

            // Mock Common and MaskParameters
            var commonMock = new Mock<ICommon>() { CallBase = true };
            commonMock.Setup(c => c.MaskParameters(It.IsAny<string>()))
                                        .Returns("parameters");

            // Act
            var context = contextMock.Object;
            var user = GetUserDetail();
            var result = auth.Authenticate(ref context, ref user, cancellationTokenSource);

            // Assert
            Assert.Null(result);
            Assert.Null(auth.NetworkIdentity);
            Assert.Null(auth.NetworkUsername);
        }

        private UserPrincipal GetUserPrincipal()
        {
            UserPrincipal user = new UserPrincipal(new PrincipalContext(ContextType.Domain));
            user.SamAccountName = "user123";
            user.GivenName = "John";
            user.Surname = "Murphy";
            user.EmailAddress = "john.murphy@example.com";
            user.EmployeeId = "12345";
            user.MiddleName = "Michael";
            user.VoiceTelephoneNumber = "021-12345";
            return user;
        }

        private UserDetail GetUserDetail()
        {
            var user = new UserDetail
            {
                Identity = "user456",
                SamAccountName = "user456",
                UserPrincipalName = "user456@example.com",
                DistinguishedName = "CN=User 456,OU=Users,DC=example,DC=com",
                DisplayName = "User 456",
                Name = "User 456",
                GivenName = "Jane",
                MiddleName = null,
                Surname = "Smith",
                EmailAddress = "jane.smith@example.com",
                EmployeeId = "EMP67890",
                Description = "Another test user.",
                Enabled = false,
                VoiceTelephoneNumber = "555-987-6543",
                AdditionalProperties = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "Role", "Manager" },
                    { "LastLogin", "2024-10-27T12:00:00Z" }
                }
            };
            return user;
        }

        private Dictionary<string, UserDetail> GetUserDetailDictionary()
        {
            Dictionary<string, UserDetail> dictionary = new Dictionary<string, UserDetail>();
            var user = GetUserDetail();
            dictionary[user.SamAccountName] = user;
            return dictionary;
        }


        public void Dispose()
        {
            // Clean up static state after each test
            ResetApiServicesHelper();
        }

        private void ResetApiServicesHelper()
        {
            // Reset all static services
            ApiServicesHelper.ActiveDirectory = null;
            ApiServicesHelper.ApiConfiguration = null;
            ApiServicesHelper.CacheD = null;
            ApiServicesHelper.EntraTokenAuthentication = null;
            ApiServicesHelper.GraphApi = null;
            settingsMock.Clear();
        }
    }
}
