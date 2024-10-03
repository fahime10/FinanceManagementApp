using Microsoft.AspNetCore.Mvc;
using FinanceManagementApp.Models;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;

namespace FinanceManagementApp.Controllers
{
    [Route("signup")]
    public class SignUpController : Controller
    {
        private readonly IConfiguration _configuration;

        public SignUpController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View("~/Views/App/SignUp.cshtml", new User());
        }

        [HttpPost]
        public IActionResult CreateUser(string first_name, string last_name, string email_address, string password, string confirm_password)
        {
            User user = new User
            {
                FirstName = first_name,
                LastName = last_name,
                EmailAddress = email_address,
                Password = password
            };

            if (string.IsNullOrWhiteSpace(first_name) ||
                string.IsNullOrWhiteSpace(last_name) ||
                string.IsNullOrWhiteSpace(email_address) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewData["ErrorMessage"] = "All fields are required";
                return View("~/Views/App/SignUp.cshtml", user);
            }

            if (password.Length < 4)
            {
                ViewData["ErrorMessage"] = "Password must be at least 4 characters";
            }

            var passwordRegex = new Regex(@"^(?=.*[a-zA-Z])(?=.*[0-9])(?=.*[\W_]).+$");
            if (!passwordRegex.IsMatch(password))
            {
                ViewData["ErrorMessage"] = "Password must contain at least one letter, one number, and one special character.";
                return View("~/Views/App/SignUp.cshtml", user);
            }

            if (password != confirm_password)
            {
                ViewData["ErrorMessage"] = "Passwords do not match";
                return View("~/Views/App/SignUp.cshtml", user);
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            
            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "INSERT INTO users " +
                                   "(user_first_name, user_last_name, user_email_address, user_password) VALUES" +
                                   "(@first_name, @last_name, @email_address, @password);";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@first_name", user.FirstName);
                        command.Parameters.AddWithValue("@last_name", user.LastName);
                        command.Parameters.AddWithValue("@email_address", user.EmailAddress);
                        command.Parameters.AddWithValue("@password", user.Password);

                        command.ExecuteNonQuery();
                    } 
                }

                var secretKey = _configuration["JwtSettings:SecretKey"];

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("sub", email_address),
                        new Claim("email_address", email_address)
                    }),
                    Expires = DateTime.UtcNow.AddHours(2),
                    SigningCredentials = credentials
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                string jwtToken = tokenHandler.WriteToken(token);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTime.UtcNow.AddHours(2),
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };
                Response.Cookies.Append("jwtToken", jwtToken, cookieOptions);

                return RedirectToAction("FinanceOverview", "FinanceOverview");

            }
            catch (Exception ex) 
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~/Views/App/SignUp.cshtml", user);
            }
        }
    }
}
