namespace FinanceManagementApp.Models
{
    public class FinanceReport
    {
        public List<Income> Incomes { get; set; }
        public List<Expense> Expenses { get; set; }
        public Budget Budget { get; set; }

        public FinanceReport()
        {
            Incomes = new List<Income>();
            Expenses = new List<Expense>();
            Budget = new Budget();
        }
    }
}
