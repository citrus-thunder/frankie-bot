using System;

using SQLite;

namespace FrankieBot.DB.Model
{
	/// <summary>
	/// Model representing a word tracker subscriber
	/// </summary>
	[Table("wt_subscribers")]
	public class WTSubscriber : DBModel
	{
		/// <summary>
		/// User ID representing the subscriber
		/// </summary>
		/// <value></value>
		public string UserID { get; set; }

		/// <summary>
		/// The subscriber's daily wordcount goal
		/// </summary>
		/// <value></value>
		public int WordCountGoal { get; set; }

		/// <summary>
		/// The subscriber's progress toward today's wordcount goal
		/// </summary>
		/// <value></value>
		public int WordCountProgress { get; set; }
	}
}