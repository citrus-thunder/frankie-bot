using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace FrankieBot.Discord.Modules
{
	/// <summary>
	/// Command Module containing FrankieBot's basic chat commands
	/// </summary>
	public class ChatModule : ModuleBase<SocketCommandContext>
	{
		[Command("ping")]
		public Task PingAsync()
			=> ReplyAsync("pong!");
	}
}