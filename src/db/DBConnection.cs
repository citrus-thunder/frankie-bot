using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using SQLite;

namespace FrankieBot.DB
{
	/// <summary>
	/// Manages connection between FrankieBot a database
	/// </summary>
	public class DBConnection : SQLiteConnection
	{
		/// <summary>
		/// Constructs a new DBConnection
		/// </summary>
		/// <param name="context">Command context which created this connection</param>
		/// <param name="path">Path to the target database</param>
		/// <returns></returns>
		public DBConnection(SocketCommandContext context, string path) : base(path)
		{
			Context = context;	
		}

		/// <summary>
		/// Command context which created the connection
		/// </summary>
		/// <value></value>
		public SocketCommandContext Context { get; private set; }
	}
}