using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using YumeChan.Core.Config;

namespace YumeChan.Core.Services;

/// <summary>
/// Provides the Discord bot token.
/// </summary>
internal sealed class DiscordBotTokenProvider
{
	private readonly ICoreProperties _coreProperties;
	private readonly ILogger<DiscordBotTokenProvider> _logger;

	public DiscordBotTokenProvider(ICoreProperties coreProperties, ILogger<DiscordBotTokenProvider> logger)
	{
		_coreProperties = coreProperties;
		_logger = logger;
	}

	/// <summary>
	/// Gets the Discord bot token from either the <see cref="ICoreProperties"/>, or from the environment variables.
	/// </summary>
	/// <returns>The Discord bot token.</returns>
	/// <exception cref="ApplicationException">Thrown if no bot token was supplied.</exception>
	public string GetBotToken()
	{
		string? token = _coreProperties.BotToken;

		if (!string.IsNullOrWhiteSpace(token))
		{
			return token;
		}

		string envVarName = $"{_coreProperties.AppInternalName}.Token";

		if (TryBotTokenFromEnvironment(envVarName, out token, out EnvironmentVariableTarget target))
		{
			_logger.LogInformation("Bot Token was read from {target} Environment Variable \"{envVar}\", instead of \"coreproperties.json\" Config File", target, envVarName);
			return token;
		}

		ApplicationException e = new("No Bot Token supplied.");
		_logger.LogCritical(e, "No Bot Token was found in \"coreconfig.json\" Config File, and Environment Variables \"{envVar}\" from relevant targets are empty. Please set a Bot Token before launching the Bot", envVarName);

		throw e;
	}

	private static bool TryBotTokenFromEnvironment(string envVarName, [NotNullWhen(true)] out string? token, out EnvironmentVariableTarget foundFromTarget)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			foreach (EnvironmentVariableTarget target in typeof(EnvironmentVariableTarget).GetEnumValues())
			{
				token = Environment.GetEnvironmentVariable(envVarName, target);
				
				if (token is not null)
				{
					foundFromTarget = target;

					return true;
				}
			}

			token = null;
			foundFromTarget = default;

			return false;
		}

		token = Environment.GetEnvironmentVariable(envVarName);
		foundFromTarget = EnvironmentVariableTarget.Process;

		return token is not null;
	}
}