using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model representing the relationship between users and
	/// currencies
	/// </summary>
	[Table("users2currencies")]
	public class Users2Currency : DBModel
	{
		/// <summary>
		/// Discord user represented in this association
		/// </summary>
		/// <remarks>
		/// Stored as string as SQLitePCL does not support ulong
		/// </remarks>
		public string UserID { get; set; }

		/// <summary>
		/// Currency represented in this association
		/// </summary>
		/// <value></value>
		public int CurrencyID { get; set; }

		/// <summary>
		/// Amount of the associated currency represented
		/// in this association
		/// </summary>
		/// <value></value>
		public int Amount { get; set; }
	}
}