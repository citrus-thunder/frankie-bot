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
		/// <summary>
		/// Responds to user message containing the key word. Used for testing.
		/// </summary>
		/// <returns></returns>
		[Command("ping")]
		public Task PingAsync()
			=> ReplyAsync("pong!");
	}
}