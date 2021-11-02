using System;
using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

using FrankieBot.Discord.Services;

namespace FrankieBot.Discord.Modules
{
	/// <summary>
	/// Module responsible for handling quotes
	/// </summary>
	[Group("quote")]
	public class QuoteModule : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// DatabaseService reference
		/// </summary>
		/// <remarks>
		/// Automatically assigned via DependencyInjection
		/// </remarks>
		public DataBaseService DataBaseService { get; set; }

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
				await DataBaseService.AddQuote(
					Context,
					Context.Message.ReferencedMessage.Author,
					Context.Message.ReferencedMessage.Content,
					Context.Message.Author);
			}
			else
			{
				//await AddQuoteText(user, msg, Context.Message.Author);
				await DataBaseService.AddQuote(
					Context,
					user, 
					msg, 
					Context.Message.Author);
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
				await DataBaseService.AddQuote(
					Context,
					Context.Message.ReferencedMessage.Author,
					Context.Message.ReferencedMessage.Content,
					Context.Message.Author);
			}
		}

		/// <summary>
		/// Lists quotes for the specified user
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		[Command("list")]
		public async Task ListQuotes(IUser user)
		{
			await DataBaseService.ListQuotes(Context, user);
		}

		/// <summary>
		/// Lists the quote with the specified ID
		/// </summary>
		/// <param name="quoteID"></param>
		/// <returns></returns>
		[Command("list")]
		public async Task ListQuotes(int quoteID)
		{
			await DataBaseService.PostQuote(Context, quoteID);
		}
	}
}