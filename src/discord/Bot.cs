using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using FrankieBot.Discord.Modules;
using FrankieBot.Discord.Services;

namespace FrankieBot.Discord
{
	/// <summary>
	/// FrankieBot's core application
	/// </summary>
	public class Bot
	{
		/// <summary>
		/// Creates a new Bot instance
		/// </summary>
		public Bot()
		{

		}

		/// <summary>
		/// Begins the Bot's execution loop
		/// </summary>
		/// <returns></returns>
		public async Task Run()
		{
			using (var services = ConfigureServices())
			{
				var client = services.GetRequiredService<DiscordSocketClient>();		
				await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("FRANKIE_TOKEN"));

				await client.StartAsync();

				await services.GetRequiredService<CommandHandlerService>().InitializeAsync();

				await services.GetRequiredService<EavesDropperService>().InitializeAsync();

				// Block task until program close
				await Task.Delay(-1);
			}
		}

		private Task OnBotJoinedServer(SocketGuild server)
		{
			Console.WriteLine($"Joined Server: {server.Name}"); // test
			return Task.CompletedTask; // test
			// todo: add new server to server meta db, or set to active if already exists
		}

		private Task OnBotLeftServer(SocketGuild server)
		{
			Console.WriteLine($"Left Server: {server.Name}"); // test
			return Task.CompletedTask; // test
			// todo: set server record in server meta db to inactive
		}

		private ServiceProvider ConfigureServices()
		{
			return new ServiceCollection()
				.AddSingleton<DiscordSocketClient>()
				.AddSingleton<EavesDropperService>()
				.AddSingleton<CommandService>()
				.AddSingleton<CommandHandlerService>()
				.BuildServiceProvider();
		}
	}
}