using System.ComponentModel.DataAnnotations;

namespace TopMealApi.Dtos
{
    public class UserForRegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters")]
        public string Username { get; set; }
        
        [Required]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "You must specify a password between 4 and 20 characters")]
        public string Password { get; set; }
    }
}