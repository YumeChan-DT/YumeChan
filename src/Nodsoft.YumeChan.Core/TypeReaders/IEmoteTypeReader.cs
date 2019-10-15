using Discord;
using Discord.Commands;

using System;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core.TypeReaders
{
	class IEmoteTypeReader : TypeReader
	{
		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
		{
			if (Emote.TryParse(input, out Emote emote))
			{
				return Task.FromResult(TypeReaderResult.FromSuccess(emote));
			}
			else if (NeoSmart.Unicode.Emoji.IsEmoji(input))
			{
				return Task.FromResult(TypeReaderResult.FromSuccess(new Emoji(input)));
			}
			else
			{
				return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Input {input} could neither be parsed as an Unicode Emoji or as a Discord Emote."));
			}
		}
	}
}
