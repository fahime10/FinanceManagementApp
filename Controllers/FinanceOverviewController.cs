using Microsoft.AspNetCore.Mvc;
using FinanceManagementApp.Models;
using System.Data.SqlClient;

namespace FinanceManagementApp.Controllers
{
    [Route("financeoverview")]
    public class FinanceOverviewController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HandleToken _handleToken;

        public FinanceOverviewController(IConfiguration configuration)
        {
            _configuration = configuration;
            _handleToken = new HandleToken(configuration);
        }

        public IActionResult FinanceOverview()
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

                    DateTime currentDate = DateTime.Now;

                    string findBudgetQuery = "SELECT TOP 1 budget_amount " +
                                             "FROM budgets " +
                                             "WHERE user_id = @user_id " +
                                             "AND @currentDate >= budget_start_date " +
                                             "AND @currentDate <= budget_end_date";

                    string findIncomesQuery = "SELECT income_id, income_description, income_amount " +
                                              "FROM incomes " +
                                              "WHERE user_id = @user_id " +
                                              "AND MONTH(transaction_date) = MONTH(GETDATE()) " +
                                              "AND YEAR(transaction_date) = YEAR(GETDATE()) " +
                                              "ORDER BY transaction_date DESC";

                    string findExpensesQuery = "SELECT expense_id, expense_description, expense_amount " +
                                               "FROM expenses " +
                                               "WHERE user_id = @user_id " +
                                               "AND MONTH(transaction_date) = MONTH(GETDATE()) " +
                                               "AND YEAR(transaction_date) = YEAR(GETDATE()) " +
                                               "ORDER BY transaction_date DESC";

                    using (SqlCommand budgetCommand = new SqlCommand(findBudgetQuery, connection))
                    {
                        budgetCommand.Parameters.AddWithValue("@user_id", userId);
                        budgetCommand.Parameters.AddWithValue("@currentDate", currentDate);

                        using (SqlDataReader budgetReader = budgetCommand.ExecuteReader())
                        {
                            if (budgetReader.Read())
                            {
                                float budgetAmount = budgetReader.GetFloat(0);
                                ViewData["Budget"] = budgetAmount;
                            } else
                            {
                                ViewData["Budget"] = "Not set";
                            }
                        }
                    }

                    using (SqlCommand incomeCommand = new SqlCommand(findIncomesQuery, connection))
                    {
                        incomeCommand.Parameters.AddWithValue("@user_id", userId);

                        using (SqlDataReader incomeReader = incomeCommand.ExecuteReader())
                        {
                            List<Income> incomes = new List<Income>();

                            while (incomeReader.Read())
                            {
                                int incomeId = incomeReader.GetInt32(0);
                                string incomeDescription = incomeReader.GetString(1);
                                double incomeAmount = incomeReader.GetDouble(2);

                                incomes.Add(new Income
                                {
                                    Id = incomeId,
                                    Description = incomeDescription,
                                    Amount = incomeAmount
                                });
                            }

                            ViewData["Incomes"] = incomes;
                        }
                    }

                    using (SqlCommand expenseCommand = new SqlCommand(findExpensesQuery, connection))
                    {
                        expenseCommand.Parameters.AddWithValue("@user_id", userId);

                        using (SqlDataReader expenseReader = expenseCommand.ExecuteReader())
                        {
                            List<Expense> expenses = new List<Expense>();

                            while (expenseReader.Read())
                            {
                                int expenseId = expenseReader.GetInt32(0);
                                string expenseDescription = expenseReader.GetString(1);
                                float expenseAmount = expenseReader.GetFloat(2);

                                expenses.Add(new Expense
                                {
                                    Id = expenseId,
                                    Description = expenseDescription,
                                    Amount = expenseAmount
                                });
                            }

                            ViewData["Expenses"] = expenses;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                Console.WriteLine(ex.Message);
            }

            return View("~/Views/App/FinanceOverview.cshtml");
        }

        [HttpPost]
        public IActionResult DeleteIncome(int id)
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string deleteIncomeQuery = "DELETE FROM incomes WHERE income_id = @income_id;";

                    using (SqlCommand command = new SqlCommand(deleteIncomeQuery, connection))
                    {
                        command.Parameters.AddWithValue("@income_id", id);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected  > 0)
                        {
                            TempData["SuccessMessage"] = "Income record deleted successfully";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to delete income record";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting income: " + ex.Message;
            }
            return RedirectToAction("FinanceOverview");
        }
    }
}
