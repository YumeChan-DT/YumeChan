using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

namespace YumeChan.NetRunner.Pages.Auth
{
	public class LogoutModel : PageModel
	{
		public async Task<IActionResult> OnGet()
		{
			await HttpContext.SignOutAsync();
			return Redirect("/");
		}
	}
}
