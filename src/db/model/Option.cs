using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model representing an option record
	/// </summary>
	[Table("options")]
	public class Option : DBModel
	{
		/// <summary>
		/// Option Name
		/// </summary>
		/// <value></value>
		public string Name { get; set; }

		/// <summary>
		/// Option Value
		/// </summary>
		/// <value></value>
		public string Value { get; set; }
	}
}