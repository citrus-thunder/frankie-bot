using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model object representing a scheduled job
	/// </summary>
	[Table("jobs")]
	public class CronJob : DBModel
	{
		public string GuildID { get; set; }
		public string CronString { get; set; }
		public string Name { get; set; }
		public bool Active { get; set; }
	}
}