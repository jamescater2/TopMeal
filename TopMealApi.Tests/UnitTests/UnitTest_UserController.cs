using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using Xunit;
using TopMealApi.Context;
using TopMealApi.Controllers;
using TopMealApi.Model;
using System;

namespace TopMealApi.Tests
{
    public class UnitTest_UserController : UnitTest_ControllerBase
    {
        private readonly IConfiguration _configuration; 
        private readonly ILogger<UserController> _logger;

        public UnitTest_UserController()
        {
            var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
            var factory = serviceProvider.GetService<ILoggerFactory>();
            
             _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            _logger = factory.CreateLogger<UserController>();
        }

        [Fact]
        public async void Test_Get()
        {
            var dbOptions = NewInMemoryDb("User_Test_Get");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<User>>(retResult.Value);

                    Assert.Single(retValue);
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == DefaultTestUser));
                }
            }
        }

        [Fact]
        public async void Test_Get_Admin()
        {
            var dbOptions = NewInMemoryDb("User_Test_Get_Admin");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User with Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "joe", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();

                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<User>>(retResult.Value);

                    Assert.Equal(2, retValue.Count());
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == DefaultTestUser));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == "joe"));
                }


                var getById_Id = 0;

                // Add a third User and retest
                context.Users.Add(new User { Name = "harry22", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();

                // Test Get()
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<User>>(retResult.Value);

                    Assert.Equal(3, retValue.Count());
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == DefaultTestUser));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == "joe"));

                    var userHarry22 = retValue.FirstOrDefault(r => r.Name == "harry22");

                    Assert.NotNull(userHarry22);
                    getById_Id = userHarry22.Id;
                }

                // Test GetById({id})
                {
                    var ret = await controller.GetById(getById_Id);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<User>(retResult.Value);

                    Assert.NotNull(retValue);
                    Assert.Equal(getById_Id, retValue.Id);
                    Assert.Equal("harry22", retValue.Name);
                }

                // Add more users to test paged
                context.Users.Add(new User { Name = "fred", RoleId = 4, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "jenny", RoleId = 4, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "albert", RoleId = 4, DailyCalories = 2000 });
                context.SaveChanges();

                // Test Paged Get(1, 3)
                {
                    var ret = await controller.Get(null, 1, 3);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<PagedList<User>>(retResult.Value);

                    Assert.Equal(3, retValue.Count());
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == DefaultTestUser));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == "joe"));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == "harry22"));
                }

                // Test Paged Get(2, 3)
                {
                    var ret = await controller.Get(null, 2, 3);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<PagedList<User>>(retResult.Value);

                    Assert.Equal(3, retValue.Count());
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == "fred"));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == "jenny"));
                    Assert.NotNull(retValue.FirstOrDefault(r => r.Name == "albert"));
                }
            }
        }
        

        [Fact]
        public async void Test_Post()
        {
            var dbOptions = NewInMemoryDb("User_Test_Post");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var newUser = new User();
                    var ret = await controller.Post(newUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var newUser = new User();
                    var ret = await controller.Post(newUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User (NOT adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var newUser = new User();
                    var ret = await controller.Post(newUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Role <User> is not authorized to run this API. Must be one of the following roles : Administrator, UserManager, DataManager", retResult.Value);
                }
            }
        }

        [Fact]
        public async void Test_Post_Admin()
        {
            var dbOptions = NewInMemoryDb("User_Test_Post_Admin");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var newUser = new User();
                    var ret = await controller.Post(newUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var newUser = new User();
                    var ret = await controller.Post(newUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User (Adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var newUser = new User();   // Create Role with no Name
                    var ret = await controller.Post(newUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("User Name must be supplied", retResult.Value);
                }

                // Test Post()
                {
                    var newUser = new User { Name = nameof(TestRoleEnum.UserManager) }; // Create Role with Name
                    var ret = await controller.Post(newUser);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retRole = Assert.IsType<User>(retResult.Value);

                    Assert.NotNull(retRole);
                    Assert.Equal(nameof(TestRoleEnum.UserManager), retRole.Name);
                }
            }
        }

        [Fact]
        public async void Test_Put()
        {
            var dbOptions = NewInMemoryDb("User_Test_Put");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var testUser = new User { Id = 33 };
                    var ret = await controller.Put(33, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var testUser = new User { Id = 33 };
                    var ret = await controller.Put(2, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two default User (NOT adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "SecondUser", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var testUser = new User { Id = 33 };
                    var ret = await controller.Put(4, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Id=4 not equal to role.Id=33", retResult.Value);
                }

                {
                    var testUser = new User { Id = 4 };
                    var ret = await controller.Put(4, testUser);
                    var retResult = Assert.IsType<NotFoundResult>(ret);
                }

                {
                    var testUser = new User { Id = 2 };
                    var ret = await controller.Put(2, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("You are not authorized to modify other users details", retResult.Value);
                }

                {
                    var testUser = new User { Id = 1, Name = "ThirdUser" };
                    var ret = await controller.Put(1, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("You cannot modify User Names", retResult.Value);
                }

                // Test Put(1, {Role 1})
                {
                    var testUser = new User { Id = 1, DailyCalories = 2500 };
                    var ret = await controller.Put(1, testUser);
                    var retResult = Assert.IsType<NoContentResult>(ret);

                    Assert.NotNull(retResult);

                    var changedUser = context.Users.FirstOrDefault(r => r.Id == 1);

                    Assert.NotNull(changedUser);
                    Assert.Equal(2500, changedUser.DailyCalories);
                }
            }
        }

        [Fact]
        public async void Test_Put_Admin()
        {
            var dbOptions = NewInMemoryDb("User_Test_Put_Admin");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var testUser = new User { Id = 33 };
                    var ret = await controller.Put(33, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var testUser = new User { Id = 33 };
                    var ret = await controller.Put(2, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add default User (Adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Jenny", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var testUser = new User { Id = 33 };
                    var ret = await controller.Put(2, testUser);  // Test Put(2, {Role 33})
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Id=2 not equal to role.Id=33", retResult.Value);
                }

                // Test Put(2, {Role 2})
                {
                    var testUser = new User { Id = 2, Name = "Fred" }; // Create Role with new Name
                    var ret = await controller.Put(2, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("You cannot modify User Names", retResult.Value);
                }

                // Test Put(2, {Role 2})
                {
                    var testUser = new User { Id = 2, DailyCalories = 1500 }; // Create Role with Name
                    var ret = await controller.Put(2, testUser);
                    var retResult = Assert.IsType<NoContentResult>(ret);

                    Assert.NotNull(retResult);

                    var changedUser = context.Users.FirstOrDefault(r => r.Id == 2);

                    Assert.NotNull(changedUser);
                    Assert.Equal(1500, changedUser.DailyCalories);
                }
            }
        }
        
        [Fact]
        public async void Test_Put_Modify_Daily_Calories()
        {
            var dbOptions = NewInMemoryDb("User_Test_Put_Modify_Daily_Calories(");

            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var testUser = new User { Id = 33 };
                    var ret = await controller.Put(33, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Role
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var testUser = new User { Id = 33 };
                    var ret = await controller.Put(2, testUser);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two default User (NOT adminstrator)
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "SecondUser", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();

                // Add test date - Meals and DailyUserCalories for our user
                {
                    var testDate = new DateTime(2019, 10, 03);

                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 50, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 50, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 100 });

                    testDate = testDate.AddDays(1);
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 100, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 100, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 200 });

                    testDate = testDate.AddDays(1);
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 150, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 150, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 300 });

                    testDate = testDate.AddDays(1);
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 200, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 200, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 400 });

                    testDate = testDate.AddDays(1);
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 250, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 250, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 500 });

                    testDate = testDate.AddDays(1);
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 300, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 300, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 600 });

                    testDate = testDate.AddDays(1);
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 350, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 350, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 700 });

                    testDate = testDate.AddDays(1);
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 400, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 400, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 800 });

                    testDate = testDate.AddDays(1);
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 450, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 450, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 900 });

                    testDate = testDate.AddDays(1);
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 500, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 500, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 1000 });
                    context.SaveChanges();
                }

                // Check the inital state of the meals and DailyUserCalories
                {
                    Assert.Equal(20, context.Meals.Count());
                    Assert.Equal(20, context.Meals.Count(m => m.WithinLimit));
                    Assert.Equal(10, context.DailyUserCalories.Count());
                    Assert.Equal(10, context.DailyUserCalories.Count(duc => duc.Calories < 1001));
                }

                // Put the user with changed DailyCalories
                {
                    var testUser = new User { Id = 1, DailyCalories = 1500 };
                    var ret = await controller.Put(1, testUser);
                    var retResult = Assert.IsType<NoContentResult>(ret);

                    Assert.NotNull(retResult);
                    Assert.Equal(20, context.Meals.Count(m => m.WithinLimit));
                    Assert.Equal(20, context.Meals.Count(m => m.WithinLimit && m.Calories < 750));
                }

                {
                    var testUser = new User { Id = 1, DailyCalories = 1000 };
                    var ret = await controller.Put(1, testUser);
                    var retResult = Assert.IsType<NoContentResult>(ret);

                    Assert.NotNull(retResult);
                    Assert.Equal(18, context.Meals.Count(m => m.WithinLimit));
                    Assert.Equal(18, context.Meals.Count(m => m.WithinLimit && m.Calories < 500));
                }

                {
                    var testUser = new User { Id = 1, DailyCalories = 100 };
                    var ret = await controller.Put(1, testUser);
                    var retResult = Assert.IsType<NoContentResult>(ret);

                    Assert.NotNull(retResult);
                    Assert.Equal(0, context.Meals.Count(m => m.WithinLimit));
                    Assert.Equal(0, context.Meals.Count(m => m.WithinLimit && m.Calories < 50));
                }

                {
                    var testUser = new User { Id = 1, DailyCalories = 200 };
                    var ret = await controller.Put(1, testUser);
                    var retResult = Assert.IsType<NoContentResult>(ret);

                    Assert.NotNull(retResult);
                    Assert.Equal(2, context.Meals.Count(m => m.WithinLimit));
                    Assert.Equal(2, context.Meals.Count(m => m.WithinLimit && m.Calories < 100));
                }

                {
                    var testUser = new User { Id = 1, DailyCalories = 500 };
                    var ret = await controller.Put(1, testUser);
                    var retResult = Assert.IsType<NoContentResult>(ret);

                    Assert.NotNull(retResult);
                    Assert.Equal(8, context.Meals.Count(m => m.WithinLimit));
                    Assert.Equal(8, context.Meals.Count(m => m.WithinLimit && m.Calories < 250));
                }

                {
                    var testUser = new User { Id = 1, DailyCalories = 800 };
                    var ret = await controller.Put(1, testUser);
                    var retResult = Assert.IsType<NoContentResult>(ret);

                    Assert.NotNull(retResult);
                    Assert.Equal(14, context.Meals.Count(m => m.WithinLimit));
                    Assert.Equal(14, context.Meals.Count(m => m.WithinLimit && m.Calories < 400));
                }
            }
        }
        
        [Fact]
        public async void Test_Delete()
        {
            var dbOptions = NewInMemoryDb("User_Test_Delete");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

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

                    Assert.Equal("Role <User> is not authorized to run this API. Must be one of the following roles : Administrator, UserManager, DataManager", retResult.Value);
                }
            }
        }

        [Fact]
        public async void Test_Delete_Admin()
        {
            var dbOptions = NewInMemoryDb("User_Test_Delete_Admin");
                
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

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
                context.Users.Add(new User { Name = "FredDel", RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "JoeDel", RoleId = 2, DailyCalories = 2000 });

                context.Meals.Add(new Meal { UserId = 2, Calories = 1000, Description = "large cake" });
                context.SaveChanges();

                {
                    var ret = await controller.Delete(33);
                    var retResult = Assert.IsType<NotFoundResult>(ret);

                    Assert.NotNull(retResult);
                }

                // Test Delete(2)
                {
                    var ret = await controller.Delete(2);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Cannot delete User <FredDel> with Id <2> until all their meals have been deleted", retResult.Value);
                }

                // Test Delete(3)
                {
                    var ret = await controller.Delete(3);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retUser = Assert.IsType<User>(retResult.Value);

                    Assert.NotNull(retUser);
                    Assert.Equal("JoeDel", retUser.Name);
                    Assert.Equal(2, retUser.RoleId);
                }
            }
        }
    }
}
