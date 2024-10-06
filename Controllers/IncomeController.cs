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

        [HttpPost]
        public IActionResult AddNewIncome(string description, double amount)
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            var userId = _handleToken.ExtractIdFromToken(jwtToken);

            DateTime currentDate = DateTime.Now;

            Income income = new Income
            {
                UserId = Convert.ToInt32(userId),
                Description = description,
                Amount = amount,
                TransactionDate = currentDate
            };

            if (string.IsNullOrEmpty(description) || amount == 0)
            {
                ViewData["ErrorMessage"] = "All fields are required";
                return View("~Views/App/AddNewIncomeForm.cshtml", income);
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

                return RedirectToAction("FinanceOverview", "FinanceOverview");

            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("~/Views/App/AddNewIncomeForm.cshtml", income);
            }
        }
    }
}
