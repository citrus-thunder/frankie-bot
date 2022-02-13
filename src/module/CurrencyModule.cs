using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.WebSocket;
using Discord.Commands;

using SQLite;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;
using FrankieBot.DB.Container;
using FrankieBot.Discord.Services;
using FrankieBot.Discord;

namespace FrankieBot.Discord.Modules
{
	/// <summary>
	/// Module containing currency and redemption functionality
	/// </summary>
	[Group("currency")]
	[Alias("c")]
	public class CurrencyModule : ModuleBase<SocketCommandContext>
	{
		#region Options
		/// <summary>
		/// Option title for the option which enables the Currency module
		/// </summary>
		public const string OptionEnabled = "currency_module_enabled";
		#endregion // Options

		/// <summary>
		/// The module's DataBaseService reference
		/// </summary>
		/// <value></value>
		public DataBaseService DataBaseService { get; set; }

		/// <summary>
		/// Initializes the currency module
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static async Task Initialize(IServiceProvider services)
		{
			var db = services.GetRequiredService<DataBaseService>();
			await db.RunForAllGuilds(async guild =>
			{
				await InitializeOptions(guild, db);
			});
		}

		private static async Task InitializeOptions(IGuild guild, DataBaseService database)
		{
			await database.RunGuildDBAction(guild, connection =>
			{
				var enabled = Option.FindOne(connection, o => o.Name == OptionEnabled).As<Option>();
				if (enabled.IsEmpty)
				{
					enabled = new Option(connection)
					{
						Name = OptionEnabled
					};
					enabled.Initialize();
					enabled.Save();
				}
			});
		}

		/// <summary>
		/// Enables the currency module
		/// </summary>
		/// <returns></returns>
		[Command("enable")]
		[Alias("on", "true")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task EnableModule()
		{
			await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
			{
				var option = Option.FindOne(connection, o => o.Name == OptionEnabled).As<Option>();

				if (option.IsEmpty)
				{
					option = new Option(connection)
					{
						Name = OptionEnabled
					};
					option.Initialize();
				}

				option.Value = "true";
				option.Save();
			});
		}

		/// <summary>
		/// Disables the currency module
		/// </summary>
		/// <returns></returns>
		[Command("disable")]
		[Alias("off", "false")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task DisableModule()
		{
			await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
			{
				var option = Option.FindOne(connection, o => o.Name == OptionEnabled).As<Option>();

				if (option.IsEmpty)
				{
					option = new Option(connection)
					{
						Name = OptionEnabled
					};
					option.Initialize();
				}

				option.Value = "false";
				option.Save();
			});
		}

		/// <summary>
		/// Transfers currency from the commanding user to the given user
		/// </summary>
		/// <returns></returns>
		[Command("give")]
		[Alias("gift", "pay")]
		public async Task GiveCurrency(IUser recipient, int amount, [Remainder] string currency)
		{
			/* todo
			* Validate user & currency
			  * ensure currency valid & giver has enough
				  * validate currency via name.tolower
				* ensure target user is valid
			* Reduce invoking user's chosen currency by amount
			* Increase target user's chosen currency by amount
			*/
		}

		/// <summary>
		/// Grants a user new currency
		/// </summary>
		/// <returns></returns>
		[Command("grant")]
		[Alias("reward")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task GrantCurrency(IUser recipient, int amount, [Remainder] string currency)
		{
			/* todo
			* Validate user & currency
				* ensure currency valid
					* validate currency via name.tolower
				* ensure target user is valid
			* Increment target's chosen currency by amount
			*/
		}

		/// <summary>
		/// Attempts to purchase the given redemption using the highest
		/// priority currency
		/// </summary>
		/// <param name="redemptionName"></param>
		/// <returns></returns>
		[Command("buy")]
		[Alias("redeem", "purchase")]
		public async Task BuyRedemption([Remainder] string redemptionName)
		{
			/* todo
			* Validate redemption via name.tolower
			* Get list of valid currencies for redemption, ordered by priority (good ViewModel method?)
				* From list, get list of currencies the user can use based on what
					they can afford, and choose best based on priority
					* If user cannot afford with any currency, error and let them know
			*/
		}

		/// <summary>
		/// Submodule containing currency management commands
		/// </summary>
		[Group("mint")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public class MintOptions : ModuleBase<SocketCommandContext>
		{
			[Command("list")]
			[Alias("ls")]
			public async Task ListCurrencies()
			{

			}

			[Command("new")]
			[Alias("add")]
			public async Task AddCurrency([Remainder] string currencyName)
			{

			}

			[Command("remove")]
			[Alias("delete", "destroy")]
			public async Task RemoveCurrency(int currencyID)
			{
				// note: also delete all currency2user and price records for destroyed currency type
			}

			[Command("editname")]
			[Alias("rename", "name")]
			public async Task EditName(int currencyID, [Remainder] string currencyName)
			{

			}

			[Command("editdescription")]
			[Alias("setdescription", "description")]
			public async Task EditDescription(int currencyID, [Remainder] string description)
			{

			}

			[Command("setpriority")]
			[Alias("priority")]
			public async Task EditPriority(int currencyID, int priority)
			{

			}
		}

		/// <summary>
		/// Submodule containing redemption management commands
		/// </summary>
		[Group("redemption")]
		[Alias("prize", "prizes")]
		public class RedemptionOptions : ModuleBase<SocketCommandContext>
		{
			public DataBaseService DataBaseService { get; set; }

			/// <summary>
			/// Lists all available redemptions and their prices
			/// </summary>
			/// <returns></returns>
			[Command]
			[Alias("list")]
			public async Task ListRedemptions()
			{
				/* todo
				*
				*/
			}

			/// <summary>
			/// Adds a new redemption available for purchase
			/// </summary>
			/// <param name="title"></param>
			/// <returns></returns>
			[Command("add")]
			[Alias("new")]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task AddRedemption([Remainder]string title)
			{
				/* todo
				*
				*/
			}

			/// <summary>
			/// Removes a redemption from the database
			/// </summary>
			/// <param name="redemptionID"></param>
			/// <returns></returns>
			[Command("remove")]
			[Alias("delete")]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task RemoveRedemption(int redemptionID)
			{
				/* todo
				*
				*/
			}

			[Command("edit")]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task EditRedemption(int redemptionID, string title, string description)
			{

			}

			[Command("settitle")]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task SetRedemptionTitle(int redemptionID, [Remainder] string title)
			{

			}

			[Command("setdescription")]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task SetRedemptionDescription(int redemptionID, [Remainder] string description)
			{

			}

			[Command("setprice")]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task SetRedemptionPrice(int redemptionID, int price, [Remainder] string currency)
			{

			}
		}

		/*
		[Group("option")]
		[Alias("set", "o")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public class ModuleOptions : ModuleBase<SocketCommandContext>
		{
			public DataBaseService DataBaseService { get; set; }
		}
		*/
	}
}