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
	/// Service responsible for reacting to user message content
	/// </summary>
	public class EavesDropperService
	{
		private readonly DiscordSocketClient _client;
		private readonly IServiceProvider _services;
		
		/// <summary>
		/// Instantiates a new EavesdropperService
		/// </summary>
		/// <param name="services"></param>
		public EavesDropperService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();
			_services = services;

			_client.MessageReceived += OnMessageReceived;
		}

		/// <summary>
		/// Initializes the Service
		/// </summary>
		/// <returns></returns>
		public async Task InitializeAsync()
		{
			// Stub: even though we don't need to initialize this service,
			// doing so ensures that it is instantiated when we need it.
			await Task.CompletedTask;
		}

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

			if (DunceDetector.Detect(message.Content))
			{
				await sourceMessage.Channel.SendMessageAsync(text:"lol dunced", messageReference: new MessageReference(message.Id));
			}

			await Task.CompletedTask;
		}
	}
}