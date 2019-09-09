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
    public class IntTest_RoleController : UnitTest_ControllerBase
    {
        private readonly IConfiguration _configuration; 
        private readonly ILogger<RoleController> _logger;
        private readonly DbContextOptions<AppDbContext> _dbOptions;

        public IntTest_RoleController()
        {
            var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
            var factory = serviceProvider.GetService<ILoggerFactory>();

            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            _logger = factory.CreateLogger<RoleController>();
            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(_configuration.GetValue<string>("Data:CommandAPIConnection:ConnectionString"))
                    .Options;
        }

        [Fact]
        public async void Test_GetFilter()
        {
            using (var context = new AppDbContext(_dbOptions))
            {
                var controller = new RoleController(_configuration, context, _logger, DefaultTestUser);

                {
                    var ret = await controller.GetFilter("(Id > 0 OR Id < 10) AND (Name eq 'User' OR Name eq 'UserManager')");
                    var retType = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<List<Role>>(retType.Value);

                    Assert.Equal(2, retValue.Count());
                    Assert.True(retValue.All(r => (r.Id > 0 || r.Id < 10) && (r.Name == "User" || r.Name == "UserManager")));
                }

                // Paged
                {
                    var ret = await controller.GetFilter("(Id > 0 OR Id < 10) AND (Name eq 'User' OR Name eq 'UserManager')", 1, 1);
                    var retType = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<PagedList<Role>>(retType.Value);

                    Assert.Single(retValue);
                    Assert.True(retValue.All(r => (r.Id > 0 || r.Id < 10) && (r.Name == "User" || r.Name == "UserManager")));
                }
                {
                    var ret = await controller.GetFilter("(Id > 0 OR Id < 10) AND (Name eq 'User' OR Name eq 'UserManager')", 2, 1);
                    var retType = Assert.IsType<OkObjectResult>(ret);
                    var retValue = Assert.IsType<PagedList<Role>>(retType.Value);

                    Assert.Single(retValue);
                    Assert.True(retValue.All(r => (r.Id > 0 || r.Id < 10) && (r.Name == "User" || r.Name == "UserManager")));
                }
            }
        }
    }
}
