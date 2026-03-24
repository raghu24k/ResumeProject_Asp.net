using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ResumeProject.Controllers
{
    [Authorize(AuthenticationSchemes = "CookieAuth")]
    public class InterviewPrepController : Controller
    {
        public IActionResult Index(string tab = "hr", string domain = "Frontend Development")
        {
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            ViewBag.Tab = tab;
            ViewBag.Domain = domain;
            return View();
        }
    }
}
