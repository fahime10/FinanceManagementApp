using Microsoft.AspNetCore.Mvc;
using FinanceManagementApp.Models;
using System.Data.SqlClient;

namespace FinanceManagementApp.Controllers
{
    [Route("financereport")]
    public class FinanceReportController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HandleToken _handleToken;

        public FinanceReportController(IConfiguration configuration)
        {
            _configuration = configuration;
            _handleToken = new HandleToken(configuration);
        }

        [HttpGet("financeyearlyreport")]
        public IActionResult FinanceReportView()
        {
            string jwtToken = Request.Cookies["jwtToken"];
            bool isTokenValid = _handleToken.IsTokenValid(jwtToken);

            if (!isTokenValid)
            {
                return RedirectToAction("Login", "Login");
            }

            var userId = _handleToken.ExtractIdFromToken(jwtToken);

            var allFinances = new Dictionary<int, FinanceReport>();

            try 
            {
                string connectionString = _configuration["ConnectionStrings:DefaultConnection"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    for (int month = 1; month <= 12; month++)
                    {
                        var financeReport = new FinanceReport();

                        string findIncomesQuery = "SELECT * " +
                                                  "FROM incomes " +
                                                  "WHERE user_id = @user_id " +
                                                  "AND MONTH(transaction_date) = @month " +
                                                  "AND YEAR(transaction_date) = YEAR(GETDATE());";

                        using (SqlCommand findIncomesCommand = new SqlCommand(findIncomesQuery, connection))
                        {
                            findIncomesCommand.Parameters.AddWithValue("@user_id", userId);
                            findIncomesCommand.Parameters.AddWithValue("@month", month);

                            using (SqlDataReader incomeReader = findIncomesCommand.ExecuteReader())
                            {
                                while (incomeReader.Read())
                                {
                                    var income = new Income
                                    {
                                        Id = incomeReader.GetInt32(incomeReader.GetOrdinal("income_id")),
                                        Description = incomeReader.GetString(incomeReader.GetOrdinal("income_description")),
                                        Amount = incomeReader.GetDouble(incomeReader.GetOrdinal("income_amount")),
                                        TransactionDate = incomeReader.GetDateTime(incomeReader.GetOrdinal("transaction_date"))
                                    };
                                    financeReport.Incomes.Add(income);
                                }
                            }
                        }

                        string findExpensesQuery = "SELECT * " +
                                                   "FROM expenses " +
                                                   "WHERE user_id = @user_id " +
                                                   "AND MONTH(transaction_date) = @month " +
                                                   "AND YEAR(transaction_date) = YEAR(GETDATE());";

                        using (SqlCommand findExpenseCommand = new SqlCommand(findExpensesQuery, connection))
                        {
                            findExpenseCommand.Parameters.AddWithValue("@user_id", userId);
                            findExpenseCommand.Parameters.AddWithValue("@month", month);

                            using (SqlDataReader expenseReader = findExpenseCommand.ExecuteReader())
                            {
                                while (expenseReader.Read())
                                {
                                    var expense = new Expense
                                    {
                                        Id = expenseReader.GetInt32(expenseReader.GetOrdinal("expense_id")),
                                        Description = expenseReader.GetString(expenseReader.GetOrdinal("expense_description")),
                                        Amount = expenseReader.GetDouble(expenseReader.GetOrdinal("expense_amount")),
                                        TransactionDate = expenseReader.GetDateTime(expenseReader.GetOrdinal("transaction_date"))
                                    };
                                    financeReport.Expenses.Add(expense);
                                }
                            }
                        }

                        string findBudgetsQuery = "SELECT * " +
                                                  "FROM budgets " +
                                                  "WHERE user_id = @user_id " +
                                                  "AND MONTH(budget_created_at) = @month " +
                                                  "AND YEAR(budget_created_at) = YEAR(GETDATE());";

                        using (SqlCommand findBudgetCommand = new SqlCommand(findBudgetsQuery, connection))
                        {
                            findBudgetCommand.Parameters.AddWithValue("@user_id", userId);
                            findBudgetCommand.Parameters.AddWithValue("@month", month);

                            using (SqlDataReader budgetReader = findBudgetCommand.ExecuteReader())
                            {
                                if (budgetReader.Read())
                                {
                                    var budget = new Budget
                                    {
                                        Amount = budgetReader.GetDouble(budgetReader.GetOrdinal("budget_amount")),
                                        CreatedAt = budgetReader.GetDateTime(budgetReader.GetOrdinal("budget_created_at"))
                                    };
                                    financeReport.Budget = budget;
                                }
                            }
                        }

                        allFinances[month] = financeReport;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "Error retrieving details...";
                return View("~/Views/App/FinanceReportView.cshtml");
            }

            return View("~/Views/App/FinanceReportView.cshtml", allFinances);
        }
    }
}
