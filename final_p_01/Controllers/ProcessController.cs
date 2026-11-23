using Microsoft.AspNetCore.Mvc;

namespace final_p_01.Controllers;

[Route("process")]
public class ProcessController : Controller
{
    [HttpGet]
    [HttpGet("/execution-steps")]
    public IActionResult Index() => RedirectPermanent("/projects");
}
