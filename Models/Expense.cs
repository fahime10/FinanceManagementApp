namespace FinanceManagementApp.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string Description { get; set; }
        public float Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
