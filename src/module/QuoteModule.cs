using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

using SQLite;

using FrankieBot.Discord.Services;
using FrankieBot.DB;

using ViewModel = FrankieBot.DB.ViewModel;


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

		#region Commands

		/// <summary>
		/// Returns a random quote from the server's saved quotes
		/// </summary>
		/// <returns></returns>
		[Command]
		public async Task Quote()
		{
			// Convenience alias
			var db = DataBaseService;
			ViewModel.Quote quote = null;
			await db.RunGuildDBAction(Context.Guild, async connection =>
			{
				var quotes = ViewModel.Quote.FindAll(connection).ContentAs<ViewModel.Quote>();
				var random = new Random();
				quote = quotes.Content[random.Next(0, quotes.Content.Count)];
				quote.Initialize(Context.Guild);
			});
			await PostQuote(Context, quote);
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

			await db.RunGuildDBAction(Context.Guild, async connection =>
			{
				ViewModel.Quote quote = null;

				var authorID = user.Id.ToString();
				var quotes = ViewModel.Quote.Find(connection, q => q.AuthorID == authorID).ContentAs<ViewModel.Quote>();

				var random = new Random();

				quote = quotes.Content[random.Next(0, quotes.Content.Count)];
				quote.Initialize(Context.Guild);

				await PostQuote(Context, quote);
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
			await PostQuote(Context, quoteID);
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
				await AddQuote(
					Context,
					Context.Message.ReferencedMessage.Author,
					Context.Message.ReferencedMessage.Content,
					Context.Message.Author);
			}
			else
			{
				await AddQuote(
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
				await AddQuote(
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
			await ListQuotes(Context, user);
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

			await db.RunGuildDBAction(Context.Guild, async connection =>
			{
				var quote = ViewModel.Quote.Find(connection, quoteID);
				if (quote.IsEmpty)
				{
					await Context.Channel.SendMessageAsync($"Unable to find Quote with id [{quoteID}]");
				}
				else
				{
					quote.Delete();
					await Context.Channel.SendMessageAsync($"Quote with ID [{quoteID}] deleted");
				}
			});
		}

		#endregion // Commands

		private async Task<ViewModel.Quote> GetQuote(SocketCommandContext context, int id)
		{
			ViewModel.Quote res = null;

			// Convenience Alias
			var db = DataBaseService;

			await db.RunGuildDBAction(context.Guild, connection =>
			{
				ViewModel.Quote quote = null;

				var quoteID = id.ToString();
				quote = ViewModel.Quote.Find(connection, id).As<ViewModel.Quote>();
				quote.Initialize(context.Guild);

				if (quote.IsEmpty)
				{
					throw new RecordNotFoundException($"Quote with ID [{id}] not found!");
				}
				res = quote;
			});
			return res;
		}

		private async Task PostQuote(SocketCommandContext context, ViewModel.Quote quote)
		{
			var eb = new EmbedBuilder()
				.WithAuthor(quote.Author)
				.WithDescription(quote.Content)
				.WithColor(0x00c88c)
				.WithTimestamp(new DateTimeOffset(quote.QuoteTimeStamp.ToLocalTime()));

			await context.Channel.SendMessageAsync(embed: eb.Build());
		}

		private async Task PostQuote(SocketCommandContext context, int quoteID)
		{
			ViewModel.Quote quote = null;
			try
			{
				quote = await GetQuote(context, quoteID);
				await PostQuote(context, quote);
			}
			catch (DBException ex)
			{
				await context.Channel.SendMessageAsync(ex.Message);
			}
		}

		private async Task PostQuoteList(SocketCommandContext context, List<ViewModel.Quote> quotes)
		{
			var fields = new List<EmbedFieldBuilder>();
			foreach (var quote in quotes)
			{
				var newField = new EmbedFieldBuilder()
					.WithName($"[#{quote.ID}]: {new DateTimeOffset(quote.QuoteTimeStamp.ToLocalTime()).ToString("d/M/yyyy hh:mm tt K")}")
					.WithValue($"{quote.Content}");
				fields.Add(newField);
			}

			var eb = new EmbedBuilder()
				.WithAuthor(quotes[0].Author)
				.WithColor(0x00c88c)
				.WithFields(fields);

			await context.Channel.SendMessageAsync(text: $"Quotes by {quotes[0].Author.Username}", embed: eb.Build());
		}

		private async Task ListQuotes(SocketCommandContext context, IUser user)
		{
			//Convenience Alias
			var db = DataBaseService;

			List<ViewModel.Quote> quoteList = null;

			await db.RunGuildDBAction(context.Guild, connection =>
			{
				var userID = user.Id.ToString();
				var quotes = ViewModel.Quote.Find(connection, (quote) => quote.AuthorID == userID).ContentAs<ViewModel.Quote>();
				quotes.Content.ForEach(q => q.Initialize(context.Guild));
				quoteList = quotes.Content;
			});

			if (quoteList.Count < 1)
			{
				await context.Channel.SendMessageAsync($"No quotes found yet for {user.Username}");
			}
			else
			{
				await PostQuoteList(context, quoteList);
			}
		}

		private async Task AddQuote(SocketCommandContext context, IUser user, string message, IUser recorder)
		{
			// Convenience Alias
			var db = DataBaseService;
			try
			{
				ViewModel.Quote quote = null;
				await db.RunGuildDBAction(context.Guild, connection =>
				{
					quote = new ViewModel.Quote(connection)
					{
						Author = user,
						Content = message,
						Recorder = recorder,
						RecordTimeStamp = DateTime.UtcNow,
						QuoteTimeStamp = DateTime.UtcNow
					};

					if (context.Message.ReferencedMessage != null)
					{
						quote.QuoteTimeStamp = context.Message.ReferencedMessage.Timestamp.UtcDateTime;
					}
					quote.Save();
				});

				await context.Channel.SendMessageAsync("New Quote added!");
				await PostQuote(context, quote);
			}
			catch (SQLiteException ex)
			{
				await context.Channel.SendMessageAsync($"Something went wrong when recording that last quote. {ex}");
			}
		}
	}
}