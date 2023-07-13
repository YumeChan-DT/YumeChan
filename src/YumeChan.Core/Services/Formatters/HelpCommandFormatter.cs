using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using JetBrains.Annotations;

#nullable enable
namespace YumeChan.Core.Services.Formatters;

[UsedImplicitly]
public sealed class HelpCommandFormatter : DefaultHelpFormatter
{
    public HelpCommandFormatter(CommandContext ctx) : base(ctx) { }

    public override CommandHelpMessage Build()
    {
        EmbedBuilder.Footer = Utilities.DefaultCoreFooter;
        return base.Build();
    }
}