namespace SoftUni_Platform_Certificate_Exchange.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SoftUni_Platform_Certificate_Exchange.Models;
using SoftUni_Platform_Certificate_Exchange.ViewModels;

public class AccountController : Controller
{
    // Услуги за потребители и автентикация
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    // Dependency Injection
    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Account/Login - показва Login формата
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    // POST: /Account/Login - проверява въведените данни
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        // Проверяваме валидността на формата
        if (!ModelState.IsValid)
            return View(model);

        // Търсим потребителя по имейл
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // Identity проверява паролата и създава login сесия
        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Пренасочваме само към безопасен локален адрес
            if (Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
            ModelState.AddModelError("", "Account is temporarily locked.");
        else
            ModelState.AddModelError("", "Invalid email or password.");

        return View(model);
    }

    // POST: /Account/Logout - прекратява сесията
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // Страница при липса на необходимите права
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return StatusCode(403, "Access denied.");
    }
}
