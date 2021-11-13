using System;
using SQLite;

namespace FrankieBot.DB.Model
{
	[Table("progress_report_windows")]
	public class ProgressReportWindow : DBModel
	{
		public DateTime StartTime;
		public int Duration;
	}
}