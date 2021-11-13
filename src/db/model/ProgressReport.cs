using System;

using SQLite;

namespace FrankieBot.DB.Model
{
	[Table("progress_reports")]
	public class ProgressReport : DBModel
	{
		public string UserID;
		public int WindowID;
		public int WordCount;
		public string Note;
		public DateTime TimeStamp;
	}
}