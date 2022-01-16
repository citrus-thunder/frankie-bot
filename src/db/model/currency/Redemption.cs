using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model representing items that can be redeemed with currency
	/// </summary>
	[Table("redemptions")]
	public class Redemption : DBModel
	{
		/// <summary>
		/// Name of the Redemption
		/// </summary>
		/// <value></value>
		public string Name { get; set; }

		/// <summary>
		/// The Redemption's detailed description
		/// </summary>
		/// <value></value>
		public string Description { get; set; }
	}
}