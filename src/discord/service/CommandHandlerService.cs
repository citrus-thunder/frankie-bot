// see https://docs.stillu.cc/guides/commands/intro.html
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace FrankieBot.Discord.Services
{
	/// <summary>
	/// Primary command handling service
	/// </summary>
	public class CommandHandlerService
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		private readonly IServiceProvider _services;

		/// <summary>
		/// Creates a new CommandService instance
		/// </summary>
		/// <param name="services"></param>
		public CommandHandlerService(IServiceProvider services)
		{
			_commands = services.GetRequiredService<CommandService>();
			_client = services.GetRequiredService<DiscordSocketClient>();
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
			await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
		}

		/// <summary>
		/// Event Handler method called when receiving messages
		/// </summary>
		/// <param name="sourceMessage"></param>
		public async Task OnMessageReceived(SocketMessage sourceMessage)
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

			if (!message.HasCharPrefix('.', ref argPos))
			{
				return;
			}

			var context = new SocketCommandContext(_client, message);

			await _commands.ExecuteAsync(context, argPos, _services);
		}

		/// <summary>
		/// Event Handler method called once processing a command is concluded
		/// </summary>
		/// <param name="command"></param>
		/// <param name="context"></param>
		/// <param name="result"></param>
		public async Task OnCommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
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