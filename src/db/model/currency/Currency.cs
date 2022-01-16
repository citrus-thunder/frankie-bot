using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model representing a denomination of currency
	/// </summary>
	[Table("currencies")]
	public class Currency : DBModel
	{
		/// <summary>
		/// Name of the currency
		/// </summary>
		/// <value></value>
		public string Name { get; set; }

		/// <summary>
		/// Detailed description of the currency
		/// </summary>
		/// <value></value>
		public string Description { get; set; }

		/// <summary>
		/// Priority for using this currency over other
		/// equivalent currency
		/// </summary>
		/// <remarks>
		/// Higher value equals higher priority
		/// </remarks>
		public int Priority { get; set; } = 0;
	}
}