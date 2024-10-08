using FinanceManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace FinanceManagementApp.Controllers
{
    [Route("expense")]
    public class ExpenseController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HandleToken _handleToken;

        public ExpenseController(IConfiguration configuration)
        {
            _configuration = configuration;
            _handleToken = new HandleToken(configuration);
        }

        [HttpGet("addnew")]
        public IActionResult AddNewExpenseForm()
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            return View("~/Views/App/AddNewExpenseForm.cshtml", new Expense());
        }

        [HttpPost("addnew")]
        public IActionResult AddNewExpense(string description, double amount)
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

            Expense expense = new Expense
            {
                UserId = Convert.ToInt32(userId),
                Description = description,
                Amount = roundedAmount,
                TransactionDate = currentDate
            };

            if (string.IsNullOrEmpty(description) || amount == 0)
            {
                ViewData["ErrorMessage"] = "All fields are required";
                return View("~/Views/App/AddNewExpenseForm.cshtml", expense);
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string saveNewExpense = "INSERT INTO expenses " +
                                            "(user_id, expense_description, expense_amount, transaction_date) VALUES " +
                                            "(@user_id, @description, @amount, @transaction_date);";

                    using (SqlCommand command = new SqlCommand(saveNewExpense, connection))
                    {
                        command.Parameters.AddWithValue("@user_id", expense.UserId);
                        command.Parameters.AddWithValue("@description", expense.Description);
                        command.Parameters.AddWithValue("@amount", expense.Amount);
                        command.Parameters.AddWithValue("@transaction_date", expense.TransactionDate);

                        command.ExecuteNonQuery();
                    }
                }

                ViewData["ErrorMessage"] = "";

                return RedirectToAction("FinanceOverview", "FinanceOverview");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~/Views/App/AddNewExpenseForm.cshtml", expense);
            }
        }

        [HttpGet("editexpense/{id}")]
        public IActionResult EditExpenseForm(int id)
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

                    string findExpenseQuery = "SELECT TOP 1 expense_description, expense_amount " +
                                             "FROM expenses " +
                                             "WHERE expense_id=@expense_id " +
                                             "AND user_id=@user_id " +
                                             "AND MONTH(transaction_date) = MONTH(GETDATE()) " +
                                             "AND YEAR(transaction_date) = YEAR(GETDATE());";

                    using (SqlCommand command = new SqlCommand(findExpenseQuery, connection))
                    {
                        command.Parameters.AddWithValue("@expense_id", id);
                        command.Parameters.AddWithValue("@user_id", userId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Expense expense = new Expense
                                {
                                    Id = id,
                                    Description = reader.GetString(0),
                                    Amount = reader.GetDouble(reader.GetOrdinal("expense_amount"))
                                };

                                return View("~/Views/App/EditExpenseForm.cshtml", expense);
                            }
                            else
                            {
                                ViewData["ErrorMessage"] = "Error trying to update expense...";
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

        [HttpPost("editexpense")]
        public IActionResult EditIncome(int id, string description, double amount)
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            double roundedAmount = Math.Floor(amount * 100) / 100;

            Expense expense = new Expense
            {
                Id = id,
                Description = description,
                Amount = roundedAmount
            };

            if (string.IsNullOrEmpty(description) ||
                amount == 0)
            {
                ViewData["ErrorMessage"] = "All fields are required";
                return View("~/Views/App/EditExpenseForm.cshtml", expense);
            }

            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string editExpenseQuery = "UPDATE expenses " +
                                              "SET expense_description=@description, expense_amount=@amount " +
                                              "WHERE expense_id=@id;";

                    using (SqlCommand command = new SqlCommand(editExpenseQuery, connection))
                    {
                        command.Parameters.AddWithValue("@description", expense.Description);
                        command.Parameters.AddWithValue("@amount", expense.Amount);
                        command.Parameters.AddWithValue("@id", expense.Id);

                        command.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("FinanceOverview", "FinanceOverview");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~Views/App/EditExpenseForm.cshtml", expense);
            }
        }
    }
}
