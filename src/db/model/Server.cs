using System;

using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model object representing a Discord server
	/// </summary>
	[Table("servers")]
	public class Server : DBModel
	{
		/*
		/// <summary>
		/// Unique Discord Server ID
		/// </summary>
		[PrimaryKey, AutoIncrement]
		public int ID { get; set; }
		*/

		/// <summary>
		/// Timestamp representing the last time Frankie joined this server
		/// </summary>
		/// <value></value>
		public DateTime LastJoinedTimestamp { get; set; }

		/// <summary>
		/// Timestamp representing the last time Frankie left this server
		/// </summary>
		/// <value></value>
		public DateTime LastLeftTimestamp { get; set; }
	}
}