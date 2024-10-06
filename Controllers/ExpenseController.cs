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

        [HttpPost]
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
    }
}
