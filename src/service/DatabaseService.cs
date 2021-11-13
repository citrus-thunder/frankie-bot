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
using FrankieBot.DB.Container;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.Discord.Services
{
	/// <summary>
	/// Service responsible for handling database transactions
	/// </summary>
	public class DataBaseService
	{
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
						connection.CreateTable<Model.CronJob>();
						connection.CreateTable<Model.ProgressReportWindow>();
						connection.CreateTable<Model.ProgressReport>();
					}
				}
			});
		}

		/// <summary>
		/// Returns the file path for the given guild's server database
		/// </summary>
		/// <param name="guildId"></param>
		/// <returns></returns>
		public string GetServerDBFilePath(ulong guildId)
		{
			return Path.Combine(DBConfig.SERVER_DATA_ROOT, guildId.ToString() + DBConfig.DATABASE_FILE_EXTENSION);
		}

		/// <summary>
		/// Returns the file path for the given guild's server database
		/// </summary>
		/// <param name="guild"></param>
		/// <returns></returns>
		public string GetServerDBFilePath(SocketGuild guild) => GetServerDBFilePath(guild.Id);
	}
}