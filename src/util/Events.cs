using System;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;

/// <summary>
/// Contains basic data required for handling and displaying
/// </summary>
public class QuoteEventArgs : EventArgs
{
	/// <summary>
	/// Constructs new QuoteEventArgs
	/// </summary>
	/// <returns></returns>
	public QuoteEventArgs() : base()
	{

	}
	/// <summary>
	/// Quote model
	/// </summary>
	/// <value></value>
	public Quote Quote { get; set; }

	/// <summary>
	/// Quote context
	/// </summary>
	/// <value></value>
	public SocketCommandContext Context { get; set; }
}