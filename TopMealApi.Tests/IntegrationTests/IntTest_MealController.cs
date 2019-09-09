using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using X.PagedList;
using Xunit;
using TopMealApi.Context;
using TopMealApi.Controllers;
using TopMealApi.Model;

namespace TopMealApi.Tests
{
    public class IntTest_MealController : UnitTest_ControllerBase
    {
        private readonly IConfiguration _configuration; 
        private readonly ILogger<MealController> _logger;
        private readonly DbContextOptions<AppDbContext> _dbOptions;

        public IntTest_MealController()
        {
            var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
            var factory = serviceProvider.GetService<ILoggerFactory>();
            
            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            _logger = factory.CreateLogger<MealController>();
            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(_configuration.GetValue<string>("Data:CommandAPIConnection:ConnectionString"))
                    .Options;
        }

        [Fact]
        public async void Test_GetFilter()
        {
            using (var context = new AppDbContext(_dbOptions))
            {
                var testUserName = "inttestusermeal";

                try
                {
                    var testUser = await context.GetOrCreateTestUserAsync(testUserName);
                    Assert.NotNull(testUser);
                    var controller = new MealController(_configuration, context, _logger, testUserName);
                    var tTime1 = new DateTime(2019, 10, 03, 16, 30, 0);
                    var tDate1 = tTime1.Date;
                    var tTime2 = tTime1.AddDays(2);
                    var tDate2 = tTime2.Date;
                    var tId = testUser.Id;

                    context.Meals.Add(new Meal { UserId = tId, Date = tDate1, Time = tTime1, Calories = 500, Description = "bun", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = tId, Date = tDate1, Time = tTime1, Calories = 600, Description = "cake", WithinLimit = true});
                    context.Meals.Add(new Meal { UserId = tId, Date = tDate1, Time = tTime1, Calories = 700, Description = "curry", WithinLimit = true});

                    context.Meals.Add(new Meal { UserId = tId, Date = tDate2, Time = tTime2, Calories = 500, Description = "bun", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = tId, Date = tDate2, Time = tTime2, Calories = 500, Description = "bun", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = tId, Date = tDate2, Time = tTime2, Calories = 600, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = tId, Date = tDate2, Time = tTime2, Calories = 600, Description = "cake", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = tId, Date = tDate2, Time = tTime2, Calories = 700, Description = "curry", WithinLimit = false});
                    context.Meals.Add(new Meal { UserId = tId, Date = tDate2, Time = tTime2, Calories = 700, Description = "curry", WithinLimit = false});
                    await context.SaveChangesAsync();

                    {
                        var ret = await controller.GetByUserId(tId);
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<List<Meal>>(retType.Value);

                        Assert.Equal(9, retValue.Count());
                    }
                    {
                        var ret = await controller.GetFilter($"UserId = {tId}");
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<List<Meal>>(retType.Value);

                        Assert.Equal(9, retValue.Count());
                        Assert.Equal(9, retValue.Count(m => m.UserId == tId));
                    }
                    {
                        var ret = await controller.GetFilter($"UserId = {tId} AND (Calories gt 500 AND Calories le 700)");
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<List<Meal>>(retType.Value);

                        Assert.Equal(6, retValue.Count());
                        Assert.Equal(6, retValue.Count(m => m.UserId == tId && (m.Calories > 500 && m.Calories <= 700)));
                    }
                    {
                        var ret = await controller.GetFilter($"UserId = {tId} AND ((Calories gt 500 AND Calories le 700) || WithinLimit = 1)");
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<List<Meal>>(retType.Value);

                        Assert.Equal(7, retValue.Count());
                        Assert.Equal(7, retValue.Count(m => m.UserId == tId && ((m.Calories > 500 && m.Calories <= 700) || m.WithinLimit)));
                    }

                    // Test paged filtered queries
                    {
                        var ret = await controller.GetFilter($"UserId = {tId}", 1, 5);
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<PagedList<Meal>>(retType.Value);

                        Assert.Equal(5, retValue.Count());
                        Assert.Equal(5, retValue.Count(m => m.UserId == tId));
                    }
                    {
                        var ret = await controller.GetFilter($"UserId = {tId}", 2, 5);
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<PagedList<Meal>>(retType.Value);

                        Assert.Equal(4, retValue.Count());
                        Assert.Equal(4, retValue.Count(m => m.UserId == tId));
                    }

                    {
                        var ret = await controller.GetFilter($"UserId = {tId} AND (Calories gt 500 AND Calories le 700)", 1, 4);
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<PagedList<Meal>>(retType.Value);

                        Assert.Equal(4, retValue.Count());
                        Assert.Equal(4, retValue.Count(m => m.UserId == tId && (m.Calories > 500 && m.Calories <= 700)));
                    }
                    {
                        var ret = await controller.GetFilter($"UserId = {tId} AND (Calories gt 500 AND Calories le 700)", 2, 4);
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<PagedList<Meal>>(retType.Value);

                        Assert.Equal(2, retValue.Count());
                        Assert.Equal(2, retValue.Count(m => m.UserId == tId && (m.Calories > 500 && m.Calories <= 700)));
                    }

                    {
                        var ret = await controller.GetFilter($"UserId = {tId} AND ((Calories gt 500 AND Calories le 700) || WithinLimit = 1)", 1, 4);
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<PagedList<Meal>>(retType.Value);

                        Assert.Equal(4, retValue.Count());
                        Assert.Equal(4, retValue.Count(m => m.UserId == tId && ((m.Calories > 500 && m.Calories <= 700) || m.WithinLimit)));
                    }
                    {
                        var ret = await controller.GetFilter($"UserId = {tId} AND ((Calories gt 500 AND Calories le 700) || WithinLimit = 1)", 2, 4);
                        var retType = Assert.IsType<OkObjectResult>(ret);
                        var retValue = Assert.IsType<PagedList<Meal>>(retType.Value);

                        Assert.Equal(3, retValue.Count());
                        Assert.Equal(3, retValue.Count(m => m.UserId == tId && ((m.Calories > 500 && m.Calories <= 700) || m.WithinLimit)));
                    }
                }
                finally
                {
                    await context.DeleteAllDataForTestUserAsync(testUserName);
                }
            }
        }
    }
}
