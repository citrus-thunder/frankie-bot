using System;

using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model representing a progress report record
	/// </summary>
	[Table("progress_reports")]
	public class ProgressReport : DBModel
	{
		/// <summary>
		/// User ID representing the author of the progress report
		/// </summary>
		public string UserID { get; set; }
		
		/// <summary>
		/// ID of the progress report window this report was submitted to
		/// </summary>
		public int WindowID { get; set; }
		
		/// <summary>
		/// User's submitted word count
		/// </summary>
		public int WordCount { get; set; }

		/// <summary>
		/// Optional user note
		/// </summary>
		public string Note { get; set; }

		/// <summary>
		/// Time the progress report was submitted
		/// </summary>
		public DateTime TimeStamp { get; set; }
	}
}