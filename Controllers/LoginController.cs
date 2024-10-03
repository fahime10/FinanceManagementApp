using Microsoft.AspNetCore.Mvc;
using FinanceManagementApp.Models;
using System.Data;
using System.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;

namespace FinanceManagementApp.Controllers
{
    [Route("login")]
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult LoginPage()
        {
            return View("~/Views/App/LoginPage.cshtml", new User());
        }

        [HttpPost]
        public IActionResult Login(string email_address,  string password)
        {
            User user = new User
            {
                EmailAddress = email_address,
                Password = password
            };

            if (string.IsNullOrEmpty(email_address) ||
                string.IsNullOrEmpty(password))
            {
                ViewData["ErrorMessage"] = "All fields are required";
                return View("~/Views/App/LoginPage.cshtml", user);
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string findUserQuery = "SELECT TOP 1 user_email_address, user_password FROM users " +
                                           "WHERE user_email_address = @email_address ";

                    using (SqlCommand command = new SqlCommand(findUserQuery, connection))
                    {
                        command.Parameters.Add("@email_address", SqlDbType.NVarChar).Value = email_address;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string hashedPassword = reader["user_password"].ToString();

                                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, hashedPassword);

                                if (isPasswordCorrect)
                                {
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
                                else
                                {
                                    ViewData["ErrorMessage"] = "Incorrect credentials";
                                    return View("~/Views/App/LoginPage.cshtml", user);
                                }
                            }
                            else
                            {
                                ViewData["ErrorMessage"] = "User does not exist";
                                return View("~/Views/App/LoginPage.cshtml", user);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~/Views/App/LoginPage.cshtml", user);
            }
        }
    }
}
