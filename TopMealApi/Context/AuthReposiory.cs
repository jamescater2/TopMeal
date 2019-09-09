using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TopMealApi.Model;

namespace TopMealApi.Context
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> Login(string username, string password)
        {
            var user = await _context.UserByNameAsync(username);
            return user != null && VerifyPassword(password, user.PasswordHash, user.PasswordSalt) ? user : null;
        }

        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;

            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> UserExists(string username) => await _context.Users.AnyAsync(u => u.Name == username);

        private bool VerifyPassword(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            var ret = true;
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            { 
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)); // Create hash using password and salt

                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i])
                    {
                        ret = false;
                        break;
                    }
                }    
            }
            return ret;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
    }
}