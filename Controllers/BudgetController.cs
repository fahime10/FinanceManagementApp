using Microsoft.AspNetCore.Mvc;
using FinanceManagementApp.Models;
using System.Data.SqlClient;

namespace FinanceManagementApp.Controllers
{
    [Route("budget")]
    public class BudgetController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HandleToken _handleToken;

        public BudgetController(IConfiguration configuration)
        {
            _configuration = configuration;
            _handleToken = new HandleToken(configuration);
        }

        [HttpGet("setnew")]
        public IActionResult BudgetForm()
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            var userId = _handleToken.ExtractIdFromToken(jwtToken);

            Budget budget = new Budget
            {
                UserId = Convert.ToInt32(userId)
            };

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string findBudgetQuery = "SELECT TOP 1 budget_amount " +
                                             "FROM budgets " +
                                             "WHERE user_id = @user_id " +
                                             "AND MONTH(budget_created_at) = MONTH(GETDATE()) " +
                                             "AND YEAR(budget_created_at) = YEAR(GETDATE())";

                    using (SqlCommand command = new SqlCommand(findBudgetQuery, connection))
                    {
                        command.Parameters.AddWithValue("@user_id", budget.UserId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                budget.Amount = reader.GetDouble(reader.GetOrdinal("budget_amount"));

                                ViewData["ErrorMessage"] = null;

                                return View("~/Views/App/BudgetForm.cshtml", budget);
                            }
                            else
                            {
                                return View("~/Views/App/BudgetForm.cshtml", new Budget());
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
        }

        [HttpPost]
        public IActionResult SetBudget(double amount)
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            var userId = _handleToken.ExtractIdFromToken(jwtToken);

            double roundedAmount = Math.Floor(amount * 100) / 100;

            DateTime currentDate = DateTime.Now;

            bool foundBudget = false;

            Budget budget = new Budget
            {
                UserId = Convert.ToInt32(userId),
                Amount = roundedAmount,
                CreatedAt = currentDate
            };

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string findBudgetQuery = "SELECT TOP 1 budget_amount " +
                                             "FROM budgets " +
                                             "WHERE user_id = @user_id " +
                                             "AND MONTH(budget_created_at) = MONTH(GETDATE()) " +
                                             "AND YEAR(budget_created_at) = YEAR(GETDATE())";

                    using (SqlCommand findBudgetCommand = new SqlCommand(findBudgetQuery, connection))
                    {
                        findBudgetCommand.Parameters.AddWithValue("@user_id", budget.UserId);

                        using (SqlDataReader reader = findBudgetCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                foundBudget = true;
                            }
                        }

                        if (!foundBudget)
                        {
                            string setBudgetQuery = "INSERT INTO budgets " +
                                                    "(user_id, budget_amount, budget_created_at) VALUES " +
                                                    "(@user_id, @amount, @created_at);";

                            using (SqlCommand newBudgetCommand = new SqlCommand(setBudgetQuery, connection))
                            {
                                newBudgetCommand.Parameters.AddWithValue("@user_id", budget.UserId);
                                newBudgetCommand.Parameters.AddWithValue("@amount", budget.Amount);
                                newBudgetCommand.Parameters.AddWithValue("@created_at", budget.CreatedAt);

                                newBudgetCommand.ExecuteNonQuery();
                            }

                        }
                        else
                        {
                            string setBudgetQuery = "UPDATE budgets " +
                                                    "SET budget_amount=@amount " +
                                                    "WHERE user_id=@user_id " +
                                                    "AND MONTH(budget_created_at) = MONTH(GETDATE()) " +
                                                    "AND YEAR(budget_created_at) = YEAR(GETDATE());";

                            using (SqlCommand setBudgetCommand = new SqlCommand(setBudgetQuery, connection))
                            {
                                setBudgetCommand.Parameters.AddWithValue("@amount", budget.Amount);
                                setBudgetCommand.Parameters.AddWithValue("@user_id", budget.UserId);

                                setBudgetCommand.ExecuteNonQuery();
                            }
                        }

                        ViewData["ErrorMessage"] = null;

                        return RedirectToAction("FinanceOverview", "FinanceOverview");
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~Views/App/BudgetForm.cshtml", budget);
            }
        }
    }
}
