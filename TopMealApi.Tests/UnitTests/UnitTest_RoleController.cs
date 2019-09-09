using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using X.PagedList;
using Xunit;
using TopMealApi.Context;
using TopMealApi.Model;
using TopMealApi.Controllers;

namespace TopMealApi.Tests
{
    public class UnitTest_RoleController : UnitTest_ControllerBase
    {
        private readonly IConfiguration _configuration; 
        private readonly ILogger<RoleController> _logger;

        public UnitTest_RoleController()
        {
            var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
            var factory = serviceProvider.GetService<ILoggerFactory>();
            
            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            _logger = factory.CreateLogger<RoleController>();
        }

        [Fact]
        public async void Test_Get()
        {
            var dbOptions = NewInMemoryDb("Role_Test_Get");
            
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new RoleController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Role <User> is not authorized to run this API. Must be one of the following roles : Administrator", retResult.Value);
                }
            }
        }

        [Fact]
        public async void Test_Get_Admin()
        {
            var dbOptions = NewInMemoryDb("Role_Test_Get_Admin");
            
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new RoleController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User with Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.SaveChanges();

                // Test Get()
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Role>>(retResult.Value);

                    Assert.Equal(2, retValue.Count());
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == nameof(TestRoleEnum.Administrator)));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == nameof(TestRoleEnum.User)));
                }

                // Add a third Role and retest
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.UserManager) });
                context.SaveChanges();

                // Test Get()
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Role>>(retResult.Value);

                    Assert.Equal(3, retValue.Count());
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == nameof(TestRoleEnum.Administrator)));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == nameof(TestRoleEnum.User)));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == nameof(TestRoleEnum.UserManager)));
                }

                    // Add a 4th role roles to test paged
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.DataManager) });
                context.SaveChanges();

                // Test Paged Get(1, 2)
                {
                    var ret = await controller.Get(1, 2);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<PagedList<Role>>(retResult.Value);

                    Assert.Equal(2, retValue.Count());
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == nameof(TestRoleEnum.Administrator)));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == nameof(TestRoleEnum.User)));
                }

                // Test Paged Get(2, 2)
                {
                    var ret = await controller.Get(2, 2);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<PagedList<Role>>(retResult.Value);

                    Assert.Equal(2, retValue.Count());
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == nameof(TestRoleEnum.UserManager)));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == nameof(TestRoleEnum.DataManager)));
                }
            }
        }

        [Fact]
        public async void Test_Post()
        {
            var dbOptions = NewInMemoryDb("Role_Test_Post");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new RoleController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var newRole = new Role();
                    var ret = await controller.Post(newRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var newRole = new Role();
                    var ret = await controller.Post(newRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User (NOT adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var newRole = new Role();
                    var ret = await controller.Post(newRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Role <User> is not authorized to run this API. Must be one of the following roles : Administrator", retResult.Value);
                }
            }
        }

        [Fact]
        public async void Test_Post_Admin()
        {
            var dbOptions = NewInMemoryDb("Role_Test_Post_Admin");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new RoleController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var newRole = new Role();
                    var ret = await controller.Post(newRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var newRole = new Role();
                    var ret = await controller.Post(newRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User (Adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var newRole = new Role();   // Create Role with no Name
                    var ret = await controller.Post(newRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Role Name must be supplied", retResult.Value);
                }

                // Test Post()
                {
                    var newRole = new Role { Name = nameof(TestRoleEnum.UserManager) }; // Create Role with Name
                    var ret = await controller.Post(newRole);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retRole = Assert.IsType<Role>(retResult.Value);

                    Assert.NotNull(retRole);
                    Assert.Equal(nameof(TestRoleEnum.UserManager), retRole.Name);
                }

                // Test Post("{name}")
                {
                    var ret = await controller.Post(nameof(TestRoleEnum.DataManager));
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retRole = Assert.IsType<Role>(retResult.Value);

                    Assert.NotNull(retRole);
                    Assert.Equal(nameof(TestRoleEnum.DataManager), retRole.Name);
                }
            }
        }

        [Fact]
        public async void Test_Put()
        {
            var dbOptions = NewInMemoryDb("Role_Test_Put");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new RoleController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var testRole = new Role { Id = 33 };
                    var ret = await controller.Put(33, testRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var testRole = new Role { Id = 33 };
                    var ret = await controller.Put(2, testRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User (NOT adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var testRole = new Role { Id = 33 };
                    var ret = await controller.Put(2, testRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Role <User> is not authorized to run this API. Must be one of the following roles : Administrator", retResult.Value);
                }
            }
        }

        [Fact]
        public async void Test_Put_Admin()
        {
            var dbOptions = NewInMemoryDb("Role_Test_Put_Admin");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new RoleController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var testRole = new Role { Id = 33 };
                    var ret = await controller.Put(33, testRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var testRole = new Role { Id = 33 };
                    var ret = await controller.Put(2, testRole);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User (Adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var testRole = new Role { Id = 33 };
                    var ret = await controller.Put(2, testRole);  // Test Put(2, {Role 33})
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Id=2 not equal to role.Id=33", retResult.Value);
                }

                // Test Put(2, {Role 2})
                {
                    var testRole = new Role { Id = 2, Name = "ChangedName" }; // Create Role with Name
                    var ret = await controller.Put(2, testRole);
                    var retResult = Assert.IsType<NoContentResult>(ret);

                    Assert.NotNull(retResult);

                    var changedRole = context.Roles.FirstOrDefault(r => r.Id == 2);

                    Assert.NotNull(changedRole);
                    Assert.Equal("ChangedName", changedRole.Name);
                }
            }
        }
        
        [Fact]
        public async void Test_Delete()
        {
            var dbOptions = NewInMemoryDb("Role_Test_Delete");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new RoleController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Delete(33);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Delete(33);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User (NOT adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var ret = await controller.Delete(33);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Role <User> is not authorized to run this API. Must be one of the following roles : Administrator", retResult.Value);
                }
            }
        }

        [Fact]
        public async void Test_Delete_Admin()
        {
            var dbOptions = NewInMemoryDb("Role_Test_Delete_Admin");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new RoleController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Delete(33);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Delete(33);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User (Adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var ret = await controller.Delete(33);
                    var retResult = Assert.IsType<NotFoundResult>(ret);

                    Assert.NotNull(retResult);
                }

                // Test Delete(1)
                {
                    var ret = await controller.Delete(1);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Cannot delete Role with Id 1 - Some Users are still assigned this RoleId", retResult.Value);
                }

                // Test Delete(2)
                {
                    var ret = await controller.Delete(2);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retRole = Assert.IsType<Role>(retResult.Value);

                    Assert.NotNull(retRole);
                    Assert.Equal(nameof(TestRoleEnum.User), retRole.Name);
                }
            }
        }
    }
}
