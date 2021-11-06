using System;
using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

using FrankieBot.Discord.Services;

using FrankieBot.DB;
using VModel = FrankieBot.DB.ViewModel;
using FrankieBot.DB.Container;

namespace FrankieBot.Discord.Modules
{
	/// <summary>
	/// Module responsible for handling quotes
	/// </summary>
	[Group("quote")]
	[Alias("q")]
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
			// Convenience alias
			var db = DataBaseService;

			await db.RunDBAction(Context, async context => 
			{
				VModel.Quote quote = null;
				using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
				{
					var quotes = VModel.Quote.FindAll(connection).ContentAs<VModel.Quote>();
					
					var random = new Random();

					quote = quotes.Content[random.Next(0, quotes.Content.Count)];
				}
				await db.PostQuote(Context, quote);
			});
		}

		/// <summary>
		/// Lists a random quote from the specified user
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		[Command]
		public async Task Quote(IUser user)
		{
			// Convenience alias
			var db = DataBaseService;

			await db.RunDBAction(Context, async context =>
			{
				VModel.Quote quote = null;
				using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
				{
					var authorID = user.Id.ToString();
					var quotes = VModel.Quote.Find(connection, q => q.AuthorID == authorID).ContentAs<VModel.Quote>();

					var random = new Random();

					quote = quotes.Content[random.Next(0, quotes.Content.Count)];
				}
			await db.PostQuote(Context, quote);
			});
		}

		/// <summary>
		/// Posts the quote with the matching ID
		/// </summary>
		/// <param name="quoteID"></param>
		/// <returns></returns>
		[Command]
		public async Task Quote(int quoteID)
		{
			await DataBaseService.PostQuote(Context, quoteID);
		}

		/// <summary>
		/// Adds a new quote to the server's saved quotes
		/// </summary>
		/// <param name="user">User to whom the quote belongs</param>
		/// <param name="msg">The quote to be saved</param>
		/// <returns></returns>
		[Command("add")]
		[Alias("a")]
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
		[Alias("a")]
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
		[Alias("l", "ls")]
		public async Task ListQuotes(IUser user)
		{
			await DataBaseService.ListQuotes(Context, user);
		}

		/// <summary>
		/// Removes the quote with the given ID from the database
		/// </summary>
		/// <param name="quoteID"></param>
		/// <returns></returns>
		[Command("remove")]
		[Alias("delete", "r", "d")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task RemoveQuote(int quoteID)
		{
			// Convenience alias
			var db = DataBaseService;

			await db.RunDBAction(Context, async context =>
			{
				using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
				{
					var quote = VModel.Quote.Find(connection, quoteID);
					if (quote.IsEmpty)
					{
						await context.Channel.SendMessageAsync($"Unable to find Quote with id [{quoteID}]");
					}
					else
					{
						quote.Delete();
						await context.Channel.SendMessageAsync($"Quote with ID [{quoteID}] deleted");
					}
				}
			});
		}
	}
}