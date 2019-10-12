using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Modules.Chat
{
	[Group("poll")]
	public class PollModule : ModuleBase<SocketCommandContext>
	{
		public static List<Poll> DraftPolls { get; internal set; } = new List<Poll>();
		public static List<Poll> CurrentPolls { get; internal set; } = new List<Poll>();

		public Poll SelectedPoll { get => selectedPoll ?? QueryUserPoll(Context.User, DraftPolls); protected set => selectedPoll = value; }
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
		public async Task SetPollNameAsync([Remainder]string name)
		{
			if (SelectedPoll is null) return;

			SelectedPoll.Name = name;
			await Utils.MarkCommandAsCompleted(Context);
		}

		public static Poll QueryUserPoll(IUser user, List<Poll> list) => list.FirstOrDefault(poll => poll.Author.Id == user.Id);

		private async Task<bool> CheckUserPendingPollExistsAsync()
		{
			Poll poll = QueryUserPoll(Context.User, DraftPolls);

			if (poll.Equals(null))
			{
				await ReplyAsync("Cannot perform action, no poll found.\nYou mat initialize a poll by typing ``==poll init``.");
				return false;
			}
			else
			{
				SelectedPoll = poll;
				return true;
			}
		}
	}

	public class Poll
	{
		public IUser Author { get; private set; }

		public string Name { get; set; }
		public string Notice { get; set; }

		public PollVoteOption[] VoteOptions { get; set; }

		public IMessage PublishedPollMessage { get; internal set; }


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
	}

	public struct PollVoteOption
	{
		public IEmote Emote { get; }

		public string Description { get; set; }

		public List<IGuildUser> Voters { get; internal set; }
	}
}
