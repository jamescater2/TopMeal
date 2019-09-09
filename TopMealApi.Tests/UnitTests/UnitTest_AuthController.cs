using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;
using TopMealApi.Context;
using TopMealApi.Controllers;
using TopMealApi.Dtos;

namespace TopMealApi.Tests
{
    public class UnitTest_AuthController : UnitTest_ControllerBase
    {
        private readonly ILogger<AuthController> _logger;

        private readonly IConfiguration _configuration;

        public UnitTest_AuthController()
        {
            var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
            var factory = serviceProvider.GetService<ILoggerFactory>();

            _logger = factory.CreateLogger<AuthController>();
            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        }

        [Fact]
        public async void Test_Register()
        {
            var dbOptions = NewInMemoryDb("Auth_Test_Register");
            
            using (var context = new AppDbContext(dbOptions))
            {
                var authRepository = new AuthRepository(context);
                var controller = new AuthController(authRepository, _logger, _configuration);

                // Empty UserForLoginDto object
                {
                    var ret = await controller.Register(null);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Missing argument - Need to supply a UserForLoginDto with Username and Password", retResult.Value);
                }

                // Missing username and password
                {
                    var userForLogin = new UserForRegisterDto { };
                    var ret = await controller.Register(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Username must be between 2 and 50 characters", retResult.Value);
                }

                // Username too short
                {
                    var userForLogin = new UserForRegisterDto { Username = "ra" };
                    var ret = await controller.Register(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Username must be between 2 and 50 characters", retResult.Value);
                }

                // Username too long
                {
                    var userForLogin = new UserForRegisterDto { Username = "longname1longname2longname3longname4longname5longname6" };
                    var ret = await controller.Register(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Username must be between 2 and 50 characters", retResult.Value);
                }

                // Passsword missing
                {
                    var userForLogin = new UserForRegisterDto { Username = "dummyusername" };
                    var ret = await controller.Register(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Password must be between 4 and 50 characters", retResult.Value);
                }

                // Passsword too short
                {
                    var userForLogin = new UserForRegisterDto { Username = "dummyusername", Password = "min" };
                    var ret = await controller.Register(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Password must be between 4 and 50 characters", retResult.Value);
                }

                // Passsword too long
                {
                    var userForLogin = new UserForRegisterDto { Username = "dummyusername", Password = "longname1longname2longname3longname4longname5longname6" };
                    var ret = await controller.Register(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Password must be between 4 and 50 characters", retResult.Value);
                }

                // Valid User with valid password
                {
                    var userForLogin = new UserForRegisterDto { Username = "avalidusername", Password = "avalidpassword" };
                    var ret = await controller.Register(userForLogin);
                    var retResult = Assert.IsType<StatusCodeResult>(ret); // SUCCESS
                    
                    Assert.NotNull(retResult);
                    Assert.Equal(201, retResult.StatusCode);
                }

                // Username already taken
                {
                    var userForLogin = new UserForRegisterDto { Username = "avalidusername", Password = "anyoldpasword" };
                    var ret = await controller.Register(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Username is already taken", retResult.Value);
                }
            }
        }

        [Fact]
        public async void Test_Login_Validation()
        {
            var dbOptions = NewInMemoryDb("Auth_Test_Login");
            
            using (var context = new AppDbContext(dbOptions))
            {
                var authRepository = new AuthRepository(context);
                var controller = new AuthController(authRepository, _logger, _configuration);
                
                // Empty UserForLoginDto object
                {
                    var ret = await controller.Login(null);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Missing argument - Need to supply a UserForLoginDto with Username and Password", retResult.Value);
                }

                // missing username and password
                {
                    var userForLogin = new UserForLoginDto { };
                    var ret = await controller.Login(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Username must be between 2 and 50 characters", retResult.Value);
                }

                // Username too short
                {
                    var userForLogin = new UserForLoginDto { Username = "ra" };
                    var ret = await controller.Login(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Username must be between 2 and 50 characters", retResult.Value);
                }

                // Username too long
                {
                    var userForLogin = new UserForLoginDto { Username = "longname1longname2longname3longname4longname5longname6" };
                    var ret = await controller.Login(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Username must be between 2 and 50 characters", retResult.Value);
                }

                // Passsword missing
                {
                    var userForLogin = new UserForLoginDto { Username = "dummyusername" };
                    var ret = await controller.Login(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Password must be between 4 and 50 characters", retResult.Value);
                }

                // Passsword too short
                {
                    var userForLogin = new UserForLoginDto { Username = "dummyusername", Password = "min" };
                    var ret = await controller.Login(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Password must be between 4 and 50 characters", retResult.Value);
                }

                // Passsword too long
                {
                    var userForLogin = new UserForLoginDto { Username = "dummyusername", Password = "longname1longname2longname3longname4longname5longname6" };
                    var ret = await controller.Login(userForLogin);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Password must be between 4 and 50 characters", retResult.Value);
                }

                
            }
        }

        [Fact]
        public async void Test_Login_After_Register()
        {
            var dbOptions = NewInMemoryDb("Auth_Test_Login_After_Register");
            
            using (var context = new AppDbContext(dbOptions))
            {
                var authRepository = new AuthRepository(context);
                var controller = new AuthController(authRepository, _logger, _configuration);
                
                // Register a valid user 
                {
                    var userForRegister = new UserForRegisterDto { Username = "registereduser", Password = "avalidpassword"};
                    var ret = await controller.Register(userForRegister);
                    var retResult = Assert.IsType<StatusCodeResult>(ret);
                    
                    Assert.Equal(201, retResult.StatusCode);

                    var users = context.Users.ToList();

                    Assert.Single(users);
                    Assert.Equal(1, users[0].Id);
                    Assert.Equal("registereduser", users[0].Name);
                    Assert.Equal(4, users[0].RoleId);
                    Assert.Equal(2000, users[0].DailyCalories);
                    Assert.True(users[0].PasswordHash != null);
                    Assert.True(users[0].PasswordSalt != null);
                    Assert.True(users[0].PasswordHash.Length > 20);
                    Assert.True(users[0].PasswordSalt.Length > 20);
                }

                // Unregistered User
                {
                    var userForLogin = new UserForLoginDto { Username = "dummyusername", Password = "somepassword" };
                    var ret = await controller.Login(userForLogin);
                    var retResult = Assert.IsType<UnauthorizedObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Unauthorized - Invalid Username or password", retResult.Value);
                }

                // Registered User - invalid password
                {
                    var userForLogin = new UserForLoginDto { Username = "registereduser", Password = "badpassword" };
                    var ret = await controller.Login(userForLogin);
                    var retResult = Assert.IsType<UnauthorizedObjectResult>(ret);
                    
                    Assert.NotNull(retResult);
                    Assert.Equal("Unauthorized - Invalid Username or password", retResult.Value);
                }

                // Registered User with valid password
                {
                    var userForLogin = new UserForLoginDto { Username = "registereduser", Password = "avalidpassword" };
                    var ret = await controller.Login(userForLogin);
                    var retResult = Assert.IsType<OkObjectResult>(ret); // SUCCESS
                    
                    Assert.NotNull(retResult);
                    Assert.NotNull(retResult.Value);
                    Assert.Equal(270, retResult.Value.ToString().Length);
                }
            }
        }
    }
}
