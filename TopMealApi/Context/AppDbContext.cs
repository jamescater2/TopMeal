using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TopMealApi.Model;

namespace TopMealApi.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) :
            base(options)
        {
        }

        public DbSet<Meal> Meals { get; set; }

        public DbSet<User> Users{ get; set; }

        public DbSet<Role> Roles{ get; set; }

        public DbSet<DailyUserCalories> DailyUserCalories { get; set; }


        public Role RoleById(int id) => Roles.Find(id);
        public async Task<Role> RoleByIdAsync(int id) => await Roles.FindAsync(id);

        public Role RoleByName(string name) => Roles.FirstOrDefault(r => r.Name == name);
        public async Task<Role> RoleByNameAsync(string name) => await Roles.FirstOrDefaultAsync(r => r.Name == name);

        public User UserById(int id) => Users.Find(id);
        public async Task<User> UserByIdAsync(int id) => await Users.FindAsync(id);

        public User UserByName(string name) => Users.FirstOrDefault(u => u.Name == name);
        public async Task<User> UserByNameAsync(string name) => await Users.FirstOrDefaultAsync(u => u.Name == name);

        public async Task<User> GetOrCreateTestUserAsync(string testUserName, string roleName = "User")
        {
            var ret = await Users.FirstOrDefaultAsync(u => u.Name == testUserName);;

            if (ret == null)
            {
                var role = await Roles.FirstOrDefaultAsync(r => r.Name == roleName);

                if (role != null)
                {
                    Users.Add(new User { Name = testUserName, RoleId = role.Id, PasswordHash = Encoding.ASCII.GetBytes("x"), PasswordSalt = Encoding.ASCII.GetBytes("x")} );
                    await SaveChangesAsync();
                    ret = await Users.FirstOrDefaultAsync(u => u.Name == testUserName);
                }
            }
            return ret;
        }

        public async Task<bool> DeleteAllDataForTestUserAsync(string testUserName)
        {
            var ret = false;
            var testUser = await Users.FirstOrDefaultAsync(u => u.Name == testUserName);

            if (testUser != null)
            {
                DailyUserCalories.RemoveRange(await DailyUserCalories.Where(duc => duc.UserId == testUser.Id).ToListAsync());
                Meals.RemoveRange(await Meals.Where(m => m.UserId == testUser.Id).ToListAsync());
                Users.Remove(testUser);
                await SaveChangesAsync();
                ret = true;
            }
            return ret;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Meal>().HasIndex(p => new { p.UserId, p.Date }); // create compound index on UserId / Date

            modelBuilder.Entity<DailyUserCalories>().HasKey(c => new { c.UserId, c.Date }); // Create compound primary key on UserId / Date

            modelBuilder.Entity<Role>().HasData(new Role { Id = 1, Name = Role.AdministratorDefault });
            modelBuilder.Entity<Role>().HasData(new Role { Id = 2, Name = Role.DataManagerDefault });
            modelBuilder.Entity<Role>().HasData(new Role { Id = 3, Name = Role.UserManagerDefault });
            modelBuilder.Entity<Role>().HasData(new Role { Id = 4, Name = Role.UserDefault });
        }
    }
}