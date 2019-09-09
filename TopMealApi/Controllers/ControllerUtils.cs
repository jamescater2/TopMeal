using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TopMealApi.Context;
using TopMealApi.Model;

namespace TopMealApi.Controllers
{
    public class ControllerUtils
    {
        public static async Task<AuthResult> AuthorizeUserAsync(
            HttpContext httpContext, AppDbContext context, string testUserName, string adminRoleName, string userRoleName, IEnumerable<string> roleNames
            )
        {
            var ret = new AuthResult();
            var userName = httpContext.BearerUserName(testUserName);

            if (string.IsNullOrEmpty(userName))
            {
                return ret.SetErrMsg(Role.InvalidAuthorisationToken);
            }
            ret.AdminRole = await context.RoleByNameAsync(adminRoleName);

            if (ret.AdminRole == null)
            {
                return ret.SetErrMsg(Role.AdministratorNotSetUp);
            }
            ret.UserRole = await context.RoleByNameAsync(userRoleName);

            if (ret.UserRole == null)
            {
                return ret.SetErrMsg(Role.UserRoleNotSetUp);
            }
            ret.ApiUser = await context.UserByNameAsync(userName);

            if (ret.ApiUser == null)
            {
                return ret.SetErrMsg( $"Invalid User <{userName}> - please contact your administrator");
            }
            ret.ApiUserRole = await context.RoleByIdAsync(ret.ApiUser.RoleId);

            if (ret.ApiUserRole == null)
            {
                return ret.SetErrMsg($"Invalid RoleId <{ret.ApiUser.RoleId}> - please contact your administrator");
            }
            if (!roleNames.Contains(ret.ApiUserRole.Name))
            {
                return ret.SetErrMsg($"Role <{ret.ApiUserRole.Name}> is not authorized to run this API. Must be one of the following roles : " + string.Join(", ", roleNames));
            }
            return ret;
        }
    }
}
