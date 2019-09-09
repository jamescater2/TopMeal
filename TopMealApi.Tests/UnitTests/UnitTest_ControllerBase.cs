using Microsoft.EntityFrameworkCore;
using TopMealApi.Context;

namespace TopMealApi.Tests
{
    public enum TestRoleEnum
    {
        Administrator = 1,
        DataManager = 2,
        UserManager = 3,
        User = 4,
    }
    
    public class UnitTest_ControllerBase
    {
        public static string DefaultTestUser { get => "jamescater"; }

        public UnitTest_ControllerBase()
        {
        }

        protected DbContextOptions<AppDbContext> NewInMemoryDb(string name)
            => new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: name).Options;
    }
}
