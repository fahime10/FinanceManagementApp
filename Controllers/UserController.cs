using Microsoft.AspNetCore.Mvc;
using FinanceManagementApp.Models;
using System.Data.SqlClient;

namespace FinanceManagementApp.Controllers
{
    [Route("user")]
    public class UserController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HandleToken _handleToken;

        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
            _handleToken = new HandleToken(configuration);
        }

        [HttpPost("edituserform")]
        public IActionResult EditUserForm()
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            var userId = _handleToken.ExtractIdFromToken(jwtToken);

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string findUserQuery = "SELECT TOP 1 user_first_name, user_last_name, user_email_address " +
                                      "FROM users " +
                                      "WHERE user_id=@user_id";

                    using (SqlCommand command = new SqlCommand(findUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@user_id", userId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                User user = new User
                                {
                                    Id = Convert.ToInt32(userId),
                                    FirstName = reader.GetString(0),
                                    LastName = reader.GetString(1),
                                    EmailAddress = reader.GetString(2)
                                };

                                return View("~/Views/App/EditUserForm.cshtml", user);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return RedirectToAction("FinanceOverview", "FinanceOverview");
            }

            ViewData["ErrorMessage"] = "Error loading user information...";
            return RedirectToAction("FinanceOverview", "FinanceOverview");
        }

        [HttpPost("edituser")]
        public IActionResult EditUser(int id, string first_name, string last_name, string email_address)
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            User user = new User
            {
                Id = id,
                FirstName = first_name,
                LastName = last_name,
                EmailAddress = email_address
            };

            if (string.IsNullOrEmpty(first_name) ||
                string.IsNullOrEmpty(last_name) ||
                string.IsNullOrEmpty(email_address)) 
            {
                ViewData["ErrorMessage"] = "All fields are required";
                return View("~/Views/App/EditUserForm.cshtml", user);
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string findUserQuery = "UPDATE users " +
                                      "SET user_first_name=@first_name, user_last_name=@last_name, user_email_address=@email_address " +
                                      "WHERE user_id=@user_id;";

                    using (SqlCommand command = new SqlCommand(findUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@first_name", user.FirstName);
                        command.Parameters.AddWithValue("@last_name", user.LastName);
                        command.Parameters.AddWithValue("@email_address", user.EmailAddress);
                        command.Parameters.AddWithValue("@user_id", user.Id);

                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("FinanceOverview", "FinanceOverview");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~/Views/App/EditUserForm.cshtml", user);
            }
        }

        [HttpPost("deleteuser")]
        public IActionResult DeleteUser(int id)
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string deleteUserQuery = "DELETE FROM users WHERE user_id=@user_id;";

                    using (SqlCommand command = new SqlCommand(deleteUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@user_id", id);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Response.Cookies.Delete("jwtToken");
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error deleting user: " + ex.Message;
            }
            return RedirectToAction("FinanceOverview");
        }
    }
}
