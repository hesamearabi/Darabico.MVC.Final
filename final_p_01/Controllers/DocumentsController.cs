using Microsoft.AspNetCore.Mvc;

namespace final_p_01.Controllers;

[Route("technical-docs")]
public class DocumentsController : Controller
{
    [HttpGet]
    [HttpGet("/catalog")]
    [HttpGet("/datasheets")]
    [HttpGet("/faq")]
    [HttpGet("/faqs")]
    [HttpGet("download/{id:int}")]
    public IActionResult Index() => RedirectPermanent("/blog");
}
