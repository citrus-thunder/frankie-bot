using System;
using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model representing a progress report window record
	/// </summary>
	[Table("progress_report_windows")]
	public class ProgressReportWindow : DBModel
	{
		/// <summary>
		/// Time the window is set to open
		/// </summary>
		public DateTime StartTime;

		/// <summary>
		/// Duration, in hours, the window is to remain open
		/// </summary>
		public int Duration;
	}
}