using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model object representing a scheduled job
	/// </summary>
	[Table("jobs")]
	public class CronJob : DBModel
	{
		/// <summary>
		/// ID of Guild which owns the job
		/// </summary>
		/// <value></value>
		public string GuildID { get; set; }

		/// <summary>
		/// String representing the job's cron schedule
		/// </summary>
		/// <value></value>
		public string CronString { get; set; }

		/// <summary>
		/// Name of the job
		/// </summary>
		/// <value></value>
		public string Name { get; set; }

		/// <summary>
		/// Whether the job is active
		/// </summary>
		/// <value></value>
		public bool Active { get; set; }
	}
}