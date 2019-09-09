using System.Threading.Tasks;
using TopMealApi.Model;

namespace TopMealApi.Context
{
    public interface IAuthRepository
    {
         Task<User> Register(User user, string password); 
         Task<User> Login(string username, string pasword);
         Task<bool> UserExists(string username);
    }
}