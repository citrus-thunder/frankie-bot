using System;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;

public class ScheduledEventArgs : EventArgs
{
	public ScheduledEventArgs() : base()
	{

	}
	public string EventTitle {get; set;}
	public string CronString {get; set;}
}