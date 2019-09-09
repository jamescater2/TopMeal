using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using X.PagedList;
using TopMealApi.Context;
using TopMealApi.Model;
using Microsoft.Extensions.Configuration;

namespace TopMealApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly string _testUserName;
        private readonly string _defaultRoleName;
        private readonly string _adminRoleName;
        private readonly int _defaultPageSize;
        private readonly string[] _authorizedRolesAdmins;
        private readonly string[] _authorizedRolesUsers;
        
        public UserController(IConfiguration configuration, AppDbContext context, ILogger<UserController> logger, string testUserName = null)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _testUserName = testUserName;

            _defaultRoleName = _configuration.GetValue<string>("AppRoles:DefaultRole", Role.UserDefault);
            _adminRoleName = _configuration.GetValue<string>("AppRoles:AdministratorRole", Role.AdministratorDefault);
            _defaultPageSize = _configuration.GetValue<int>("UserController:DefultPageSize", 10);
            _authorizedRolesAdmins = ContextUtils.RoleParameterToStringArray(_configuration.GetValue<string>("UserController:AuthorisedRolesAdmins"));
            _authorizedRolesUsers = ContextUtils.RoleParameterToStringArray(_configuration.GetValue<string>("UserController:AuthorisedRolesUsers"));
        }

        [AllowAnonymous]
        [HttpGet("help")]
        public ActionResult<string> GetHelp()
        {
            return ContextUtils.UserHelpStr + "\n\nUser Columns: " + String.Join(", ", typeof(User).GetProperties().Select(p => p.Name));
        }

        [HttpGet]
        public async Task<IActionResult> Get(int? userId = null, int? page = null, int? pageSize = null)
        {
            Console.WriteLine($"# # # UserController.Get(userId={userId} page={page}, pageSize={pageSize})");
            return await _GetByUserIdFilter(userId, null, page, pageSize);
        }
        
        [HttpGet("{filter}")]
        public async Task<IActionResult> GetFilter(string filter, int? userId = null, int? page = null, int? pageSize = null)
        {
            Console.WriteLine($"# # # UserController.Get(filter={filter} page={page}, pageSize={pageSize})");
            return await _GetByUserIdFilter(userId, filter, page, pageSize);
        }

        private async Task<IActionResult> _GetByUserIdFilter(int? userId = null, string filter = null, int? page = null, int? pageSize = null)
        {
            try
            {
                var ret = (IEnumerable<User>)null;
                var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

                if (authRet.ErrorMessage != null)
                {
                    return BadRequest(authRet.ErrorMessage);
                }
                var isAdmin = _authorizedRolesAdmins.Contains(authRet.ApiUserRole.Name);
 
                if (!isAdmin)
                {
                    if (userId.HasValue && userId.Value != authRet.ApiUser.Id)
                    {
                        return BadRequest("You are not authorized to view other users details");
                    }
                    userId = authRet.ApiUser.Id; // Only retrieve Users own meals
                }
                filter = ContextUtils.ConvertFilterToLinq(filter);

                if (userId.HasValue)
                {
                    if (page.HasValue)
                    {
                        ret = string.IsNullOrEmpty(filter)
                            ? _context.Users.Where(u => u.Id == userId).ToPagedList(page.Value, pageSize ?? _defaultPageSize)
                            : _context.Users.FromSqlRaw($"SELECT * FROM dbo.[User] WHERE Id = {userId} AND ({filter})").ToPagedList(page.Value, pageSize ?? _defaultPageSize);
                    }
                    else
                    {
                        ret = string.IsNullOrEmpty(filter)
                            ? await _context.Users.Where(u => u.Id == userId).ToListAsync()
                            : await _context.Users.FromSqlRaw($"SELECT * FROM dbo.[User] WHERE Id = {userId} AND ({filter})").ToListAsync();
                    }
                }
                else
                {
                    if (page.HasValue)
                    {
                        ret = string.IsNullOrEmpty(filter)
                            ? _context.Users.ToPagedList(page.Value, pageSize ?? _defaultPageSize)
                            : _context.Users.FromSqlRaw($"SELECT * FROM dbo.[User] WHERE ({filter})").ToPagedList(page.Value, pageSize ?? _defaultPageSize);
                    }
                    else
                    {
                        ret = string.IsNullOrEmpty(filter)
                            ? await _context.Users.ToListAsync()
                            : await _context.Users.FromSqlRaw($"SELECT * FROM dbo.[User] WHERE ({filter})").ToListAsync();
                    }
                }
                return Ok(ret);
            }
            catch (SqlException e)
            {
                var columns = typeof(User).GetProperties().Select(p => p.Name);
                return BadRequest("\nGetByFilter(filter) Sql Exception: " + e.Message + "\n\n" + "Columns: " + String.Join(", ", columns));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

                if (authRet.ErrorMessage != null)
                {
                    return BadRequest(authRet.ErrorMessage);
                }
                var isAdmin = _authorizedRolesAdmins.Contains(authRet.ApiUserRole.Name);

                if (!isAdmin && id != authRet.ApiUser.Id)
                {
                    return BadRequest("You are not authorized to view other users details");
                }
                var ret = await _context.UserByIdAsync(id);

                if (ret == null)
                {
                    return BadRequest($"Invalid User Identifier Id={id}");
                }
                return Ok(ret);
            }
            catch (SqlException e)
            {
                var columns = typeof(User).GetProperties().Select(p => p.Name);
                return BadRequest("\nGet(id) - Sql Exception: " + e.Message + "\n\n" + "Columns: " + String.Join(", ", columns));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(User newUser)
        {
            Console.WriteLine($"# # # UserController.Post newUser.Id={newUser.Id}");
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesAdmins);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            if (newUser.RoleId < 1)
            {
                newUser.RoleId = authRet.UserRole.Id;
            }
            if (newUser.RoleId != authRet.UserRole.Id && authRet.ApiUser.RoleId != authRet.AdminRole.Id)
            {
                return BadRequest("Only Administrators can add users with Roles other than <User>");
            }
            if (string.IsNullOrEmpty(newUser.Name))
            {
                return BadRequest("User Name must be supplied");
            }
            newUser.Id = 0;
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction("Get", new User{ Id = newUser.Id }, newUser);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, User newUser)
        {
            Console.WriteLine($"# # # UserController.Put id={id} newUser.Id={newUser.Id}");
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);
            var allowModifyUserName = _configuration.GetValue<string>("UserController:AllowModifyUserName") == "true";

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            var isAdmin = _authorizedRolesAdmins.Contains(authRet.ApiUserRole.Name);

            if (id != newUser.Id)
            {
                return BadRequest($"Id={id} not equal to role.Id={newUser.Id}");
            }
            var oldUser = await _context.UserByIdAsync(id);

            if (oldUser == null)
            {
                return NotFound();
            }
            if (!isAdmin && id != authRet.ApiUser.Id)
            {
                return BadRequest("You are not authorized to modify other users details");
            }
            if (newUser.RoleId < 1)
            {
                newUser.RoleId = oldUser.RoleId;
            }
            if (newUser.RoleId != oldUser.RoleId && authRet.ApiUser.RoleId != authRet.AdminRole.Id)
            {
                return BadRequest("Only Administrators can modify RoleId");
            }
            if (newUser.Name == null || newUser.Name.Trim().Length < 1)
            {
                newUser.Name = oldUser.Name;
            }
            if (newUser.Name != oldUser.Name)
            {
                if (allowModifyUserName)
                {
                    if (_context.Users.FirstOrDefault(u => u.Name == newUser.Name) != null)
                    {
                        BadRequest($"UserName <{newUser.Name} Already taken, please choose another name");
                    }
                }
                else
                {
                    return BadRequest("You cannot modify User Names");
                }
            }
            if (newUser.DailyCalories > oldUser.DailyCalories)
            {
                var dailyUserCaloriesToUpdate = await _context.DailyUserCalories.Where(duc => duc.Calories >= oldUser.DailyCalories && duc.Calories < newUser.DailyCalories).ToListAsync();

                foreach (var duc in dailyUserCaloriesToUpdate)
                {
                    var sameDayMeals = await _context.Meals.Where(m => m.UserId == duc.UserId && m.Date.Date == duc.Date.Date).ToListAsync();

                    sameDayMeals.ForEach(m => m.WithinLimit = true); // Update meals for given day with broken limit
                    _context.Meals.UpdateRange(sameDayMeals);
                }
            }
            else if (newUser.DailyCalories < oldUser.DailyCalories)
            {
                var dailyUserCaloriesToUpdate = await _context.DailyUserCalories.Where(duc => duc.Calories >= newUser.DailyCalories && duc.Calories < oldUser.DailyCalories).ToListAsync();

                foreach (var duc in dailyUserCaloriesToUpdate)
                {
                    var sameDayMeals = await _context.Meals.Where(m => m.UserId == duc.UserId && m.Date.Date == duc.Date.Date).ToListAsync();

                    sameDayMeals.ForEach(m => m.WithinLimit = false); // Update meals for given day with broken limit
                    _context.Meals.UpdateRange(sameDayMeals);
                }
            }
            newUser.PasswordHash = oldUser.PasswordHash;
            newUser.PasswordSalt = oldUser.PasswordSalt;
            oldUser.AssignFrom(newUser);
            _context.Entry(oldUser).State = EntityState.Modified;
            await _context.SaveChangesAsync();
 
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesAdmins);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            var oldUser = await _context.UserByIdAsync(id);

            if (oldUser == null)
            {
                return NotFound();
            }
            if (_context.Meals.Any(u => u.UserId == id))
            {
                return BadRequest($"Cannot delete User <{oldUser.Name}> with Id <{id}> until all their meals have been deleted");
            }
            _context.Users.Remove(oldUser);
            await _context.SaveChangesAsync();

            return Ok(oldUser);
        }
    }
}
