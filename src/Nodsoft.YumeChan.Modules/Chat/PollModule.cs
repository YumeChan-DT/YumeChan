using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Modules.Chat
{
	[Group("poll")]
	public class PollModule : ModuleBase<SocketCommandContext>
	{
		public static List<Poll> DraftPolls { get; internal set; } = new List<Poll>();
		// public static List<Poll> CurrentPolls { get; internal set; } = new List<Poll>();

		public Poll SelectedPoll { get => selectedPoll ?? GetUserPollAsync().Result; protected set => selectedPoll = value; }
		private Poll selectedPoll;


		[Command("init")]
		public async Task InitPollAsync()
		{
			Poll poll = QueryUserPoll(Context.User, DraftPolls);

			if (poll is null)
			{
				IUserMessage reply = await ReplyAsync("Creating a new Poll...");
				DraftPolls.Add(new Poll(Context.User));
				await reply.ModifyAsync(msg => msg.Content += " Done !");
			}
			else
			{
				IUserMessage reply = await ReplyAsync("Resetting existing Poll...");
				DraftPolls.Find(x => x == poll).Reset();
				await reply.ModifyAsync(msg => msg.Content += " Done !");
			}
		}

		[Command("name")]
		public async Task SetPollNameAsync([Remainder]string name)
		{
			if (SelectedPoll is null) return;

			SelectedPoll.Name = name;
			await Utils.MarkCommandAsCompleted(Context);
		}
		[Command("notice")]
		public async Task SetPollNoticeAsync([Remainder]string notice)
		{
			if (SelectedPoll is null) return;

			SelectedPoll.Notice = notice;
			await Utils.MarkCommandAsCompleted(Context);
		}

		[Command("addoption")]
		public async Task AddPollOptionAsync(IEmote reactionEmote, [Remainder]string description)
		{
			if (SelectedPoll is null) return;

			SelectedPoll.VoteOptions.Add(new PollVoteOption { ReactionEmote = reactionEmote, Description = description });
			await Utils.MarkCommandAsCompleted(Context);
		}
		//[Command("setoption"), Priority(1)]
		public async Task SetPollOptionAsync(byte index, IEmote reactionEmote, [Remainder]string description)
		{
			if (SelectedPoll is null) return;
			if (await Poll.VoteOptionsIndexIsOutsideRange(index, Context)) return;

			SelectedPoll.VoteOptions[index] = new PollVoteOption { ReactionEmote = reactionEmote, Description = description };
			await Utils.MarkCommandAsCompleted(Context);
		}
		//[Command("setoption")]
		public async Task SetPollOptionAsync(IEmote reactionEmote, [Remainder]string description)
		{
			if (SelectedPoll is null) return;

			SelectedPoll.VoteOptions.Find(x => x.ReactionEmote == reactionEmote).Description = description; 

			await Utils.MarkCommandAsCompleted(Context);
		}
		[Command("removeoption"), Priority(1)]
		public async Task RemovePollOptionAsync(byte index)
		{
			if (SelectedPoll is null) return;
			if (await Poll.VoteOptionsIndexIsOutsideRange(index, Context)) return;

			SelectedPoll.VoteOptions.RemoveAt(index);
			await Utils.MarkCommandAsCompleted(Context);
		}
		[Command("removeoption")]
		public async Task RemovePollOptionAsync(IEmote emote)
		{
			if (SelectedPoll is null) return;

			try
			{
				SelectedPoll.VoteOptions.Remove(SelectedPoll.VoteOptions.First(option => option.ReactionEmote == emote));
				await Utils.MarkCommandAsCompleted(Context);
			}
			catch (System.InvalidOperationException)
			{
				await Utils.MarkCommandAsFailed(Context);
			}
		}
		[Command("clearoptions")]
		public async Task ClearPollOptionsAsync()
		{
			if (SelectedPoll is null) return;

			SelectedPoll.VoteOptions.Clear();

			await Utils.MarkCommandAsCompleted(Context);
		}

		[Command("previewoptions")]
		public async Task PreviewOptionsAsync()
		{
			if (SelectedPoll is null) return;

			if (SelectedPoll.VoteOptions.Count == 0)
			{
				await ReplyAsync($"{Context.User.Mention} **No Vote Options registered.** Be sure to add some before attempting to Preview !");
			}
			else
			{
				StringBuilder previewBuilder = new StringBuilder(Context.User.Mention).Append($" Previewing Vote Options on **{SelectedPoll.Name ?? "Unnamed Poll"}** :\n\n");
				byte index = 0;

				foreach (PollVoteOption option in SelectedPoll.VoteOptions)
				{
					index++;
					previewBuilder.AppendLine($"**{index} :** {option.ReactionEmote} - {option.Description}");
				}

				await ReplyAsync(previewBuilder.ToString());
			}
		}

		[Command("previewpoll")]
		public async Task PreviewPollAsync()
		{
			if (SelectedPoll is null) return;

			IUserMessage message = await Context.User.GetOrCreateDMChannelAsync().Result.SendMessageAsync(embed: BuildPollMessage().Result.Build());
			await AddPollReactionsAsync(SelectedPoll, message);
		}
		[Command("publish")]
		public async Task PublishPollAsync()
		{
			if (SelectedPoll is null) return;

			SelectedPoll.PublishedPollMessage = await ReplyAsync(embed: BuildPollMessage().Result.Build());
			await AddPollReactionsAsync(SelectedPoll, SelectedPoll.PublishedPollMessage);

			await Context.User.GetOrCreateDMChannelAsync().Result.SendMessageAsync(
				$"Published Poll ``{SelectedPoll.Name}`` in channel ``{SelectedPoll.PublishedPollMessage.Channel.Name}``.");

			await Context.Message.DeleteAsync();

			// CurrentPolls.Add(SelectedPoll);
			DraftPolls.Remove(SelectedPoll);
		}

		public static Poll QueryUserPoll(IUser user, List<Poll> list) => list.FirstOrDefault(poll => poll.Author.Id == user.Id);

		protected Task<EmbedBuilder> BuildPollMessage()
		{
			EmbedBuilder embed = new EmbedBuilder { Title = "**Poll**", Description = SelectedPoll.Name ?? "No Description." }
				.WithAuthor(SelectedPoll.Author);

			if (!string.IsNullOrWhiteSpace(SelectedPoll.Notice))
			{
				embed.WithFooter(SelectedPoll.Notice);
			}

			foreach (PollVoteOption option in SelectedPoll.VoteOptions)
			{
				embed.AddField(option.Description, option.ReactionEmote, true);
			}

			return Task.FromResult(embed);
		}

		protected static async Task AddPollReactionsAsync(Poll poll, IUserMessage message)
		{
			await message.AddReactionsAsync(poll.VoteOptions.Select(x => x.ReactionEmote).ToArray());
		}

		private async Task<Poll> GetUserPollAsync()
		{
			Poll poll = QueryUserPoll(Context.User, DraftPolls);

			if (poll is null)
			{
				await ReplyAsync("Cannot perform action, no Poll was found.\nYou may initialize a Poll by typing ``==poll init``.");
			}
			return poll;
		}
	}

	public class Poll
	{
		public IUser Author { get; private set; }

		public string Name { get; set; }
		public string Notice { get; set; }

		public List<PollVoteOption> VoteOptions { get; set; } = new List<PollVoteOption>(20);

		public IUserMessage PublishedPollMessage { get; internal set; }

		public Poll(IUser author) => Author = author;

		public void Reset()
		{
			ResetObjects(Name, Notice, VoteOptions, PublishedPollMessage);

			static void ResetObjects(params object[] objects)
			{
				for (int i = 0; i < objects.Length; i++)
				{
					objects[i] = null;
				}
			}
		}

		public static async Task<bool> VoteOptionsIndexIsOutsideRange(byte index, SocketCommandContext context)
		{
			if (index > 20)
			{
				await context.Channel.SendMessageAsync($"{context.User.Mention} You have entered an index greater than 20. Please note that Discord only authorizes up to 20 reaction types per message.");
				return true;
			}
			return false;
		}
	}


#pragma warning disable CA1815 // Override equals and operator equals on value types

	public class PollVoteOption
	{
		public IEmote ReactionEmote { get; set; }

		public string Description { get; set; }

		// public List<IGuildUser> Voters { get; internal set; }
	}
}
