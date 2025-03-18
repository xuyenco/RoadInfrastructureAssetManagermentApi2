using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Jwt;
using Road_Infrastructure_Asset_Management.Model.Request;

namespace Road_Infrastructure_Asset_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _Service;
        private readonly IConfiguration _Configuration;
        public UsersController(IUsersService Service, IConfiguration configuration)
        {
            _Service = Service;
            _Configuration = configuration;
        }
        [HttpGet]
        public async Task<ActionResult> GetAllUsers()
        {
            return Ok(await _Service.GetAllUsers());
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetUsersById(int id)
        {
            var budgets = await _Service.GetUserById(id);
            if (budgets == null)
            {
                return NotFound("Users does't exist");
            }
            return Ok(budgets);
        }

        [HttpPost]
        public async Task<ActionResult> CreateUsers(UsersRequest request)
        {
            var budget = await _Service.CreateUser(request);
            if (budget == null)
            {
                return BadRequest();
            }
            return Ok(budget);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateUsers(UsersRequest request, int id)
        {
            var budget = await _Service.GetUserById(id);
            if (budget == null)
            {
                return NotFound();
            }
            var newbudget = await _Service.UpdateUser(id, request);
            if (newbudget == null)
            {
                return BadRequest();
            }
            return Ok(newbudget);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUsers(int id)
        {
            var budget = await _Service.GetUserById(id);
            if (budget == null)
            {
                return NotFound();
            }
            var result = await _Service.DeleteUser(id);
            if (result != true)
            {
                return BadRequest();
            }
            return NoContent();
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginRequest userinput)
        {
            var user = await _Service.Login(userinput);
            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }
            var token = JwtTokenHelper.GenerateJwtToken(
                user.username,
                user.role,
                _Configuration["Jwt:Key"],
                _Configuration["Jwt:Issuer"],
                _Configuration["Jwt:Audience"]
            );
            return Ok(token);
        }

    }
}
