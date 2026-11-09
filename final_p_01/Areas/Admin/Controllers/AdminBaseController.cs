using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace final_p_01.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public abstract class AdminBaseController : Controller
{
}