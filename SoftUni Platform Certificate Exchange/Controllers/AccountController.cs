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

       
// При успешна автентикация
if (result.Succeeded)
{
    // При временна парола отваряме Change Password
    if (user.MustChangePassword)
        return RedirectToAction(nameof(ChangePassword));

    // Връщаме потребителя към поисканата локална страница
    if (Url.IsLocalUrl(returnUrl))
        return LocalRedirect(returnUrl);

    // Стандартно пренасочване към Home
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

    // GET: /Account/ChangePassword
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangePassword()
    {
        // Получаваме текущия потребител
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        // Ако паролата вече е сменена, отиваме към Home
        if (!user.MustChangePassword)
            return RedirectToAction("Index", "Home");

        return View(new ChangePasswordViewModel());
    }

    // POST: /Account/ChangePassword
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model)
    {
        // Получаваме текущия потребител
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        if (!user.MustChangePassword)
            return RedirectToAction("Index", "Home");

        // Новата парола трябва да бъде различна
        if (model.CurrentPassword == model.NewPassword)
        {
            ModelState.AddModelError(
                nameof(model.NewPassword),
                "New password must be different.");
        }

        // Проверяваме въведените данни
        if (!ModelState.IsValid)
            return View(model);

        // Identity проверява старата и записва хеш на новата
        var result = await _userManager.ChangePasswordAsync(
            user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // Отбелязваме, че задължителната смяна е изпълнена
        user.MustChangePassword = false;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            // Паролата вече е сменена, но флагът не е записан
            ModelState.AddModelError("",
                "Password changed, but account activation failed. " +
                "Contact the administrator.");

            return View(model);
        }

        // Обновяваме Login сесията след смяната
        await _signInManager.RefreshSignInAsync(user);

        // Клиентът вече може да използва Certificates
        return RedirectToAction("Index", "Certificates");
    }

}
