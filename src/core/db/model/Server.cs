using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model object representing a Discord server
	/// </summary>
	[Table("Servers")]
	public class Server
	{
		/// <summary>
		/// Unique Discord Server ID
		/// </summary>
		[PrimaryKey, AutoIncrement]
		[Column("id")]
		public int Id { get; set; }
	}
}