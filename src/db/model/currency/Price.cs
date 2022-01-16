using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model representing prices associated with redemptions
	/// </summary>
	[Table("prices")]
	public class Price : DBModel
	{
		/// <summary>
		/// Redemption associated with this Price
		/// </summary>
		/// <value></value>
		public int RedemptionID { get; set; }

		/// <summary>
		/// Currency associated with this Price
		/// </summary>
		/// <value></value>
		public int CurrencyID { get; set; }

		/// <summary>
		/// Amount of associated currency which must be exchanged for the
		/// associated redeemable
		/// </summary>
		/// <value></value>
		public int Amount { get; set; }
	}
}