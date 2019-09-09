using Microsoft.EntityFrameworkCore;
using TopMealApi.Context;

namespace TopMealApi.Tests
{
    public class IntTest_ControllerBase
    {
        public IntTest_ControllerBase()
        {
        }

        public void DeleteAllData(DbContextOptions<AppDbContext> options)
        {
            using (var context = new AppDbContext(options))
            {
                context.Meals.RemoveRange(context.Meals);
                context.Users.RemoveRange(context.Users);
                context.Roles.RemoveRange(context.Roles);
                context.SaveChanges();
            }
        }

        public void DeleteAllMealsAndUsers(DbContextOptions<AppDbContext> options)
        {
            using (var context = new AppDbContext(options))
            {
                context.Meals.RemoveRange(context.Meals);
                context.Users.RemoveRange(context.Users);
                context.SaveChanges();
            }
        }

        public void DeleteAllMeals(DbContextOptions<AppDbContext> options)
        {
            using (var context = new AppDbContext(options))
            {
                context.Meals.RemoveRange(context.Meals);
                context.SaveChanges();
            }
        }
    }
}
