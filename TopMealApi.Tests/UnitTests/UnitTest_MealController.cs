using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using X.PagedList;
using Xunit;
using TopMealApi.Context;
using TopMealApi.Controllers;
using TopMealApi.Model;

namespace TopMealApi.Tests
{
    public class UnitTest_MealController : UnitTest_ControllerBase
    {
        private readonly IConfiguration _configuration; 
        private readonly ILogger<MealController> _logger;

        public UnitTest_MealController()
        {
            var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
            var factory = serviceProvider.GetService<ILoggerFactory>();

            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            _logger = factory.CreateLogger<MealController>();
        }

        [Fact]
        public async void Test_Get()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Get");
            
            using (var context = new AppDbContext(dbOptions))
            {
                var testDate = new DateTime(2019, 10, 03);
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                {
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                    context.SaveChanges();
                
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                {
                    context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                    context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                    context.SaveChanges();

                    var ret = await controller.Get();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Meal>>(retResult.Value);

                    Assert.Empty(retValue);
                }

                // Add a single deal
                {
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 100, Description = "apple" });
                    context.SaveChanges();

                    var ret = await controller.Get();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Meal>>(retResult.Value);

                    Assert.Single(retValue);
                    Assert.Equal("apple", retValue[0].Description);
                    Assert.True(retValue[0].WithinLimit);
                }

                // Add a second deal
                {
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testDate, Calories = 120, Description = "orange" });
                    context.SaveChanges();
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Meal>>(retResult.Value);

                    Assert.Equal(2, retValue.Count());

                    Assert.Equal("apple", retValue[0].Description);
                    Assert.Equal("orange", retValue[1].Description);
                    Assert.True(retValue[0].WithinLimit);
                    Assert.True(retValue[1].WithinLimit);
                }
            }
        }

        [Fact]
        public async void Test_Remaining()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Remaining");
            
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.GetRemaining();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                {
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                    context.SaveChanges();
                
                    var ret = await controller.GetRemaining();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add user without Adminstrator Role
                {
                    context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                    context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                    context.SaveChanges();

                    var ret = await controller.GetRemaining();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<long>(retResult.Value);

                    Assert.Equal(2000, retValue);
                }

                // Add a single DailyUserCalories for our user
                {
                    var testDate = DateTime.Now.Date;
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 1500} );
                    context.SaveChanges();

                    var ret = await controller.GetRemaining();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<long>(retResult.Value);

                    Assert.Equal(500, retValue);
                }

                // Modify daily calories
                {
                    var duc = context.DailyUserCalories.First();
                    duc.Calories = 2100;
                    context.Entry(duc).State = EntityState.Modified;
                    await context.SaveChangesAsync();
                }

                // Retest
                {
                    var ret = await controller.GetRemaining();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<long>(retResult.Value);

                    Assert.Equal(0, retValue);
                }

                // Try for a different user
                {
                    var ret = await controller.GetRemaining(2);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("No permission to execute this api for other users with Role <User>", retResult.Value);
                }
            }
        }

                [Fact]
        public async void Test_Remaining_Admin()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Remaining_Admin");
            
            using (var context = new AppDbContext(dbOptions))
            {
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.GetRemaining();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                {
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                    context.SaveChanges();
                
                    var ret = await controller.GetRemaining();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add user with Adminstrator Role
                {
                    context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                    context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                    context.SaveChanges();

                    var ret = await controller.GetRemaining();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<long>(retResult.Value);

                    Assert.Equal(2000, retValue);
                }

                // Add a single DailyUserCalories for two users
                {
                    var testDate = DateTime.Now.Date;
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 1500 } );
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 2, Date = testDate, Calories = 1500 } );
                    context.SaveChanges();
                }

                // Test for first user
                {
                    var ret = await controller.GetRemaining(1);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<long>(retResult.Value);

                    Assert.Equal(500, retValue);
                }

                // Try for a different user
                {
                    var ret = await controller.GetRemaining(2);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<long>(retResult.Value);

                    Assert.Equal(500, retValue);
                }

                // Modify daily calories
                {
                    var ducs = context.DailyUserCalories.ToList();
                    ducs[0].Calories = 2100;
                    ducs[1].Calories = 2100;
                    context.Entry(ducs[0]).State = EntityState.Modified;
                    context.Entry(ducs[1]).State = EntityState.Modified;
                    await context.SaveChangesAsync();
                }

                // Retest for first user
                {
                    var ret = await controller.GetRemaining(1);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<long>(retResult.Value);

                    Assert.Equal(0, retValue);
                }

                // Retest for a different user
                {
                    var ret = await controller.GetRemaining(2);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<long>(retResult.Value);

                    Assert.Equal(0, retValue);
                }
            }
        }

        [Fact]
        public async void Test_Get_Paged()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Get_Paged");
            
            using (var context = new AppDbContext(dbOptions))
            {
                var testDate = new DateTime(2019, 10, 03);
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                {
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                    context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                    context.SaveChanges();
                
                    var ret = await controller.Get();
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                {
                    context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                    context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                    context.SaveChanges();

                    var ret = await controller.Get();
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Meal>>(retResult.Value);

                    Assert.Empty(retValue);
                }

                // Test Paged Get(1, 3)
                {
                    context.Meals.Add(new Meal { Id = 1, UserId = 1, Date = testDate, Time = testDate, Calories = 121, Description = "pear" });
                    context.Meals.Add(new Meal { Id = 2, UserId = 1, Date = testDate, Time = testDate, Calories = 122, Description = "pear" });
                    context.Meals.Add(new Meal { Id = 3, UserId = 1, Date = testDate, Time = testDate, Calories = 123, Description = "pear" });
                    context.Meals.Add(new Meal { Id = 4, UserId = 1, Date = testDate, Time = testDate, Calories = 81,  Description = "lemon" });
                    context.Meals.Add(new Meal { Id = 5, UserId = 1, Date = testDate, Time = testDate, Calories = 82,  Description = "lemon" });
                    context.Meals.Add(new Meal { Id = 6, UserId = 1, Date = testDate, Time = testDate, Calories = 83,  Description = "lemon" });
                    context.Meals.Add(new Meal { Id = 7, UserId = 1, Date = testDate, Time = testDate, Calories = 150, Description = "banana" });
                    context.Meals.Add(new Meal { Id = 8, UserId = 1, Date = testDate, Time = testDate, Calories = 150, Description = "banana" });
                    context.Meals.Add(new Meal { Id = 9, UserId = 1, Date = testDate, Time = testDate, Calories = 150, Description = "banana" });
                    context.SaveChanges();
                
                    var ret = await controller.Get(1, 3);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retMeals = Assert.IsType<PagedList<Meal>>(retResult.Value);

                    Assert.Equal(3, retMeals.Count());
                    Assert.Equal("pear", retMeals[0].Description);
                    Assert.Equal("pear", retMeals[1].Description);
                    Assert.Equal("pear", retMeals[2].Description);
                    Assert.Equal(121, retMeals.First(m => m.Id == 1).Calories);
                    Assert.Equal(122, retMeals.First(m => m.Id == 2).Calories);
                    Assert.Equal(123, retMeals.First(m => m.Id == 3).Calories);
                }

                // Test Paged Get(2, 3)
                {
                    var ret = await controller.Get(2, 3);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retMeals = Assert.IsType<PagedList<Meal>>(retResult.Value);

                    Assert.Equal(3, retMeals.Count());
                    Assert.Equal("lemon", retMeals[0].Description);
                    Assert.Equal("lemon", retMeals[1].Description);
                    Assert.Equal("lemon", retMeals[2].Description);
                    Assert.Equal(81, retMeals.First(m => m.Id == 4).Calories);
                    Assert.Equal(82, retMeals.First(m => m.Id == 5).Calories);
                    Assert.Equal(83, retMeals.First(m => m.Id == 6).Calories);
                }
            }
        }

        [Fact]
        public async void Test_GetByUserId()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_GetByUserId");

            using (var context = new AppDbContext(dbOptions))
            {
                var testDate = new DateTime(2019, 10, 03);
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.GetByUserId(1);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.GetByUserId(1);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var ret = await controller.GetByUserId(1);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Meal>>(retResult.Value);

                    Assert.Empty(retValue);
                }

                // GetByUserId(1)
                {
                    context.Meals.Add(new Meal { Id = 1, UserId = 1, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = false });
                    context.Meals.Add(new Meal { Id = 2, UserId = 2, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = true });
                    context.Meals.Add(new Meal { Id = 3, UserId = 2, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = true });
                    context.Meals.Add(new Meal { Id = 4, UserId = 1, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = false });
                    context.Meals.Add(new Meal { Id = 5, UserId = 1, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = false });
                    context.SaveChanges();
                
                    var ret = await controller.GetByUserId(1);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Meal>>(retResult.Value);

                    Assert.Equal(3, retValue.Count());
                }

                // GetByUserId(2)
                {
                    var ret = await controller.GetByUserId(2);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    Assert.Equal("No permission to execute this api for other users with Role <User>", retResult.Value);
                }
            }
        }

        [Fact]
        public async void Test_GetByUserId_Admin()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_GetByUserId_Admin");

            using (var context = new AppDbContext(dbOptions))
            {
                var testDate = new DateTime(2019, 10, 03);
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.GetByUserId(1);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.GetByUserId(1);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // GetByUserId(4)
                {
                    // Add default User with Adminstrator Role
                    context.Users.Add(new User { Id = 4, Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                    context.Users.Add(new User { Id = 5, Name = "Joe", RoleId = 2, DailyCalories = 2000 });
                    context.Meals.Add(new Meal { Id = 11, UserId = 4, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = false });
                    context.Meals.Add(new Meal { Id = 12, UserId = 5, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = true });
                    context.Meals.Add(new Meal { Id = 13, UserId = 5, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = true });
                    context.Meals.Add(new Meal { Id = 14, UserId = 4, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = false });
                    context.Meals.Add(new Meal { Id = 15, UserId = 4, Date = testDate, Time = testDate, Calories = 1500, Description = "apple pie", WithinLimit = false });
                    context.SaveChanges();
                    
                    var ret = await controller.GetByUserId(4);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Meal>>(retResult.Value);

                    Assert.Equal(3, retValue.Count());
                }

                // GetByUserId(4)
                {
                    var ret = await controller.GetByUserId(5);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Meal>>(retResult.Value);

                    Assert.Equal(2, retValue.Count());
                }
            }
        }

        [Fact]
        public async void Test_Post()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Post");

            using (var context = new AppDbContext(dbOptions))
            {
                var testDate = new DateTime(2019, 10, 03);
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();
                {
                    var ret = await controller.Post(500, "apple pie");
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.True(retMeal.WithinLimit);
                    Assert.Equal(500, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(500, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);
                }
                {
                    var ret = await controller.Post(750, "cake");
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.True(retMeal.WithinLimit);
                    Assert.Equal(750, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(1250, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);
                }
                {
                    var ret = await controller.Post(1000, "fish and chips");
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.False(retMeal.WithinLimit);
                    Assert.Equal(1000, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var meals = context.Meals.ToList();
                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(2250, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);

                    Assert.Equal(3, meals.Count());
                    Assert.True(meals.All(m => !m.WithinLimit));
                }
                {
                    var ret = await controller.Post(50);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.False(retMeal.WithinLimit);
                    Assert.Equal(50, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var meals = context.Meals.ToList();
                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(2300, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);

                    Assert.Equal(4, meals.Count());
                    Assert.True(meals.All(m => !m.WithinLimit));
                }
                {
                    var meal = new Meal { Calories = 50 };
                    var ret = await controller.Post(meal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.False(retMeal.WithinLimit);
                    Assert.Equal(50, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var meals = context.Meals.ToList();
                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(2350, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);

                    Assert.Equal(5, meals.Count());
                    Assert.True(meals.All(m => !m.WithinLimit));
                }
                {
                    var time = DateTime.Now.TruncToSecond();
                    var meal = new Meal {
                        Calories = 50,
                        Date = DateTime.Now.Date.AddDays(4),
                        Time = time };
                    var ret = await controller.Post(meal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.True(retMeal.WithinLimit);
                    Assert.Equal(DateTime.Now.Date.AddDays(4), retMeal.Date);
                    Assert.Equal(retMeal.Date, retMeal.Time.Date);
                    Assert.Equal(50, retMeal.Calories);

                    var meals = context.Meals.ToList();
                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(50, ducs[1].Calories);
                    Assert.Equal(retMeal.Date, ducs[1].Date);

                    Assert.Equal(6, meals.Count());
                    Assert.Equal(1, meals.Count(m => m.WithinLimit));
                }

                var useNutritionixInTests = _configuration.GetValue<string>("Nutritionix:UseInUnitTests");

                if (useNutritionixInTests == "true")
                {
                    var ret = await controller.Post("apple"); // Call Neutrix Service to get apple calories
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.False(retMeal.WithinLimit);
                    Assert.Equal(95, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var ducs = context.DailyUserCalories.ToList();
                    var meals = context.Meals.ToList();

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(2445, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);

                    Assert.Equal(7, meals.Count());
                }
            }
        }

        [Fact]
        public async void Test_Put_Individual()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Put_Individual");

            using (var context = new AppDbContext(dbOptions))
            {
                var testTime = new DateTime(2019, 10, 03, 16, 30, 0);
                var testDate = testTime.Date;
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });

                // Test PutIdMeal(1, meal) with unmatching Id
                {
                    // Add a Meal that we can change
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 750, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 750});
                    context.SaveChanges();

                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = 1 };
                    var ret = await controller.PutIdMeal(2, changedMeal);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Id not equal to meal.Id", retResult.Value);
                }

                // Test PutIdMeal(102, meal) with invalid Id
                {
                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = mealId + 10 };
                    var ret = await controller.PutIdMeal(mealId + 10, changedMeal);
                    var retResult = Assert.IsType<NotFoundObjectResult>(ret);

                    Assert.Equal($"Not found Id={mealId + 10}", retResult.Value);
                }

                // Test PutIdMeal(101, meal) with Invalid User Id
                {
                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = mealId, UserId = 999 };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Invalid UserId=999", retResult.Value);
                }

                // Test PutIdMeal(101, meal) with Changed User Id
                {
                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = mealId, UserId = 2 };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Only User Managers and Administrators can modify UserId. Please use your own UserId <1>", retResult.Value);
                }

                // Test PutIdMeal(101, meal) - Valid but no changed details 
                {
                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = mealId, UserId = 1 };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.Equal(testDate, retMeal.Date);
                    Assert.Equal(testTime, retMeal.Time);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(750, ducs[0].Calories);
                    Assert.Equal(testDate, ducs[0].Date);
                }

                // Test PutIdMeal(101, meal) with modified Calories
                {
                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = mealId, Calories = 850 };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.Equal(testDate, retMeal.Date);
                    Assert.Equal(testTime, retMeal.Time);
                    Assert.Equal(850, retMeal.Calories);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(850, ducs[0].Calories);
                    Assert.Equal(testDate, ducs[0].Date);
                }
            }
        }

        [Fact]
        public async void Test_Put_Id_Calories()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Put_Id_Calories");

            using (var context = new AppDbContext(dbOptions))
            {
                var testTime = new DateTime(2019, 10, 03, 16, 30, 0);
                var testDate = testTime.Date;
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });

                // Test PutIdMeal(101, meal) with modified Calories
                {
                    // Add a Meal that we can change
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 750, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 750});
                    context.SaveChanges();

                    var mealId = context.Meals.First().Id;
                    var ret = await controller.PutIdCalories(mealId, 850);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.Equal(testDate, retMeal.Date);
                    Assert.Equal(testTime, retMeal.Time);
                    Assert.Equal(850, retMeal.Calories);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(850, ducs[0].Calories);
                    Assert.Equal(testDate, ducs[0].Date);
                }
            }
        }

        [Fact]
        public async void Test_Put_Individual_UserId_Admin()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Put_Individual_UserId_Admin");

            using (var context = new AppDbContext(dbOptions))
            {
                var testTime = new DateTime(2019, 10, 03, 16, 30, 0);
                var testDate = testTime.Date;
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add user with Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });

                // Test PutIdMeal(101, meal) with modified UserId
                {
                    // Reset the Meal Data
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 750, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 750});
                    context.SaveChanges();
                    
                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = mealId, UserId = 2 };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.Equal(testDate, retMeal.Date);
                    Assert.Equal(testTime, retMeal.Time);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(1, ducs[0].UserId);
                    Assert.Equal(2, ducs[1].UserId);
                    Assert.Equal(testDate, ducs[0].Date);
                    Assert.Equal(testDate, ducs[1].Date);
                    Assert.Equal(0, ducs[0].Calories);
                    Assert.Equal(750, ducs[1].Calories);
                }
            }
        }

        [Fact]
        public async void Test_Put_Individual_Date()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Put_Individual_Date");

            using (var context = new AppDbContext(dbOptions))
            {
                var testTime = new DateTime(2019, 10, 03, 16, 30, 0);
                var testDate = testTime.Date;
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });

                // Test PutIdMeal(101, meal) with modified date
                {
                    // Reset the Meal Data
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 750, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 750});
                    context.SaveChanges();

                    var mealId = context.Meals.First().Id;
                    var newTestDate = testDate.AddDays(1);
                    var changedMeal = new Meal { Id = mealId, UserId = 1, Date = newTestDate };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.Equal(newTestDate, retMeal.Date);
                    Assert.Equal(newTestDate + testTime.TimeOfDay, retMeal.Time);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(0, ducs[0].Calories);
                    Assert.Equal(750, ducs[1].Calories);
                    Assert.Equal(testDate, ducs[0].Date);
                    Assert.Equal(newTestDate, ducs[1].Date);
                }
            }
        }

        [Fact]
        public async void Test_Put_Individual_Time()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Put_Individual_Time");

            using (var context = new AppDbContext(dbOptions))
            {
                var testTime = new DateTime(2019, 10, 03, 16, 30, 0);
                var testDate = testTime.Date;
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });

                // Test PutIdMeal(101, meal) with modified time
                {
                    // Reset the Meal Data
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 750, Description = "cake", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 750});
                    context.SaveChanges();

                    var mealId = context.Meals.First().Id;
                    var newTestTime = testTime.AddDays(1);
                    var changedMeal = new Meal { Id = mealId, UserId = 1, Time = newTestTime };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.Equal(newTestTime.Date, retMeal.Date);
                    Assert.Equal(newTestTime, retMeal.Time);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(0, ducs[0].Calories);
                    Assert.Equal(750, ducs[1].Calories);
                    Assert.Equal(testDate, ducs[0].Date);
                    Assert.Equal(newTestTime.Date, ducs[1].Date);
                }
            }
        }

        [Fact]
        public async void Test_Put_Buckets_Modify_UserId_Admin()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Put_Buckets_Modify_UserId_Admin");

            using (var context = new AppDbContext(dbOptions))
            {
                var testTime = new DateTime(2019, 10, 03, 16, 30, 0);
                var testDate = testTime.Date;
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add user with Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });

                // Test PutIdMeal(1, meal) with modified UserId
                {
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 2, Date = testDate, Time = testTime, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 2, Date = testDate, Time = testTime, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 2, Date = testDate, Time = testTime, Calories = 500, Description = "steak", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 2100 });
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 2, Date = testDate, Calories = 1500 });
                    context.SaveChanges();
                    
                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = mealId, UserId = 2 };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.False(retMeal.WithinLimit);
                    Assert.Equal(testDate, retMeal.Date);
                    Assert.Equal(testTime, retMeal.Time);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(1, ducs[0].UserId);
                    Assert.Equal(2, ducs[1].UserId);
                    Assert.Equal(testDate, ducs[0].Date);
                    Assert.Equal(testDate, ducs[1].Date);
                    Assert.Equal(1400, ducs[0].Calories);
                    Assert.Equal(2200, ducs[1].Calories);

                    var meals = context.Meals.ToList();

                    Assert.Equal(2, meals.Count(m => m.UserId == 1));
                    Assert.Equal(4, meals.Count(m => m.UserId == 2));
                    Assert.True(meals.Where(m => m.UserId == 1).All(m => m.WithinLimit));
                    Assert.True(meals.Where(m => m.UserId == 2).All(m => m.WithinLimit == false));
                }
            }
        }

        [Fact]
        public async void Test_Put_Buckets_Modify_Date()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Put_Buckets_Modify_Date");

            using (var context = new AppDbContext(dbOptions))
            {
                var testTime = new DateTime(2019, 10, 03, 16, 30, 0);
                var testDate = testTime.Date;
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });

                // Test PutIdMeal(1, meal) with modified Date
                {
                    var testTime2 = testTime.AddDays(2);
                    var testDate2 = testDate.AddDays(2);

                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 2100 });
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate2, Calories = 1500 });
                    context.SaveChanges();
                    
                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = mealId, Date = testDate2 };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.False(retMeal.WithinLimit);
                    Assert.Equal(testDate2, retMeal.Date);
                    Assert.Equal(testTime2, retMeal.Time);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(1, ducs[0].UserId);
                    Assert.Equal(1, ducs[1].UserId);
                    Assert.Equal(testDate, ducs[0].Date);
                    Assert.Equal(testDate2, ducs[1].Date);
                    Assert.Equal(1400, ducs[0].Calories);
                    Assert.Equal(2200, ducs[1].Calories);

                    var meals = context.Meals.ToList();

                    Assert.Equal(2, meals.Count(m => m.Date == testDate));
                    Assert.Equal(4, meals.Count(m => m.Date == testDate2));
                    Assert.True(meals.Where(m => m.Date == testDate).All(m => m.WithinLimit));
                    Assert.True(meals.Where(m => m.Date == testDate2).All(m => m.WithinLimit == false));
                }
            }
        }

        [Fact]
        public async void Test_Put_Buckets_Modify_Time()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Put_Buckets_Modify_Time");

            using (var context = new AppDbContext(dbOptions))
            {
                var testTime = new DateTime(2019, 10, 03, 16, 30, 0);
                var testDate = testTime.Date;
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });

                // Test PutIdMeal(1, meal) with modified Date
                {
                    var testTime2 = testTime.AddDays(2).AddHours(1);
                    var testDate2 = testTime2.Date;

                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 2100 });
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate2, Calories = 1500 });
                    context.SaveChanges();
                    
                    var mealId = context.Meals.First().Id;
                    var changedMeal = new Meal { Id = mealId, Time = testTime2 };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.False(retMeal.WithinLimit);
                    Assert.Equal(testDate2, retMeal.Date);
                    Assert.Equal(testTime2, retMeal.Time);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(1, ducs[0].UserId);
                    Assert.Equal(1, ducs[1].UserId);
                    Assert.Equal(testDate, ducs[0].Date);
                    Assert.Equal(testDate2, ducs[1].Date);
                    Assert.Equal(1400, ducs[0].Calories);
                    Assert.Equal(2200, ducs[1].Calories);

                    var meals = context.Meals.ToList();

                    Assert.Equal(2, meals.Count(m => m.Date == testDate));
                    Assert.Equal(4, meals.Count(m => m.Date == testDate2));
                    Assert.True(meals.Where(m => m.Date == testDate).All(m => m.WithinLimit));
                    Assert.True(meals.Where(m => m.Date == testDate2).All(m => m.WithinLimit == false));
                }
            }
        }

        [Fact]
        public async void Test_Put_Buckets_Modify_UserId_Time_Calories()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Put_Buckets_Modify_UserId_Time_Calories");

            using (var context = new AppDbContext(dbOptions))
            {
                var testTime = new DateTime(2019, 10, 03, 16, 30, 0);
                var testDate = testTime.Date;
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Post(50, "apple");
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add user with Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 1, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });

                // Test PutIdMeal(1, meal) with modified Date
                {
                    var testTime2 = testTime.AddDays(2).AddHours(1);
                    var testDate2 = testTime2.Date;

                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 1, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 2, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 2, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 2, Date = testDate, Time = testTime, Calories = 700, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = 2, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 2, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = 2, Date = testDate2, Time = testTime2, Calories = 500, Description = "steak", WithinLimit = true});
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 2100 });
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate2, Calories = 1500 });
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 2, Date = testDate, Calories = 2100 });
                    context.DailyUserCalories.Add(new DailyUserCalories { UserId = 2, Date = testDate2, Calories = 1500 });
                    context.SaveChanges();
                    
                    var mealId = context.Meals.First().Id;
                    var userId = context.Meals.First().UserId;
                    var userId2 = context.Meals.First().UserId == 1 ? 2 : 1;

                    var changedMeal = new Meal { Id = mealId, UserId = userId2, Time = testTime2, Calories = 900 };
                    var ret = await controller.PutIdMeal(mealId, changedMeal);
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.False(retMeal.WithinLimit);
                    Assert.Equal(testDate2, retMeal.Date);
                    Assert.Equal(testTime2, retMeal.Time);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Equal(4, ducs.Count());
                    Assert.Equal(userId,  ducs[0].UserId);
                    Assert.Equal(userId,  ducs[1].UserId);
                    Assert.Equal(userId2, ducs[2].UserId);
                    Assert.Equal(userId2, ducs[3].UserId);
                    Assert.Equal(testDate,  ducs[0].Date);
                    Assert.Equal(testDate2, ducs[1].Date);
                    Assert.Equal(testDate,  ducs[2].Date);
                    Assert.Equal(testDate2, ducs[3].Date);
                    Assert.Equal(1400, ducs[0].Calories);
                    Assert.Equal(1500, ducs[1].Calories);
                    Assert.Equal(2100, ducs[2].Calories);
                    Assert.Equal(2400, ducs[3].Calories);

                    var meals = context.Meals.ToList();

                    Assert.Equal(2, meals.Count(m => m.UserId == userId && m.Date == testDate));
                    Assert.Equal(3, meals.Count(m => m.UserId == userId && m.Date == testDate2));
                    Assert.Equal(3, meals.Count(m => m.UserId == userId2 && m.Date == testDate));
                    Assert.Equal(4, meals.Count(m => m.UserId == userId2 && m.Date == testDate2));

                    Assert.True(meals.Where(m => m.UserId == userId  && m.Date == testDate).All(m => m.WithinLimit));
                    Assert.True(meals.Where(m => m.UserId == userId  && m.Date == testDate2).All(m => m.WithinLimit));
                    Assert.True(meals.Where(m => m.UserId == userId2 && m.Date == testDate).All(m => m.WithinLimit == false));
                    Assert.True(meals.Where(m => m.UserId == userId2 && m.Date == testDate2).All(m => m.WithinLimit == false));
                }
            }
        }

        [Fact]
        public async void Test_Delete()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Delete");

            using (var context = new AppDbContext(dbOptions))
            {
                var testDate = new DateTime(2019, 10, 03);
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Delete(50);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Delete(50);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();

                // Try to delete non existant meal
                {
                    var ret = await controller.Delete(9999);
                    var retResult = Assert.IsType<NotFoundResult>(ret);
                }

                // Add Two meals
                context.Meals.Add(new Meal { Id = 101, UserId = 1, Date = testDate, Time = testDate, Calories = 750, Description = "cake", WithinLimit = false });
                context.Meals.Add(new Meal { Id = 102, UserId = 1, Date = testDate, Time = testDate, Calories = 1500, Description = "burger and chips", WithinLimit = false });
                context.Meals.Add(new Meal { Id = 103, UserId = 2, Date = testDate, Time = testDate, Calories = 1600, Description = "burger and chips", WithinLimit = true });
                context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 2250} );
                context.DailyUserCalories.Add(new DailyUserCalories { UserId = 2, Date = testDate, Calories = 1600} );
                context.SaveChanges();

                // Test Delete other users meal
                {
                    var ret = await controller.Delete(103);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Only User Managers and Administrators can delete other users deals", retResult.Value);
                }

                // Test Delete(101)
                {
                    var ret = await controller.Delete(101);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.True(retMeal.WithinLimit);   // WithinLimit should be changed by API
                    Assert.Equal(750, retMeal.Calories);
                    Assert.Equal("cake", retMeal.Description);
                    Assert.Equal(testDate, retMeal.Date);

                    var remainingmeals = context.Meals.ToList();

                    Assert.Equal(2, remainingmeals.Count());
                    Assert.Equal(102, remainingmeals[0].Id);
                    Assert.True(remainingmeals[0].WithinLimit);

                    var ducs = context.DailyUserCalories.ToList();  // WithinLimit should be changed by API

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(1, ducs[0].UserId);
                    Assert.Equal(2, ducs[1].UserId);
                    Assert.Equal(testDate, ducs[0].Date);
                    Assert.Equal(testDate, ducs[1].Date);
                    Assert.Equal(1500, ducs[0].Calories);
                    Assert.Equal(1600, ducs[1].Calories);
                }

                // Test Delete(102)
                {
                    var ret = await controller.Delete(102);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.True(retMeal.WithinLimit);
                    Assert.Equal(1500, retMeal.Calories);
                    Assert.Equal("burger and chips", retMeal.Description);
                    Assert.Equal(testDate, retMeal.Date);

                    var remainingmeals = context.Meals.ToList();

                    Assert.Single(remainingmeals);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Equal(2, ducs.Count());
                    Assert.Equal(1, ducs[0].UserId);
                    Assert.Equal(2, ducs[1].UserId);
                    Assert.Equal(testDate, ducs[0].Date);
                    Assert.Equal(testDate, ducs[1].Date);
                    Assert.Equal(0, ducs[0].Calories);
                    Assert.Equal(1600, ducs[1].Calories);
                }
            }
        }

        [Fact]
        public async void Test_Delete_All_By_User_Id()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Delete_All_By_User_Id");

            using (var context = new AppDbContext(dbOptions))
            {
                var testDate = new DateTime(2019, 10, 03);
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);

                // Empty database
                {
                    var ret = await controller.Delete(50);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Delete(50);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();

                // Try to delete non existant meal
                {
                    var ret = await controller.Delete(9999);
                    var retResult = Assert.IsType<NotFoundResult>(ret);
                }

                var testDate2 = testDate.AddDays(1);
                // Add Two meals
                context.Meals.Add(new Meal { Id = 101, UserId = 1, Date = testDate, Time = testDate, Calories = 750, Description = "cake", WithinLimit = false });
                context.Meals.Add(new Meal { Id = 102, UserId = 1, Date = testDate, Time = testDate, Calories = 1500, Description = "burger and chips", WithinLimit = false });
                context.Meals.Add(new Meal { Id = 103, UserId = 1, Date = testDate2, Time = testDate2, Calories = 1500, Description = "burger and chips", WithinLimit = false });
                context.Meals.Add(new Meal { Id = 104, UserId = 2, Date = testDate, Time = testDate, Calories = 1600, Description = "burger and chips", WithinLimit = true });
                context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate, Calories = 2250} );
                context.DailyUserCalories.Add(new DailyUserCalories { UserId = 1, Date = testDate2, Calories = 1500} );
                context.DailyUserCalories.Add(new DailyUserCalories { UserId = 2, Date = testDate, Calories = 1600} );
                context.SaveChanges();

                // Test Delete other users meal
                {
                    var ret = await controller.DeleteAllByUserId(2);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal("Only User Managers and Administrators can delete other users deals", retResult.Value);
                }

                // Test Delete(1)
                {
                    var ret = await controller.DeleteAllByUserId(1);
                    var retResult = Assert.IsType<OkObjectResult>(ret);

                    Assert.Equal("Deleted 3 meals for UserId 1", retResult.Value);

                    var remainingmeals = context.Meals.ToList();

                    Assert.Single(remainingmeals);
                    Assert.Equal(104, remainingmeals[0].Id);
                    Assert.Equal(2, remainingmeals[0].UserId);
                    Assert.True(remainingmeals[0].WithinLimit);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(2, ducs[0].UserId);
                    Assert.Equal(testDate, ducs[0].Date);
                    Assert.Equal(1600, ducs[0].Calories);
                }
            }
        }

        [Fact]
        public async void Test_Delete_Following_Post()
        {
            var dbOptions = NewInMemoryDb("Meal_Test_Delete_Following_Post");

            using (var context = new AppDbContext(dbOptions))
            {
                var testDate = new DateTime(2019, 10, 03);
                var controller = new MealController(_configuration, context, _logger, DefaultTestUser);
                var idToDelete = 0;

                // Empty database
                {
                    var ret = await controller.Delete(50);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);
                    
                    Assert.Equal(Role.AdministratorNotSetUp, retResult.Value);
                }
                
                // Add Administrator and User Roles
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.Administrator)});
                context.Roles.Add(new Role { Name = nameof(TestRoleEnum.User)});
                context.SaveChanges();
                {
                    var ret = await controller.Delete(50);
                    var retResult = Assert.IsType<BadRequestObjectResult>(ret);

                    Assert.Equal($"Invalid User <{DefaultTestUser}> - please contact your administrator", retResult.Value);
                }

                // Add two users without Adminstrator Role
                context.Users.Add(new User { Name = DefaultTestUser, RoleId = 2, DailyCalories = 2000 });
                context.Users.Add(new User { Name = "Fred", RoleId = 2, DailyCalories = 2000 });
                context.SaveChanges();

                // Now Add some Meals using Post API method so we can test DailyuserCalories table
                {
                    var ret = await controller.Post(500, "apple pie");
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.True(retMeal.WithinLimit);
                    Assert.Equal(500, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(500, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);
                }
                {
                    var ret = await controller.Post(750, "cake");
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.True(retMeal.WithinLimit);
                    Assert.Equal(750, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(1, ducs[0].UserId);
                    Assert.Equal(1250, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);
                    idToDelete = retMeal.Id;
                }
                {
                    var ret = await controller.Post(1000, "fish and chips");
                    var retResult = Assert.IsType<CreatedAtActionResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.False(retMeal.WithinLimit);
                    Assert.Equal(1000, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var meals = context.Meals.ToList();
                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(1, ducs[0].UserId);
                    Assert.Equal(2250, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);

                    Assert.Equal(3, meals.Count());
                    Assert.True(meals.All(m => !m.WithinLimit));
                }

                // Test Delete({id}) and check the DailyuserCalories table
                {
                    Assert.True(idToDelete > 0);

                    var ret = await controller.Delete(idToDelete);
                    var retResult = Assert.IsType<OkObjectResult>(ret);
                    var retMeal = Assert.IsType<Meal>(retResult.Value);

                    Assert.NotNull(retMeal);
                    Assert.True(retMeal.WithinLimit);
                    Assert.Equal(750, retMeal.Calories);
                    Assert.Equal(DateTime.Now.Date, retMeal.Date);

                    var meals = context.Meals.ToList();
                    var ducs = context.DailyUserCalories.ToList();

                    Assert.Single(ducs);
                    Assert.Equal(1500, ducs[0].Calories);
                    Assert.Equal(DateTime.Now.Date, ducs[0].Date);

                    Assert.Equal(2, meals.Count());
                    Assert.True(meals.All(m => m.WithinLimit));
                }
            }
        }
    }
}
