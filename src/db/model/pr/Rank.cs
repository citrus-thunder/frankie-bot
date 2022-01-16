using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model representing a rank record
	/// </summary>
	[Table("ranks")]
	public class Rank : DBModel
	{
		/// <summary>
		/// Rank's Role ID
		/// </summary>
		/// <value></value>
		[Unique]
		public string RoleID {get; set;}

		/// <summary>
		/// Rank's word count threshold
		/// </summary>
		/// <value></value>
		[Unique]
		public int Threshold {get; set;}
	}
}