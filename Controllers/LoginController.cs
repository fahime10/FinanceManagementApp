using Microsoft.AspNetCore.Mvc;
using FinanceManagementApp.Models;
using System.Data;
using System.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;

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

        [HttpGet("loginform")]
        public IActionResult LoginForm()
        {
            return View("~/Views/App/LoginForm.cshtml", new User());
        }

        [HttpPost("login")]
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
                return View("~/Views/App/LoginForm.cshtml", user);
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string findUserQuery = "SELECT TOP 1 user_id, user_email_address, user_password FROM users " +
                                           "WHERE user_email_address = @email_address ";

                    using (SqlCommand command = new SqlCommand(findUserQuery, connection))
                    {
                        command.Parameters.Add("@email_address", SqlDbType.NVarChar).Value = email_address;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["user_id"]);
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
                                            new Claim("sub", userId.ToString()),
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
                                    return View("~/Views/App/LoginForm.cshtml", user);
                                }
                            }
                            else
                            {
                                ViewData["ErrorMessage"] = "User does not exist";
                                return View("~/Views/App/LoginForm.cshtml", user);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~/Views/App/LoginForm.cshtml", user);
            }
        }

        private string GenerateRandomCode(int length)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            Random random = new Random();

            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void SendPasscode(string recipientEmail, string passcode)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_configuration["GmailSettings:EmailAddress"], _configuration["GmailSettings:AppPassword"]),
                EnableSsl = true
            };

            var emailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["GmailSettings:EmailAddress"]),
                Subject = "Passcode for resetting password",
                Body = $"To reset your password, use this passcode in the relevant form:\n{passcode}",
                IsBodyHtml = false
            };

            emailMessage.To.Add(recipientEmail);

            try
            {
                smtpClient.Send(emailMessage);
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Email could not be sent...";
            }
        }

        [HttpGet("passcoderequestform")]
        public IActionResult PasscodeRequestForm()
        {
            return View("~/Views/App/PasscodeRequestForm.cshtml", new User());
        }

        [HttpPost("verifyemail")]
        public IActionResult VerifyEmail(string email_address)
        {
            User user = new User
            {
                EmailAddress = email_address
            };

            if (string.IsNullOrEmpty(email_address))
            {
                ViewData["ErrorMessage"] = "Username is required";
                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string findUserQuery = "SELECT TOP 1 user_id " +
                                           "FROM users " +
                                           "WHERE user_email_address=@email_address;";

                    using (SqlCommand findUserCommand = new SqlCommand(findUserQuery, connection))
                    {
                        findUserCommand.Parameters.AddWithValue("@email_address", user.EmailAddress);

                        using (SqlDataReader reader = findUserCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user.Id = reader.GetInt32(0);

                                reader.Close();

                                string passcode = GenerateRandomCode(8);

                                string insertPasscodeQuery = "UPDATE users " +
                                                             "SET user_passcode=@passcode " +
                                                             "WHERE user_id=@user_id;";

                                using (SqlCommand insertPasscodeCommand = new SqlCommand(insertPasscodeQuery, connection))
                                {
                                    insertPasscodeCommand.Parameters.AddWithValue("@passcode", passcode);
                                    insertPasscodeCommand.Parameters.AddWithValue("@user_id", user.Id);

                                    insertPasscodeCommand.ExecuteNonQuery();

                                    SendPasscode(user.EmailAddress, passcode);
                                    ViewData["IsEmailVerified"] = true;
                                }
                            }
                            else
                            {
                                ViewData["ErrorMessage"] = "This email address has not been found";
                                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
                            }
                        }
                    }
                }

                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
            }
        }

        [HttpPost("resetpassword")]
        public IActionResult ResetPassword(string email_address, string passcode, string password, string confirm_password)
        {
            User user = new User
            {
                EmailAddress = email_address,
                Password = password
            };

            if (string.IsNullOrEmpty(passcode) ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(confirm_password))
            {
                ViewData["ErrorMessage"] = "All fields are required";
                ViewData["IsEmailVerified"] = true;
                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
            }

            if (password.Length < 4)
            {
                ViewData["ErrorMessage"] = "Password must be at least 4 characters";
                ViewData["IsEmailVerified"] = true;
                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
            }

            var passwordRegex = new Regex(@"^(?=.*[a-zA-Z])(?=.*[0-9])(?=.*[\W_]).+$");
            if (!passwordRegex.IsMatch(password))
            {
                ViewData["ErrorMessage"] = "Password must contain at least one letter, one number, and one special character.";
                ViewData["IsEmailVerified"] = true;
                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
            }

            if (password != confirm_password)
            {
                ViewData["ErrorMessage"] = "Passwords do not match";
                ViewData["IsEmailVerified"] = true;
                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string verifyPasscodeQuery = "SELECT TOP 1 user_id " +
                                                 "FROM users " +
                                                 "WHERE user_email_address=@email_address " +
                                                 "AND user_passcode=@passcode;";

                    using (SqlCommand verifyPasscodeCommand = new SqlCommand(verifyPasscodeQuery, connection))
                    {
                        verifyPasscodeCommand.Parameters.AddWithValue("@email_address", user.EmailAddress);
                        verifyPasscodeCommand.Parameters.AddWithValue("@passcode", passcode);

                        using (SqlDataReader reader = verifyPasscodeCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string resetPasswordQuery = "UPDATE users " +
                                                            "SET user_password=@password " +
                                                            "WHERE user_email_address=@email_address";

                                reader.Close();

                                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

                                using (SqlCommand resetPasswordCommand = new SqlCommand(resetPasswordQuery, connection))
                                {
                                    resetPasswordCommand.Parameters.AddWithValue("@password", user.Password);
                                    resetPasswordCommand.Parameters.AddWithValue("@email_address", user.EmailAddress);

                                    resetPasswordCommand.ExecuteNonQuery();

                                    return View("~/Views/App/LoginForm.cshtml", user);
                                }
                            }
                            else
                            {
                                ViewData["ErrorMessage"] = "Passcode does not match";
                                ViewData["IsEmailVerified"] = true;
                                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Somethng went wrong...";
                ViewData["IsEmailVerified"] = true;
                return View("~/Views/App/PasscodeRequestForm.cshtml", user);
            }
        }
    }
}
