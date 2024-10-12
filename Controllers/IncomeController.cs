using Microsoft.AspNetCore.Mvc;
using FinanceManagementApp.Models;
using System.Data.SqlClient;

namespace FinanceManagementApp.Controllers
{
    [Route("income")]
    public class IncomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HandleToken _handleToken;

        public IncomeController(IConfiguration configuration)
        {
            _configuration = configuration;
            _handleToken = new HandleToken(configuration);
        }

        [HttpGet("addnew")]
        public IActionResult AddNewIncomeForm()
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            return View("~/Views/App/AddNewIncomeForm.cshtml", new Income());
        }

        [HttpPost("addnew")]
        public IActionResult AddNewIncome(string description, double amount)
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

            Income income = new Income
            {
                UserId = Convert.ToInt32(userId),
                Description = description,
                Amount = roundedAmount,
                TransactionDate = currentDate
            };

            if (string.IsNullOrEmpty(description) || amount == 0)
            {
                ViewData["ErrorMessage"] = "All fields are required";
                return View("~/Views/App/AddNewIncomeForm.cshtml", income);
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string saveNewIncome = "INSERT INTO incomes " +
                                           "(user_id, income_description, income_amount, transaction_date) VALUES " +
                                           "(@user_id, @description, @amount, @transaction_date);";

                    using (SqlCommand command = new SqlCommand(saveNewIncome, connection))
                    {
                        command.Parameters.AddWithValue("@user_id", income.UserId);
                        command.Parameters.AddWithValue("@description", income.Description);
                        command.Parameters.AddWithValue("@amount", income.Amount);
                        command.Parameters.AddWithValue("@transaction_date", income.TransactionDate);

                        command.ExecuteNonQuery();
                    }
                }

                ViewData["ErrorMessage"] = "";

                return RedirectToAction("FinanceOverview", "FinanceOverview");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~/Views/App/AddNewIncomeForm.cshtml", income);
            }
        }

        [HttpGet("editincome/{id}")]
        public IActionResult EditIncomeForm(int id)
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

                    string findIncomeQuery = "SELECT TOP 1 income_description, income_amount " +
                                             "FROM incomes " +
                                             "WHERE income_id = @income_id " +
                                             "AND user_id = @user_id " +
                                             "AND MONTH(transaction_date) = MONTH(GETDATE()) " +
                                             "AND YEAR(transaction_date) = YEAR(GETDATE());";

                    using (SqlCommand command = new SqlCommand(findIncomeQuery, connection))
                    {
                        command.Parameters.AddWithValue("@income_id", id);
                        command.Parameters.AddWithValue("@user_id", userId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Income income = new Income
                                {
                                    Id = id,
                                    Description = reader.GetString(0),
                                    Amount = reader.GetDouble(reader.GetOrdinal("income_amount"))
                                };

                                return View("~/Views/App/EditIncomeForm.cshtml", income);
                            } 
                            else
                            {
                                ViewData["ErrorMessage"] = "Error trying to update income...";
                                return RedirectToAction("FinanceOverview", "FinanceOverview");
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

        [HttpPost("editincome")]
        public IActionResult EditIncome(int id, string description, double amount)
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            double roundedAmount = Math.Floor(amount * 100) / 100;

            Income income = new Income
            {
                Id = id,
                Description = description,
                Amount = roundedAmount
            };

            if (string.IsNullOrEmpty(description) ||
                amount == 0)
            {
                ViewData["ErrorMessage"] = "All fields are required";
                return View("~/Views/App/EditIncomeForm.cshtml", income);
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string editIncomeQuery = "UPDATE incomes " +
                                             "SET income_description = @description, income_amount = @amount " +
                                             "WHERE income_id = @id;";

                    using (SqlCommand command = new SqlCommand(editIncomeQuery, connection))
                    {
                        command.Parameters.AddWithValue("@description", income.Description);
                        command.Parameters.AddWithValue("@amount", income.Amount);
                        command.Parameters.AddWithValue("@id", income.Id);

                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("FinanceOverview", "FinanceOverview");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~Views/App/EditIncomeForm.cshtml", income);
            }
        }
    }
}
