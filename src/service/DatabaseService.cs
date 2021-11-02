using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using SQLite;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.Discord.Services
{
	/// <summary>
	/// Service responsible for handling database transactions
	/// </summary>
	public class DataBaseService
	{
		/// <summary>
		/// Fired when a new Quote is added to the database
		/// </summary>
		public event EventHandler<QuoteEventArgs> QuoteAdded;

		private readonly IServiceProvider _services;

		private readonly DiscordSocketClient _client;

		/// <summary>
		/// Constructs a new DatabaseService
		/// </summary>
		/// <param name="services"></param>
		public DataBaseService(IServiceProvider services)
		{
			_services = services;
			_client = _services.GetRequiredService<DiscordSocketClient>();
			QuoteAdded += OnQuoteAdded;
		}

		/// <summary>
		/// Runs a generic database action defined in the action parameter
		/// </summary>
		/// <param name="context"></param>
		/// <param name="action"></param>
		/// <remarks>
		/// Performs baseline sanity checking and validation and is called by
		/// most other DB operation methods under the hood
		/// </remarks>
		public async Task RunDBAction(SocketCommandContext context, Action<SocketCommandContext> action)
		{
			// We don't want DM commands to clutter up the database, so we'll only allow commands sent from
			// non-private channels to affect the database.
			if (context.IsPrivate)
			{
				await context.Channel.SendMessageAsync("Sorry, but this action isn't permitted in private channels or DMs");
				return;
			}

			await CheckDB(context);

			await Task.Run(() => action(context));
		}

		private async Task CheckDB(SocketCommandContext context)
		{
			await Task.Run(() =>
			{
				var dbFile = GetServerDBFilePath(context.Guild);
				if (!File.Exists(dbFile))
				{
					File.Create(dbFile).Close();
					using (var connection = new DBConnection(context, dbFile))
					{
						connection.CreateTable<Model.Option>();
						connection.CreateTable<Model.Quote>();
						connection.CreateTable<Model.Server>();
					}
				}
			});
		}

		/// <summary>
		/// Adds a new Quote to the database
		/// </summary>
		/// <param name="context"></param>
		/// <param name="user"></param>
		/// <param name="message"></param>
		/// <param name="recorder"></param>
		/// <returns></returns>
		public async Task AddQuote(SocketCommandContext context, IUser user, string message, IUser recorder)
		{
			await RunDBAction(context, (c) =>
			{
				using (var db = new DBConnection(c, GetServerDBFilePath(c.Guild)))
				{
					var quote = new Quote(db)
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

					try
					{
						quote.Save();
						QuoteAdded?.Invoke(this, new QuoteEventArgs()
						{
							Quote = quote,
							Context = context
						});
					}
					catch (SQLiteException ex)
					{
						context.Channel.SendMessageAsync($"Something went wrong when recording that last quote. {ex}");
					}
				}
			});
		}

		/// <summary>
		/// Lists quotes for the specified user
		/// </summary>
		/// <param name="context"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public async Task ListQuotes(SocketCommandContext context, IUser user)
		{
			await RunDBAction(context, async (c) => 
			{
				List<Quote> quoteList;
				using (var connection = new DBConnection(context, GetServerDBFilePath(c.Guild)))
				{
					var userID = user.Id.ToString();
					var quotes = Quote.Find(connection, (quote) => quote.AuthorID == userID).As<Quote>();
					quoteList = quotes.Content;
				}

				if (quoteList.Count < 1)
				{
					await context.Channel.SendMessageAsync($"No quotes found yet for {user.Username}");
				}
				else
				{
					await PostQuoteList(context, quoteList);
				}
			});
		}

		/// <summary>
		/// Posts a Quote embed as a reply
		/// </summary>
		/// <param name="context"></param>
		/// <param name="quote"></param>
		/// <returns></returns>
		public async Task PostQuote(SocketCommandContext context, Quote quote)
		{
			var eb = new EmbedBuilder()
				.WithAuthor(quote.Author)
				.WithDescription(quote.Content)
				.WithTimestamp(new DateTimeOffset(quote.QuoteTimeStamp.ToLocalTime()));

			await context.Channel.SendMessageAsync(embed: eb.Build());
		}

		/// <summary>
		/// Posts the quote with the specified ID
		/// </summary>
		/// <param name="context"></param>
		/// <param name="quoteID"></param>
		/// <returns></returns>
		public async Task PostQuote(SocketCommandContext context, int quoteID)
		{
			var quote = await GetQuote(context, quoteID);
			await PostQuote(context, quote);
		}

		/// <summary>
		/// Posts a list of quotes to the context's channel
		/// </summary>
		/// <param name="context"></param>
		/// <param name="quotes"></param>
		/// <returns></returns>
		public async Task PostQuoteList(SocketCommandContext context, List<Quote> quotes)
		{
			var eb = new EmbedBuilder()
				.WithTitle($"Quotes by {quotes[0].Author.Username}")
				.WithColor(0x00c88c);
			
			var fields = new List<EmbedFieldBuilder>();
			foreach (var quote in quotes)
			{
				var newField = new EmbedFieldBuilder()
					.WithName($"[#{quote.ID}]: {new DateTimeOffset(quote.QuoteTimeStamp.ToLocalTime()).ToString("d/M/yyyy hh:mm tt K")}")
					.WithValue($"{quote.Content}");
				fields.Add(newField);
			}
			eb.WithFields(fields);

			await context.Channel.SendMessageAsync(embed: eb.Build());
		}

		/// <summary>
		/// Gets a quote from the DB by ID
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Quote> GetQuote(SocketCommandContext context, int id)
		{
			return await Task.Run(() =>
			{
				Quote quote = null;
				using (var connection = new DBConnection(context, GetServerDBFilePath(context.Guild)))
				{
					var quoteID = id.ToString();
					quote = Quote.Find(connection, id).As<Quote>();
				}
				return quote;
			});
		}

		private string GetServerDBFilePath(ulong guildId)
		{
			return Path.Combine(DBConfig.SERVER_DATA_ROOT, guildId.ToString() + DBConfig.DATABASE_FILE_EXTENSION);
		}

		private string GetServerDBFilePath(SocketGuild guild) => GetServerDBFilePath(guild.Id);

		private async void OnQuoteAdded(object sender, QuoteEventArgs q)
		{
			await q.Context.Channel.SendMessageAsync("New quote added!");
			await PostQuote(q.Context, q.Quote);
		}
	}
}