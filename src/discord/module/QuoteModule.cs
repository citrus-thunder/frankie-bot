using System;
using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

namespace FrankieBot.Discord.Modules
{
	/// <summary>
	/// Module responsible for handling quotes
	/// </summary>
	[Group("quote")]
	public class QuoteModule : ModuleBase<SocketCommandContext>
	{

		/// <summary>
		/// Returns a random quote from the server's saved quotes
		/// </summary>
		/// <returns></returns>
		[Command]
		public async Task Quote()
		{
			// todo: return random quote
			await Task.CompletedTask;
		}

		/// <summary>
		/// Adds a new quote to the server's saved quotes
		/// </summary>
		/// <param name="user">User to whom the quote belongs</param>
		/// <param name="msg">The quote to be saved</param>
		/// <returns></returns>
		[Command("add")]
		public async Task AddQuote(IUser user, [Remainder] string msg)
		{
			if (Context.Message.ReferencedMessage != null)
			{
				await AddQuoteText(Context.Message.ReferencedMessage.Author, Context.Message.ReferencedMessage.Content);
			}
			else
			{
				await AddQuoteText(user, msg);
			}
		}

		/// <summary>
		/// Adds a new quote to the server's saved quotes
		/// </summary>
		/// <returns></returns>
		[Command("add")]
		public async Task AddQuote()
		{
			if (Context.Message.ReferencedMessage != null)
			{
				await AddQuoteText(Context.Message.ReferencedMessage.Author, Context.Message.ReferencedMessage.Content);
			}
		}

		private async Task AddQuoteText(IUser user, string quote)
		{
			await Context.Channel.SendMessageAsync($"> {quote}\n*--{user.Username}, <t:{DateTimeOffset.Now.ToUnixTimeSeconds()}:F>*");
		}
	}
}