using System;

using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model object representing a Quote record
	/// </summary>
	[Table("quotes")]
	public class Quote : DBModel
	{
		/// <summary>
		/// Constructs a new Quote
		/// </summary>
		/// <remarks>
		/// It is recommended to use the alternate constructor
		/// </remarks>
		public Quote()
		{

		}
		/*
		/// <summary>
		/// Constructs a new Quote
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public Quote(DBConnection connection) : base(connection)
		{

		}
		*/

		/// <summary>
		/// The quote's content
		/// </summary>
		public string Content { get; set; }

		/// <summary>
		/// ID of the user who said the quote
		/// </summary>
		public string AuthorID { get; set; }

		/// <summary>
		/// ID of the user who submitted the quote
		/// </summary>
		public string RecorderID { get; set; }

		/// <summary>
		/// Time the quote was recorded
		/// </summary>
		public DateTime RecordTimeStamp { get; set; }

		/// <summary>
		/// Time the quote was originally said
		/// </summary>
		public DateTime QuoteTimeStamp { get; set; }
	}
}