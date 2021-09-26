using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YumeChan.NetRunner.Pages.Auth;

public class LoginModel : PageModel
{
	public async Task OnGet(string redirectUri) => await HttpContext.ChallengeAsync(new AuthenticationProperties { RedirectUri = redirectUri ?? "/" });
}
