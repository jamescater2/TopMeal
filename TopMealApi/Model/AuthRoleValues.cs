
namespace TopMealApi.Model
{
    public class AuthResult
    {
        public string ErrorMessage { get; set; }
        public User ApiUser { get; set; }
        public Role ApiUserRole { get; set; }
        public Role AdminRole { get; set; }
        public Role UserRole { get; set; }
        
        public AuthResult SetErrMsg(string errorMessage)
        {
            ErrorMessage = errorMessage;
            return this;
        }
    }
}