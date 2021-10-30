using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model object representing a single User in the database
	/// </summary>
	[Table("users")]
	public class User
	{
		/// <summary>
		/// Unique Discord User ID
		/// </summary>
		/// <value></value>
		[PrimaryKey, AutoIncrement]
		[Column("id")]
		public int Id { get; set; }
	}
}