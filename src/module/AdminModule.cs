using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SQLite;

using Discord;
using Discord.Commands;

using FrankieBot.Discord.Services;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;

namespace FrankieBot.Discord.Modules
{
	/// <summary>
	/// Administrative command module
	/// </summary>
	[Group("admin")]
	[RequireUserPermission(GuildPermission.Administrator)]
	public class AdminModule : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// Provides database services
		/// </summary>
		/// <value></value>
		public DataBaseService DataBaseService { get; set; }

		/// <summary>
		/// Provides command handling services
		/// </summary>
		/// <value></value>
		public CommandHandlerService CommandHandlerService { get; set; }

		/// <summary>
		/// Sets the prefix used to identify commands in chat messages
		/// </summary>
		/// <param name="prefix"></param>
		/// <returns></returns>
		[Command("prefix")]
		public async Task SetPrefix(string prefix)
		{
			if (!CommandHandlerService.PrefixOptions.Contains(prefix))
			{
				await Context.Channel.SendMessageAsync($"\"{prefix}\" is not a valid prefix option. Please select one of {string.Join(", ", CommandHandlerService.PrefixOptions)}");
				return;
			}

			await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
			{
				var prefixOption = Option.FindOne(connection, o => o.Name == "command_prefix").As<Option>();
				prefixOption.Value = prefix;
				prefixOption.Save();
			});

			await Context.Channel.SendMessageAsync($"Command prefix has been updated to \"{prefix}\"");
		}
	}
}