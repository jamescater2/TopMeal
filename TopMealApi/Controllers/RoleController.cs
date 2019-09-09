using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using X.PagedList;
using TopMealApi.Context;
using TopMealApi.Model;

namespace TopMealApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<RoleController> _logger;
        private readonly string _testUserName;
        private readonly string _defaultRoleName;
        private readonly string _adminRoleName;
        private readonly int _defaultPageSize;
        private readonly string[] _authorizedRolesAdmins;
        
        public RoleController(IConfiguration configuration, AppDbContext context, ILogger<RoleController> logger, string testUserName = null)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _testUserName = testUserName;

            _defaultRoleName = _configuration.GetValue<string>("AppRoles:DefaultRole", Role.UserDefault);
            _adminRoleName = _configuration.GetValue<string>("AppRoles:AdministratorRole", Role.AdministratorDefault);
            _defaultPageSize = _configuration.GetValue<int>("RoleController:DefultPageSize", 5);
            _authorizedRolesAdmins = ContextUtils.RoleParameterToStringArray(_configuration.GetValue<string>("RoleController:AuthorisedRolesAdmins"));
        }

        [AllowAnonymous]
        [HttpGet("help")]
        public ActionResult<string> GetHelp()
        {
            return ContextUtils.RoleHelpStr + "\n\nRole Columns: " + String.Join(", ", typeof(Role).GetProperties().Select(p => p.Name));
        }

        [HttpGet]
        public async Task<IActionResult> Get(int? page = null, int? pageSize = null)
        {
            return await _GetByFilterPaged(null, page, pageSize);
        }

        [HttpGet("{filter}")]
        public async Task<IActionResult> GetFilter(string filter = null, int? page = null, int? pageSize = null)
        {
            return await _GetByFilterPaged(filter, page, pageSize);
        }

        [HttpGet("{filter}")]
        private async Task<IActionResult> _GetByFilterPaged(string filter = null, int? page = null, int? pageSize = null)
        {
            try
            {
                var ret = (IEnumerable<Role>)null;
                var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesAdmins);

                if (authRet.ErrorMessage != null)
                {
                    return BadRequest(authRet.ErrorMessage);
                }
                filter = ContextUtils.ConvertFilterToLinq(filter);

                if (page.HasValue)
                {
                    ret = string.IsNullOrEmpty(filter)
                        ? _context.Roles.ToPagedList(page.Value, pageSize ?? _defaultPageSize)
                        : _context.Roles.FromSqlRaw($"SELECT * FROM dbo.Role WHERE ({filter})").ToPagedList(page.Value, pageSize ?? _defaultPageSize);
                }
                else
                {
                    ret = string.IsNullOrEmpty(filter)
                        ? await _context.Roles.ToListAsync()
                        : await _context.Roles.FromSqlRaw($"SELECT * FROM dbo.Role WHERE ({filter})").ToListAsync();
                }

                return Ok(ret);
            }
            catch (SqlException e)
            {
                var columns = typeof(Role).GetProperties().Select(p => p.Name);
                return BadRequest("\nGetByFilter(filter) - Sql Exception: " + e.Message + "\n\n" + "Columns: " + String.Join(", ", columns));
            }
        }

        [HttpPost("{name}")]
        public async Task<IActionResult> Post(string name)
        {
            return await Post(new Role { Name = name });
        }

        [HttpPost]
        public async Task<IActionResult> Post(Role newRole)
        {
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesAdmins);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            if (string.IsNullOrEmpty(newRole.Name))
            {
                return BadRequest("Role Name must be supplied");
            }
            newRole.Id = 0;
            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();

            return CreatedAtAction("Post", new Role{ Id = newRole.Id }, newRole);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Role newRole)
        {
            var authRet = await ControllerUtils.AuthorizeUserAsync(HttpContext, _context, _testUserName, _adminRoleName, _defaultRoleName, _authorizedRolesAdmins);

            if (authRet.ErrorMessage != null)
            {
                return BadRequest(authRet.ErrorMessage);
            }
            if (id != newRole.Id)
            {
                return BadRequest($"Id={id} not equal to role.Id={newRole.Id}");
            }
            var oldRole = await _context.RoleByIdAsync(id);

            if (oldRole == null)
            {
                return NotFound();
            }
            oldRole.AssignFrom(newRole);
            _context.Entry(oldRole).State = EntityState.Modified;
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
            var oldRole = await _context.RoleByIdAsync(id);

            if (oldRole == null)
            {
                return NotFound();
            }
            if (_context.Users.Any(u => u.RoleId == id))
            {
                return BadRequest($"Cannot delete Role with Id {id} - Some Users are still assigned this RoleId");
            }
            _context.Roles.Remove(oldRole);
            await _context.SaveChangesAsync();

            return Ok(oldRole);
        }
    }
}
