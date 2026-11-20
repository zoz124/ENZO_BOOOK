using BookNest.Core.Interfaces.IServices;
using BookNest.Infrastructure.Data;
using BookNest.Infrastructure.Identity;
using BookNest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICartService _cart;
    private readonly IWebHostEnvironment _environment;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICartService cart,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _cart = cart;
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // One generic message for unknown email and wrong password: no user enumeration.
        const string genericError = "That email or password is not correct.";

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, genericError);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is temporarily locked after 5 failed attempts. Try again in 15 minutes.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, genericError);
            return View(model);
        }

        // Guest cart follows the customer into their account.
        await _cart.MergeGuestCartAsync(user.Id);

        var isAdmin = await _userManager.IsInRoleAsync(user, DatabaseSeeder.AdminRole);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return Redirect(isAdmin ? "/admin" : "/");
    }

    [HttpGet]
    public IActionResult Register(string? email = null)
        => View(new RegisterViewModel { Email = email?.Trim() ?? string.Empty });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var parts = model.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.Phone,
            FirstName = parts.Length > 0 ? parts[0] : null,
            LastName = parts.Length > 1 ? parts[1] : null,
            IsActive = true,
            EmailConfirmed = true
        };

        IdentityResult created;

        try
        {
            created = await _userManager.CreateAsync(user, model.Password);
        }
        catch (DbUpdateException)
        {
            // The unique email index won the race against a simultaneous registration.
            ModelState.AddModelError(string.Empty, "An account with this email already exists.");
            return View(model);
        }

        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, DatabaseSeeder.CustomerRole);
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _cart.MergeGuestCartAsync(user.Id);

        TempData["Success"] = "Welcome to Enzo Book!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action(nameof(ResetPassword), "Account", new { email = model.Email, token }, Request.Scheme);

            // No SMTP yet. Never render the reset link outside local development:
            // doing so would let anyone take over an account by typing its email.
            if (_environment.IsDevelopment())
            {
                TempData["ResetLink"] = link;
            }
        }

        TempData["Success"] = "If that email is registered, a reset link has been sent.";
        return View(nameof(ForgotPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
        => View(new ResetPasswordViewModel { Email = email, Token = token });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "This reset link is not valid.");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["Success"] = "Your password was changed. Sign in with the new one.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
