using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using X.PagedList;
using TopMealApi.Context;
using TopMealApi.Model;
using TopMealApi.Utils;

namespace TopMealApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MealController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<MealController> _logger;
        private readonly string _testUserName;
        private readonly string _defaultRoleName;
        private readonly string _adminRoleName;
        private readonly int _defaultPageSize;
        private readonly string[] _authorizedRolesAdmins;
        private readonly string[] _authorizedRolesUsers;

        
        public MealController(IConfiguration configuration, AppDbContext context, ILogger<MealController> logger, string testUserName = null)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _testUserName = testUserName;
            
            _defaultRoleName = _configuration.GetValue<string>("AppRoles:DefaultRole", "User");
            _adminRoleName = _configuration.GetValue<string>("AppRoles:AdministratorRole", Role.AdministratorDefault);
            _defaultPageSize = _configuration.GetValue<int>("MealController:DefultPageSize", 10);
            _authorizedRolesAdmins = ContextUtils.RoleParameterToStringArray(_configuration.GetValue<string>("MealController:AuthorisedRolesAdmins"));
            _authorizedRolesUsers = ContextUtils.RoleParameterToStringArray(_configuration.GetValue<string>("MealController:AuthorisedRolesUsers"));
        }

        [AllowAnonymous]
        [HttpGet("help")]
        public ActionResult<string> GetHelp()
        {
            return ContextUtils.MealHelpStr + "\n\nMeal Columns: " + String.Join(", ", typeof(Meal).GetProperties().Select(p => p.Name));
        }

        [HttpGet]
        public async Task<IActionResult> Get(int? page = null, int? pageSize = null)
        {
            return await _GetByUserIdFilterPaged(null, null, page, pageSize);
        }

        [HttpGet("{filter}")]
        public async Task<IActionResult> GetFilter(string filter, int? page = null, int? pageSize = null)
        {
            Console.WriteLine($"# # # MealController.Get(filter={filter} page={page}, pageSize={pageSize})");
            return await _GetByUserIdFilterPaged(null, filter, page, pageSize);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(int userId, int? page = null, int? pageSize = null)
        {
            return await _GetByUserIdFilterPaged(userId, null, page, pageSize);
        }

        [HttpGet("user/{userId:int}/{filter}")]
        public async Task<IActionResult> GetByUserIdFilter(int userId, string filter = null, int? page = null, int? pageSize = null)
        {
            return await _GetByUserIdFilterPaged(userId, filter, page, pageSize);
        }

        [HttpGet("remaining")]
        public async Task<IActionResult> GetRemaining()
        {
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            var userId = authRet.ApiUser.Id;
            var duc = await _context.DailyUserCalories.FirstOrDefaultAsync(duc => duc.UserId == userId && duc.Date == DateTime.Now.Date);
            var ret = authRet.ApiUser.DailyCalories - (duc?.Calories ?? 0);

            return Ok(ret > 0 ? ret : 0);
        }

        [HttpGet("remaining/{userId}")]
        public async Task<IActionResult> GetRemaining(int userId)
        {
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            if (userId != authRet.ApiUser.Id && !_authorizedRolesAdmins.Contains(authRet.ApiUserRole.Name))
            {
                return BadRequest(Role.NoExecForOtherUsersMsg + "<" + authRet.ApiUserRole.Name + ">");
            }
            var duc = await _context.DailyUserCalories.FirstOrDefaultAsync(duc => duc.UserId == userId && duc.Date == DateTime.Now.Date);
            var ret = authRet.ApiUser.DailyCalories - (duc?.Calories ?? 0);

            return Ok(ret > 0 ? ret : 0);
        }
        
        private async Task<IActionResult> _GetByUserIdFilterPaged(int? userId = null, string filter = null, int? page = null, int? pageSize = null)
        {
            try
            {
                var ret = (IEnumerable<Meal>)null;
                var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

                if (authRet.ErrorMessage != null)
                {
                    return BadRequest(authRet.ErrorMessage);
                }
                var isAdmin = _authorizedRolesAdmins.Contains(authRet.ApiUserRole.Name);

                if (!isAdmin)
                {
                    if (userId.HasValue && userId != authRet.ApiUser.Id)
                    {
                        return BadRequest(Role.NoExecForOtherUsersMsg + "<" + authRet.ApiUserRole.Name + ">");
                    }
                    userId = authRet.ApiUser.Id; // Only retrieve Users own meals
                }
                filter = ContextUtils.ConvertFilterToLinq(filter);
                
                if (userId.HasValue)
                {
                    if (await _context.UserByIdAsync(userId.Value) == null)
                    {
                        return BadRequest($"Unknown UserId <userId>");
                    }
                    if (page.HasValue)
                    {
                        ret = string.IsNullOrEmpty(filter)
                            ? _context.Meals.Where(m => m.UserId == userId).ToPagedList(page.Value, pageSize ?? _defaultPageSize)
                            : _context.Meals.FromSqlRaw($"SELECT * FROM dbo.Meal WHERE UserId = {userId} AND ({filter})").ToPagedList(page.Value, pageSize ?? _defaultPageSize);
                    }
                    else
                    {
                        ret = string.IsNullOrEmpty(filter)
                            ? await _context.Meals.Where(m => m.UserId == userId).ToListAsync()
                            : await _context.Meals.FromSqlRaw($"SELECT * FROM dbo.Meal WHERE UserId = {userId} AND ({filter})").ToListAsync();
                    }
                }
                else
                {
                    if (page.HasValue)
                    {
                        ret = string.IsNullOrEmpty(filter)
                                ? _context.Meals.ToPagedList(page.Value, pageSize ?? _defaultPageSize)
                                : _context.Meals.FromSqlRaw($"SELECT * FROM dbo.Meal WHERE ({filter})").ToPagedList(page.Value, pageSize ?? _defaultPageSize);
                    }
                    else
                    {
                        ret = string.IsNullOrEmpty(filter)
                            ? await _context.Meals.ToListAsync()
                            : await _context.Meals.FromSqlRaw($"SELECT * FROM dbo.Meal WHERE ({filter})").ToListAsync();
                    }
                }
                return Ok(ret);
            }
            catch (SqlException e)
            {
                var columns = typeof(Meal).GetProperties().Select(p => p.Name);
                return BadRequest("\nGetByFilter(filter) - Sql Exception: " + e.Message + "\n\n" + "Columns: " + String.Join(", ", columns));
            }
        }

        [HttpPost("{description}")]
        public async Task<IActionResult> Post(string description)
        {
            Console.WriteLine($"# # MealController.Post(description={description})");
            return await _InsertMeal(new Meal { Description = description });
        }

        [HttpPost("{calories:int}")]
        public async Task<IActionResult> Post(int calories)
        {
            Console.WriteLine($"# # MealController.Post(calories={calories})");
            return await _InsertMeal(new Meal { Calories = calories });
        }

        [HttpPost("{calories:int}/{description}")]
        public async Task<IActionResult> Post(int calories, string description)
        {
            Console.WriteLine($"# # MealController.Post(calories={calories} description={description})");
            return await _InsertMeal(new Meal { Calories = calories, Description = description });
        }

        [HttpPost]
        public async Task<IActionResult> Post(Meal meal)
        {
            Console.WriteLine($"# # MealController.Post(meal object) meal.Time={meal.Time}");
            return await _InsertMeal(meal);
        }

        private async Task<IActionResult> _InsertMeal(Meal newMeal)
        {
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            if (newMeal.Calories < 0)
            {
                newMeal.Calories = 0;
            }
            var erroMessage = await ValidateMealAsync(authRet.ApiUser, newMeal);

            if (erroMessage != null)
            {
                return BadRequest(erroMessage);
            }
            var isAdmin = _authorizedRolesAdmins.Contains(authRet.ApiUserRole.Name);

            if (!isAdmin)
            {
                if (newMeal.UserId != authRet.ApiUser.Id)
                {
                    return BadRequest($"Only User Managers and Administrators can add meals for other users. Please use your own UserId <authRet.ApiUser.Id>");
                }
            }
            var newUser = newMeal.UserId == authRet.ApiUser.Id ? authRet.ApiUser : await _context.Users.FindAsync(newMeal.UserId);
            var newDailyUserCalories = await _context.DailyUserCalories.FirstOrDefaultAsync(duc => duc.UserId == newUser.Id && duc.Date.Date == newMeal.Date.Date);

            if (newDailyUserCalories == null)
            {
                newDailyUserCalories = new DailyUserCalories { UserId = newUser.Id, Date = newMeal.Date.Date, Calories = newMeal.Calories };
                _context.DailyUserCalories.Add(newDailyUserCalories);
            }
            else
            {
                var origDailyUserCalories = newDailyUserCalories.Calories;

                newDailyUserCalories.Calories += newMeal.Calories;
                _context.Entry(newDailyUserCalories).State = EntityState.Modified; // Update database

                if (origDailyUserCalories < newUser.DailyCalories && newDailyUserCalories.Calories >= newUser.DailyCalories)
                {
                    await UpdateSameDayMealsWithinLimit(newMeal, withinLimit: false);
                }
            }
            newMeal.Id = 0;
            newMeal.WithinLimit = newDailyUserCalories.Calories < newUser.DailyCalories;
            _context.Meals.Add(newMeal);
            await _context.SaveChangesAsync();

            return CreatedAtAction("Get", new Meal{ Id = newMeal.Id }, newMeal);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutIdMeal(int id, Meal meal)
        {
            Console.WriteLine($"# # # Put(id={id}, meal)");
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            return await _UpdateIdMeal(authRet, id, meal);
        }

        [HttpPut("{id:int}/{calories:int}")]
        public async Task<IActionResult> PutIdCalories(int id, int calories)
        {
            Console.WriteLine($"# # # Put(id={id}, calories={calories})");
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            var existingMeal = _context.Meals.Find(id);

            if (existingMeal == null)
            {
                return NotFound($"Id={id}");
            }
            var newMeal = new Meal().AssignFrom(existingMeal);
            newMeal.Calories = calories;
            return await _UpdateIdMeal(authRet, id, newMeal);
        }

        private async Task<IActionResult> _UpdateIdMeal(AuthResult authRet, int id, Meal newMeal)
        {
            if (id != newMeal.Id)
            {
                return BadRequest("Id not equal to meal.Id");
            }
            var oldMeal = _context.Meals.Find(id);
            
            if (oldMeal == null)
            {
                return NotFound($"Not found Id={id}");
            }
            if (newMeal.UserId < 1)
            {
                newMeal.UserId = oldMeal.UserId;
            }
            if (newMeal.Date.IsDefault() && newMeal.Time.IsDefault())
            {
                newMeal.Date = oldMeal.Date;
                newMeal.Time = oldMeal.Time;
            }
            else if (newMeal.Date.IsDefault())
            {
                newMeal.Date = newMeal.Time.Date;
            }
            else if (newMeal.Time.IsDefault())
            {
                newMeal.Time = newMeal.Date.Date + (oldMeal.Time.NotDefault() ? oldMeal.Time.TimeOfDay : default(TimeSpan));
            }
            if (newMeal.Description == null || newMeal.Description.Trim().Length < 1)
            {
                newMeal.Description = oldMeal.Description;
            }
            if (newMeal.Calories == 0) // If less than zero then resend to Nutritionix service when Validated
            {
                newMeal.Calories = oldMeal.Calories;    
            }
            var erroMessage = await ValidateMealAsync(authRet.ApiUser, newMeal);

            if (erroMessage != null)
            {
                return BadRequest(erroMessage);
            }
            var isAdmin = _authorizedRolesAdmins.Contains(authRet.ApiUserRole.Name);

            if (!isAdmin)
            {
                if (newMeal.UserId != authRet.ApiUser.Id)
                {
                    return BadRequest($"Only User Managers and Administrators can modify UserId. Please use your own UserId <{authRet.ApiUser.Id}>");
                }
            }
            // No DailyUserCalories information has changed
            if (newMeal.Calories == oldMeal.Calories && newMeal.UserId == oldMeal.UserId && newMeal.Date == oldMeal.Date)
            {
                newMeal.WithinLimit = oldMeal.WithinLimit;
            }
            // Calories changed but same User and Date
            else if (newMeal.UserId == oldMeal.UserId && newMeal.Date == oldMeal.Date) 
            {
                var dealUser = newMeal.UserId == authRet.ApiUser.Id ? authRet.ApiUser : _context.Users.Find(newMeal.UserId);
                var dailyUserCalories = await _context.DailyUserCalories.FirstAsync(duc => duc.UserId == dealUser.Id && duc.Date.Date == newMeal.Date.Date);
                var origDailyUserCalories = dailyUserCalories.Calories;

                dailyUserCalories.Calories += newMeal.Calories - oldMeal.Calories;
                _context.Entry(dailyUserCalories).State = EntityState.Modified; // Update database (locking DailyUserCalories table first)

                if (origDailyUserCalories < dealUser.DailyCalories && dailyUserCalories.Calories >= dealUser.DailyCalories)
                {
                    await UpdateSameDayMealsWithinLimit(newMeal, withinLimit: false); // Update other Deals on same day
                }
                newMeal.WithinLimit = dailyUserCalories.Calories < dealUser.DailyCalories;
            }
            // User or Date have changed
            else
            {
                var oldUser = oldMeal.UserId == authRet.ApiUser.Id ? authRet.ApiUser : _context.Users.Find(oldMeal.UserId);
                var newUser = newMeal.UserId == authRet.ApiUser.Id ? authRet.ApiUser : _context.Users.Find(newMeal.UserId);
                var oldDailyUserCalories = await _context.DailyUserCalories.FirstAsync(duc => duc.UserId == oldUser.Id && duc.Date.Date == oldMeal.Date.Date);
                var newDailyUserCalories = await _context.DailyUserCalories.FirstOrDefaultAsync(duc => duc.UserId == newUser.Id && duc.Date.Date == newMeal.Date.Date);
                var origOldDailyUserCalories = oldDailyUserCalories.Calories;
                
                oldDailyUserCalories.Calories -= oldMeal.Calories;
                _context.Entry(oldDailyUserCalories).State = EntityState.Modified; // Update database (locking DailyUserCalories table first)

                if (newDailyUserCalories == null)
                {
                    newMeal.WithinLimit = newMeal.Calories < newUser.DailyCalories;
                    newDailyUserCalories = new DailyUserCalories { UserId = newUser.Id, Date = newMeal.Date.Date, Calories = newMeal.Calories };
                    _context.DailyUserCalories.Add(newDailyUserCalories);
                }
                else
                {
                    var origNewDailyUserCalories = newDailyUserCalories.Calories;

                    newDailyUserCalories.Calories += newMeal.Calories;
                    _context.Entry(newDailyUserCalories).State = EntityState.Modified; // Update database (locking DailyUserCalories table first)

                    if (origNewDailyUserCalories < newUser.DailyCalories && newDailyUserCalories.Calories >= newUser.DailyCalories)
                    {
                        await UpdateSameDayMealsWithinLimit(newMeal, withinLimit: false); // Update other Deals on same day
                    }
                }
                if (origOldDailyUserCalories >= oldUser.DailyCalories && oldDailyUserCalories.Calories < oldUser.DailyCalories)
                {
                    await UpdateSameDayMealsWithinLimit(oldMeal, withinLimit: true); // Update other Deals on same day
                }
                newMeal.WithinLimit = newDailyUserCalories.Calories < newUser.DailyCalories;
            }
            oldMeal.AssignFrom(newMeal); // Copy attributes from meal to oldMeal so we can update the database
            _context.Entry(oldMeal).State = EntityState.Modified;
            await _context.SaveChangesAsync();
 
            return CreatedAtAction("Get", new Meal{ Id = newMeal.Id }, newMeal);
        }


        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Console.WriteLine($"# # MealController.Delete(id={id})");
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            var oldMeal = _context.Meals.Find(id);

            if (oldMeal == null)
            {
                return NotFound();
            }
            var isAdmin = _authorizedRolesAdmins.Contains(authRet.ApiUserRole.Name);

            if (!isAdmin)
            {
                if (oldMeal.UserId != authRet.ApiUser.Id)
                {
                    return BadRequest($"Only User Managers and Administrators can delete other users deals");
                }
            }
            var oldUser = oldMeal.UserId == authRet.ApiUser.Id ? authRet.ApiUser : await _context.UserByIdAsync(oldMeal.UserId);
            var oldDailyUserCalories = await _context.DailyUserCalories.FirstAsync(duc => duc.UserId == oldUser.Id && duc.Date.Date == oldMeal.Date.Date);
            var origOldDailyUserCalories = oldDailyUserCalories.Calories;

            oldDailyUserCalories.Calories -= oldMeal.Calories;
            _context.Entry(oldDailyUserCalories).State = EntityState.Modified; // Update database (locking DailyUserCalories table first)

            if (origOldDailyUserCalories >= oldUser.DailyCalories && oldDailyUserCalories.Calories < oldUser.DailyCalories)
            {
                await UpdateSameDayMealsWithinLimit(oldMeal, withinLimit:true); // Update other Deals on same day
            }
            oldMeal.WithinLimit = oldDailyUserCalories.Calories < oldUser.DailyCalories;
            _context.Meals.Remove(oldMeal);
            await _context.SaveChangesAsync();

            return Ok(oldMeal);
        }

        [HttpDelete("alluser/{userId:int}")]
        public async Task<IActionResult> DeleteAllByUserId(int userId)
        {
            Console.WriteLine($"# # MealController.DeleteAllByUserId(userId={userId})");
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesUsers);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            var user = await _context.UserByIdAsync(userId);

            if (user == null)
            {
                return BadRequest($"Unknown UserId <{userId}>");
            }
            var isAdmin = _authorizedRolesAdmins.Contains(authRet.ApiUserRole.Name);

            if (!isAdmin)
            {
                if (userId != authRet.ApiUser.Id)
                {
                    return BadRequest($"Only User Managers and Administrators can delete other users deals");
                }
            }
            var userDucs = await _context.DailyUserCalories.Where(duc => duc.UserId == userId).ToListAsync();
            var userMeals = await _context.Meals.Where(m => m.UserId == userId).ToListAsync();
            var ret = userMeals.Count;

            _context.DailyUserCalories.RemoveRange(userDucs);
            _context.Meals.RemoveRange(userMeals);
            await _context.SaveChangesAsync();

            return Ok($"Deleted {ret} meals for UserId {userId}");
        }

        private async Task UpdateSameDayMealsWithinLimit(Meal meal, bool withinLimit)
        {
            var sameDayMeals = await _context.Meals.Where(m => m.UserId == meal.UserId && m.Date.Date == meal.Date.Date && m.Id != meal.Id).ToListAsync();

            if (sameDayMeals.Any())
            {
                sameDayMeals.ForEach(m => m.WithinLimit = withinLimit); // Update meals for given day with broken limit
                _context.Meals.UpdateRange(sameDayMeals);
            }
        }

        private async Task<string> ValidateMealAsync(User apiUser, Meal meal)
        {
            //Console.WriteLine($"# # # ValidateMeal: meal.UserId={meal.UserId} apiUser.Id={apiUser.Id}");
            if (meal.UserId < 1)
            {
                meal.UserId = apiUser.Id;
            }
            else if (_context.Users.Find(meal.UserId) == null)
            {
                return $"Invalid UserId={meal.UserId}";
            }
            if (meal.Date.IsDefault() && meal.Time.IsDefault())
            {
                meal.Date = meal.Time = DateTime.Now;
            }
            else if (meal.Time.IsDefault())
            {
                meal.Time = meal.Date;
            }
            else if (meal.Date.IsDefault())
            {
                meal.Date = meal.Time;
            }
            else if (meal.Time.Date != meal.Date.Date)
            {
                meal.Time = meal.Date.Date + meal.Time.TimeOfDay;
            }
            meal.Date = meal.Date.Date;
            meal.Time = meal.Time.TruncToSecond();

            if (meal.Description == null || meal.Description.Trim().Length < 1)
            {
                meal.Description = null;
            }
            if (meal.Calories < 1)
            {
                if (string.IsNullOrEmpty(meal.Description))
                {
                    return "Description must be set if calories are unset";
                }
                var xAppId = _configuration.GetValue<string>("Nutritionix:x-app-id");
                var xAppKey = _configuration.GetValue<string>("Nutritionix:x-app-key");

                meal.Calories = await NutritionixUtils.GetCaloriesIntAsync(xAppId, xAppKey, meal.Description);

                if (meal.Calories < 1)
                {
                    return "Calorie Service request failed, please enter calories or simplify description";
                }
            }
            return null;
        }
    }
}
