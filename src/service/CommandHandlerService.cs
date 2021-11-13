// see https://docs.stillu.cc/guides/commands/intro.html
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;
using FrankieBot.DB.Container;

namespace FrankieBot.Discord.Services
{
	/// <summary>
	/// Primary command handling service
	/// </summary>
	/// <remarks>
	/// This service is specifically responsible for commands that must be explicitly invoked by users.
	/// Commands that respond to the content of user messages, see EavesDropperService
	/// </remarks>
	public class CommandHandlerService
	{
		/// <summary>
		/// Option title for command prefix option
		/// </summary>
		public const string OptionCommandPrefix = "command_prefix";

		/// <summary>
		/// Available prefix options for Frankie commands
		/// </summary>
		/// <value></value>
		public static readonly string[] PrefixOptions = new[]
		{
			".",
			"!",
			"-",
			"?",
			"_"
		};

		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		private readonly IServiceProvider _services;
		private readonly DataBaseService _db;

		/// <summary>
		/// Creates a new CommandService instance
		/// </summary>
		/// <param name="services"></param>
		public CommandHandlerService(IServiceProvider services)
		{
			_commands = services.GetRequiredService<CommandService>();
			_client = services.GetRequiredService<DiscordSocketClient>();
			_db = services.GetRequiredService<DataBaseService>();
			_services = services;

			_commands.CommandExecuted += OnCommandExecuted;
			_client.MessageReceived += OnMessageReceived;
		}

		/// <summary>
		/// Automatically discover and register commands defined in this assembly
		/// </summary>
		/// <returns></returns>
		public async Task InitializeAsync()
		{
			await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _services);
		}

		/// <summary>
		/// Event Handler method called when receiving messages
		/// </summary>
		/// <param name="sourceMessage"></param>
		private async Task OnMessageReceived(SocketMessage sourceMessage)
		{
			if (!(sourceMessage is SocketUserMessage message))
			{
				return;
			}

			if (message.Source != MessageSource.User)
			{
				return;
			}

			// offset to track where prefix ends and actual command body begins
			int argPos = 0;

			if (!(
				message.HasMentionPrefix(_client.CurrentUser, ref argPos) ||
				message.HasStringPrefix(PrefixOptions, ref argPos)
				))
			{
				return;
			}

			var context = new SocketCommandContext(_client, message);

			if (!message.HasMentionPrefix(_client.CurrentUser, ref argPos))
			{
				bool validPrefix = false;
				await _db.RunDBAction(context, (ctx) => 
				{
					using (var connection = new DBConnection(context, _db.GetServerDBFilePath(context.Guild)))
					{
						var option = Option.FindOne(connection, o => o.Name == OptionCommandPrefix).As<Option>();
						if (option.IsEmpty)
						{
							option = new Option(connection)
							{
								Name = OptionCommandPrefix
							};
							option.Initialize();
							option.Save();
						}
						validPrefix = message.HasStringPrefix(option.Value, ref argPos);
					}
				});

				if (!validPrefix)
				{
					return;
				}
			}

			await _commands.ExecuteAsync(context, argPos, _services);
		}

		/// <summary>
		/// Event Handler method called once processing a command is concluded
		/// </summary>
		/// <param name="command"></param>
		/// <param name="context"></param>
		/// <param name="result"></param>
		private async Task OnCommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
		{
			if (!command.IsSpecified)
			{
				return;
			}

			if (result.IsSuccess)
			{
				return;
			}

			await context.Channel.SendMessageAsync($"I had trouble understanding your message. {result}");
		}
	}
}