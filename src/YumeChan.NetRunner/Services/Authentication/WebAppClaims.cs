using DSharpPlus;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace YumeChan.NetRunner.Services.Authentication;

public sealed class WebAppClaims : IClaimsTransformation
{
	private readonly DiscordClient _client;

	public WebAppClaims(DiscordClient client)
	{
		_client = client;
	}

	public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
	{
		if (principal.Identity is { IsAuthenticated: true })
		{
			ClaimsIdentity identity = new();
			ulong snowflake = Convert.ToUInt64(principal.FindFirstValue(ClaimTypes.NameIdentifier));

			if (_client.CurrentApplication?.Owners.Select(static u => u.Id).Contains(snowflake) ?? false)
			{
				identity.AddClaim(new(ClaimTypes.Role, UserRoles.Admin));
			}

			principal.AddIdentity(identity);
		}

		return Task.FromResult(principal);
	}
}
