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

        [HttpGet]
        public IActionResult FinanceOverview()
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);
            double budget = 0;
            double incomesTotal = 0;
            double expensesTotal = 0;

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

                    string findBudgetQuery = "SELECT TOP 1 budget_id, budget_amount " +
                                             "FROM budgets " +
                                             "WHERE user_id = @user_id " +
                                             "AND MONTH(budget_created_at) = MONTH(GETDATE()) " +
                                             "AND YEAR(budget_created_at) = YEAR(GETDATE())";

                    string findIncomesQuery = "SELECT income_id, income_description, income_amount " +
                                              "FROM incomes " +
                                              "WHERE user_id = @user_id " +
                                              "AND MONTH(transaction_date) = MONTH(GETDATE()) " +
                                              "AND YEAR(transaction_date) = YEAR(GETDATE());";

                    string findExpensesQuery = "SELECT expense_id, expense_description, expense_amount " +
                                               "FROM expenses " +
                                               "WHERE user_id = @user_id " +
                                               "AND MONTH(transaction_date) = MONTH(GETDATE()) " +
                                               "AND YEAR(transaction_date) = YEAR(GETDATE());";

                    using (SqlCommand budgetCommand = new SqlCommand(findBudgetQuery, connection))
                    {
                        budgetCommand.Parameters.AddWithValue("@user_id", userId);

                        using (SqlDataReader budgetReader = budgetCommand.ExecuteReader())
                        {
                            if (budgetReader.Read())
                            {
                                double budgetAmount = budgetReader.GetDouble(1);
                                ViewData["BudgetId"] = budgetReader.GetInt32(0);
                                ViewData["Budget"] = budgetAmount;
                                budget = budgetAmount;
                            } else
                            {
                                ViewData["Budget"] = "Not set";
                                ViewData["BudgetId"] = null;
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

                                incomesTotal += incomeAmount;

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
                                double expenseAmount = expenseReader.GetDouble(2);

                                expensesTotal += expenseAmount;

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

                if (expensesTotal > incomesTotal)
                {
                    ViewData["Info"] = "Your expenses are exceeding your income by £" + (expensesTotal - incomesTotal);
                } 
                else if (expensesTotal > budget && budget != 0)
                {
                    ViewData["Info"] = "You are overbudget by £" + (expensesTotal - budget);
                }
                else
                {
                    ViewData["Info"] = "You are within budget";
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                Console.WriteLine(ex.Message);
            }

            return View("~/Views/App/FinanceOverview.cshtml");
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwtToken");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("deleteincome")]
        public IActionResult DeleteIncome(int id)
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string deleteIncomeQuery = "DELETE FROM incomes WHERE income_id=@income_id;";

                    using (SqlCommand command = new SqlCommand(deleteIncomeQuery, connection))
                    {
                        command.Parameters.AddWithValue("@income_id", id);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected  > 0)
                        {
                            ViewData["SuccessMessage"] = "Income record deleted successfully";
                        }
                        else
                        {
                            ViewData["ErrorMessage"] = "Failed to delete income record";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error deleting income: " + ex.Message;
            }
            return RedirectToAction("FinanceOverview");
        }

        [HttpPost("deleteexpense")]
        public IActionResult DeleteExpense(int id)
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string deleteExpenseQuery = "DELETE FROM expenses WHERE expense_id=@expense_id;";

                    using (SqlCommand command = new SqlCommand(deleteExpenseQuery, connection))
                    {
                        command.Parameters.AddWithValue("@expense_id", id);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            ViewData["SuccessMessage"] = "Expense record deleted successfully";
                        }
                        else
                        {
                            ViewData["ErrorMessage"] = "Failed to delete expense record";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error deleting expense: " + ex.Message;
            }
            return RedirectToAction("FinanceOverview");
        }

        [HttpPost("deletebudget")]
        public IActionResult DeleteBudget(int id)
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string deleteBudgetQuery = "DELETE FROM budgets WHERE budget_id=@budget_id;";

                    using (SqlCommand command = new SqlCommand(deleteBudgetQuery, connection))
                    {
                        command.Parameters.AddWithValue("@budget_id", id);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            ViewData["SuccessMessage"] = "Budget record deleted successfully";
                            ViewData["Budget"] = "Not set";
                            ViewData["BudgetId"] = null;
                        }
                        else
                        {
                            ViewData["ErrorMessage"] = "Failed to delete budget record";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error deleting budget: " + ex.Message;
            }
            return RedirectToAction("FinanceOverview");
        }
    }
}
