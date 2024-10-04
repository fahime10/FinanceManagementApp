using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using FinanceManagementApp.Models;
using System.Data.SqlClient;
using System.Security.Claims;

namespace FinanceManagementApp.Controllers
{
    [Route("financeoverview")]
    public class FinanceOverviewController : Controller
    {
        private readonly IConfiguration _configuration;

        public FinanceOverviewController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult FinanceOverview()
        {
            string jwtToken = Request.Cookies["jwtToken"];

            if (!IsTokenValid(jwtToken))
            {
                return RedirectToAction("Login", "Login");
            }

            var userId = ExtractIdFromToken(jwtToken);

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    DateTime currentDate = DateTime.Now;

                    string findBudgetQuery = "SELECT TOP 1 budget_amount " +
                                             "FROM budgets " +
                                             "WHERE user_id = @user_id " +
                                             "AND @currentDate >= budget_start_date " +
                                             "AND @currentDate <= budget_end_date";

                    using (SqlCommand command = new SqlCommand(findBudgetQuery, connection))
                    {
                        command.Parameters.AddWithValue("@user_id", userId);
                        command.Parameters.AddWithValue("@currentDate", currentDate);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                float budgetAmount = reader.GetFloat(0);
                                ViewData["Budget"] = budgetAmount;
                            } else
                            {
                                ViewData["Budget"] = "Not set";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message + " " + userId;
                Console.WriteLine(ex.Message);
            }

            return View("~/Views/App/FinanceOverview.cshtml");
        }

        private bool IsTokenValid(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = _configuration["JwtSettings:SecretKey"];
                var key = Encoding.UTF8.GetBytes(secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };

                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private int? ExtractIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken != null)
            {
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
            }
            return null;
        }
    }
}
