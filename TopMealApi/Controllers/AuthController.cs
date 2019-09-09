using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TopMealApi.Context;
using TopMealApi.Model;
using TopMealApi.Dtos;

namespace TopMealApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthRepository authRepository, ILogger<AuthController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _authRepository = authRepository;
            _configuration = configuration;
        }

        // api/auth/register
        [HttpPost("register")] 
        public async Task<IActionResult> Register([FromBody] UserForRegisterDto userForReigister) // Data Transfer Object containing username and password.
        {
            // validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (userForReigister == null)
            {
                return BadRequest("Missing argument - Need to supply a UserForLoginDto with Username and Password");
            }
            if (userForReigister.Username == null || userForReigister.Username.Length < 3 || userForReigister.Username.Length > 50)
            {
                return BadRequest("Username must be between 2 and 50 characters");
            }
            if (userForReigister.Password == null || userForReigister.Password.Length < 4 || userForReigister.Password.Length > 50)
            {
                return BadRequest("Password must be between 4 and 50 characters");
            }
            userForReigister.Username = userForReigister.Username.ToLower(); // Convert username to lower case before storing in database.

            if (await _authRepository.UserExists(userForReigister.Username))
            {
                return BadRequest("Username is already taken");
            }
            var userToCreate = new User
            {
                Name = userForReigister.Username,
                RoleId = _configuration.GetValue<int>("AppRoles:DefaultRoleId", 4),
                DailyCalories = _configuration.GetValue<int>("AppRoles:DefaultDailyCalories", 2000),
            };
            var createdUser = await _authRepository.Register(userToCreate, userForReigister.Password);

            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserForLoginDto userForLogin)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (userForLogin == null)
            {
                return BadRequest("Missing argument - Need to supply a UserForLoginDto with Username and Password");
            }
            if (userForLogin.Username == null || userForLogin.Username.Length < 3 || userForLogin.Username.Length > 50)
            {
                return BadRequest("Username must be between 2 and 50 characters");
            }
            if (userForLogin.Password == null || userForLogin.Password.Length < 4 || userForLogin.Password.Length > 50)
            {
                return BadRequest("Password must be between 4 and 50 characters");
            }
            var userFromRepo = await _authRepository.Login(userForLogin.Username.ToLower(), userForLogin.Password);

            if (userFromRepo == null)
            {
                return Unauthorized("Unauthorized - Invalid Username or password");
            }
            // generate token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("AppSettings:Token").Value);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier,userFromRepo.Id.ToString()),
                    new Claim(ClaimTypes.Name, userFromRepo.Name)
                }),
                Expires = DateTime.Now.AddDays(_configuration.GetValue<int>("AppSettings:LoginExpiryDays", 2)),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { tokenString });
        }
    }
}
