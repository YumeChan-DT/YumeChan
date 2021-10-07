using DSharpPlus.Entities;
using DSharpPlus;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace YumeChan.NetRunner.Services.Authentication;

public class WebAppClaims : IClaimsTransformation
{
	private readonly DiscordClient _client;

	public WebAppClaims(DiscordClient client, IConfiguration config)
	{
		_client = client;
	}

	public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
	{
		if (principal.Identity.IsAuthenticated)
		{
			ClaimsIdentity identity = new();
			ulong snowflake = Convert.ToUInt64(principal.FindFirstValue(ClaimTypes.NameIdentifier));

			if (_client.CurrentApplication.Owners.Select(u => u.Id).Contains(snowflake))
			{
				identity.AddClaim(new(ClaimTypes.Role, UserRoles.Admin));
			}

			principal.AddIdentity(identity);
		}

		return Task.FromResult(principal);
	}
}
