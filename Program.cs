namespace FinanceManagementApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Always enforce HTTPS redirection
            app.UseHttpsRedirection();

            // Enable HSTS for security
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "signup",
                pattern: "signup",
                defaults: new { controller = "SignUp", action = "SignUp" });

            app.MapControllerRoute(
                name: "login",
                pattern: "login",
                defaults: new { controller = "Login", action = "Login" });

            app.MapControllerRoute(
                name: "financeoverview",
                pattern: "financeoverview",
                defaults: new { controller = "FinanceOverview", action = "FinanceOverview" });

            app.MapControllerRoute(
                name: "addnewincome",
                pattern: "income/addnew",
                defaults: new { controller = "Income", action = "AddNewIncomeForm" });

            app.MapControllerRoute(
                name: "addnewexpense",
                pattern: "expense/addnew",
                defaults: new { controller = "Expense", action = "AddNewExpenseForm" });

            app.MapControllerRoute(
                name: "setbudget",
                pattern: "setbudget",
                defaults: new { controller = "Budget", action = "BudgetForm" });

            app.Run();
        }
    }
}
