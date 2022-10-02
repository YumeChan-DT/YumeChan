using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;

namespace YumeChan.Core.Services.Formatters;

public class HelpCommandFormatter : DefaultHelpFormatter
{
    public HelpCommandFormatter(CommandContext ctx) : base(ctx) { }

    public override CommandHelpMessage Build()
    {
        EmbedBuilder.Footer = Utilities.DefaultCoreFooter;
        return base.Build();
    }
}