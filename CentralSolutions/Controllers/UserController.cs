using System.Security.Claims;
using CentralSolutions.Data;
using CentralSolutions.Models;
using CentralSolutions.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentralSolutions.Controllers;

public class UserController(
		ApplicationDbContext context,
		IPasswordHasher<User> passwordHasher) : Controller
{
	[AllowAnonymous]
	[HttpGet]
	public IActionResult Login(string? returnUrl = null)
	{
		ViewBag.ReturnUrl = returnUrl;

		return View("Login");
	}


	[AllowAnonymous]
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(
			LoginViewModel model,
			string? returnUrl = null)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = await context.Users
			.FirstOrDefaultAsync(user =>
					user.Email == model.Email &&
					user.IsActive
					);

		if (user is null)
		{
			ModelState.AddModelError(
					string.Empty,
					"Usuário ou senha inválido" );

			return View(model);
		}


		var passwordResult = passwordHasher.VerifyHashedPassword(
				user,
				user.PasswordHash,
				model.Password
				);

		if (passwordResult == PasswordVerificationResult.Failed)
		{
			ModelState.AddModelError(
					string.Empty,
					"Usuário ou senha inválidos"
					);

			return View(model);
		}

		var claims = new List<Claim> {
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Name, user.Email)
		};

		var identity = new ClaimsIdentity(
					claims,
					CookieAuthenticationDefaults.AuthenticationScheme);

		var principal = new ClaimsPrincipal(identity);

		var properties = new AuthenticationProperties
		{
			IsPersistent = model.RememberMe
		};

		await HttpContext.SignInAsync(
			CookieAuthenticationDefaults.AuthenticationScheme,
			principal,
			properties);

		if (!string.IsNullOrWhiteSpace(returnUrl) &&
			Url.IsLocalUrl(returnUrl))
		{
			return Redirect(returnUrl);
		}

		return RedirectToAction("Index", "Home");
	}

	[Authorize]
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync(
			CookieAuthenticationDefaults.AuthenticationScheme);

		return RedirectToAction(nameof(Login));
	}
}
