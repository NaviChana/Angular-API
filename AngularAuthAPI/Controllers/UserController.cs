using AngularAuthAPI.Context;
using AngularAuthAPI.Helpers;
using AngularAuthAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Microsoft.Data.SqlClient;

namespace AngularAuthAPI.Controllers
{
    [Route("api/controller")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly AppDbContext _appDbContext;

        public UserController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObject)
        {
            if(userObject == null)
                return BadRequest();

            var user = await _appDbContext.Users.FirstOrDefaultAsync(x => x.Username == userObject.Username && x.Password == userObject.Password);

            return Ok(new { Message = "Login was successful!" });
            
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser( User userObject)
        {
            if (userObject == null)
                return BadRequest();

            // check uname
            if (await CheckUsernameExists(userObject.Username))
                return BadRequest(new { Message = "That username already exists!" });

            // check email
            if (await CheckEmailExists(userObject.Email))
                return BadRequest(new { Message = "That email is already in use!" });

            // check password strength
            var pass = CheckPasswordStrength(userObject.Password);

            if (!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass.ToString() });

            userObject.Password = PasswordHash.HashPassword(userObject.Password);
            await _appDbContext.Users.AddAsync(userObject);
            await _appDbContext.SaveChangesAsync();

            return Ok(new { Message = "Registration was a success!" });
        }

        private Task<bool> CheckUsernameExists(string username)
            =>  _appDbContext.Users.AnyAsync(x => x.Username == username);

        private Task<bool> CheckEmailExists(string email)
            => _appDbContext.Users.AnyAsync(x => x.Email == email);

        private string CheckPasswordStrength(string password)
        {
            StringBuilder sb = new StringBuilder();

            if (password.Length < 8)
                sb.Append("Password must be at least 8 characters." + Environment.NewLine);

            if (!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]") && Regex.IsMatch(password, "[0-9]")))
                sb.Append("Password must contain upper/lowercase letters and numbers!" + Environment.NewLine);

            if ((!Regex.IsMatch(password, "[<,>,!,@,#,$,%,^,&,*,(,),_,+,{,},?,:,;,|,\\,.,/,~,`,-,=]")))
                sb.Append("Password must contain one or more special characters!" + Environment.NewLine);

            return sb.ToString();
        }
    }
}
