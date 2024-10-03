using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;

namespace FinanceManagementApp.Controllers
{
    [Route("financeoverview")]
    public class FinanceOverviewController : Controller
    {
        private readonly IConfiguration _configuration;

        public FinanceOverviewController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult FinanceOverview()
        {
            var jwtToken = Request.Cookies["jwtToken"];

            if (string.IsNullOrEmpty(jwtToken))
            {
                return RedirectToAction("Login", "Login");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtSettings:SecretKey"];
            var key = Encoding.UTF8.GetBytes(secretKey);

            try
            {
                tokenHandler.ValidateToken(jwtToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                }, out SecurityToken validatedToken);

                return View("~/Views/App/FinanceOverview.cshtml");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Login", "Login");
            }
        }
    }
}
