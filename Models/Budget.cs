namespace FinanceManagementApp.Models
{
    public class Budget
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public float Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
