using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

using FrankieBot.Discord.Services;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;

namespace FrankieBot.Discord.Modules
{
	[Group("admin")]
	[RequireUserPermission(GuildPermission.Administrator)]
	public class AdminModule : ModuleBase<SocketCommandContext>
	{
		public DataBaseService DataBaseService { get; set; }
		public CommandHandlerService CommandHandlerService { get; set; }
		[Command("prefix")]
		public async Task SetPrefix(string prefix)
		{
			await DataBaseService.RunDBAction(Context, async context => 
			{
				if (!CommandHandlerService.PrefixOptions.Contains(prefix))
				{
					await Context.Channel.SendMessageAsync($"\"{prefix}\" is not a valid prefix option. Please select one of {string.Join(", ", CommandHandlerService.PrefixOptions)}");
					return;
				}

				using (var connection = new DBConnection(context, DataBaseService.GetServerDBFilePath(context.Guild)))
				{
					var prefixOption = Option.FindOne(connection, o => o.Name == "command_prefix").As<Option>();
					prefixOption.Value = prefix;
					prefixOption.Save();
					await Context.Channel.SendMessageAsync($"Command prefix has been updated to \"{prefix}\"");
				}
			});
		}
	}
}