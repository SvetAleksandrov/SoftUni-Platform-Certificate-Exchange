namespace SoftUni_Platform_Certificate_Exchange.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Публични страници за HTTP грешки
[AllowAnonymous]
public class ErrorController : Controller
{
    // Обработва HTTP статуси без съдържание
    [ResponseCache(Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Status(int code)
    {
        if (code == 404)
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }

        // Запазваме останалите HTTP статуси
        return StatusCode(code >= 400 && code <= 599
            ? code : 404);
    }

    // Показва страница при вътрешна сървърна грешка
    [ResponseCache(Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult ServerError()
    {
        Response.StatusCode = 500;
        return View();
    }
}
