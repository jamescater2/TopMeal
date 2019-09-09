using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TopMealApi.Model
{
    [Table("Role")]
    public class Role
    {
        public const string AdministratorDefault = "Administrator";
        public const string DataManagerDefault = "DataManager";
        public const string UserManagerDefault = "UserManager";
        public const string UserDefault = "User";

        public const string InvalidAuthorisationToken = "Cannot get user name from Authorization token";
        public const string AdministratorNotSetUp =  "<Administrator> role has not been setup - please contact your administrator";
        public const string UserRoleNotSetUp =  "<User> role has not been setup - please contact your administrator";
        public const string UserNotRegisteredMsg = "User not registered: POST api/auth/register to register";
        public const string InvalidRoleMsg = "Invalid Role - Please contact administrator";
        public const string NoExecForOtherUsersMsg = "No permission to execute this api for other users with Role ";
        public const string UserManagerMsg = "Only User Managers and Administrators can view/modify User tables";
        public const string UserPostUserIdMsg = "Cannot submit meals for other users, please use your own UserId";

        [Required]
        public int Id { get; set;}
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public Role AssignFrom(Role rhs)
        {
            Id = rhs.Id;
            Name = rhs.Name;
            return this;
        }
    }
}