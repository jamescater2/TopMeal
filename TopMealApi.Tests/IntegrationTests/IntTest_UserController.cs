using System.Linq;
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
    public class IntTest_UserController : UnitTest_ControllerBase
    {
        private readonly IConfiguration _configuration; 
        private readonly ILogger<UserController> _logger;
        private readonly DbContextOptions<AppDbContext> _dbOptions;

        public IntTest_UserController()
        {
            var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
            var factory = serviceProvider.GetService<ILoggerFactory>();
            
            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            _logger = factory.CreateLogger<UserController>();
            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(_configuration.GetValue<string>("Data:CommandAPIConnection:ConnectionString"))
                    .Options;
        }

        [Fact]
        public async void Test_GetFilter()
        {
            using (var context = new AppDbContext(_dbOptions))
            {
                var controller = new UserController(_configuration, context, _logger, DefaultTestUser);

                var ret = await controller.GetFilter("Id = 1 AND (DailyCalories gt 300)", null, 1, 5);
                var retType = Assert.IsType<OkObjectResult>(ret);
                var retValue = Assert.IsType<PagedList<User>>(retType.Value);

                Assert.Single(retValue);
                Assert.Equal(DefaultTestUser, retValue.First(u => u.Id == 1).Name);
            }
        }
    }
}
