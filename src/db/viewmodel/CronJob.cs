using System;
using System.Threading;

using SQLite;

using Discord;
using Discord.WebSocket;

using Cronos;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// Represents a task that runs on a consistent schedule
	/// </summary>
	public class CronJob : ViewModel<Model.CronJob>
	{
		/// <summary>
		/// Creates a new empty CronJob instance
		/// </summary>
		/// <remarks>
		/// It is recommended to use one of the other constructors
		/// whenever possible
		/// </remarks>
		public CronJob() { }

		/// <summary>
		/// Creates a new empty CronJob instance
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		//public CronJob(DBConnection connection) : base (connection)
		public CronJob(SQLiteConnection connection) : base(connection)
		{

		}

		/// <summary>
		/// Event fired when the job is run
		/// </summary>
		public event EventHandler Run;

		/// <summary>
		/// Guild which owns the job
		/// </summary>
		/// <value></value>
		public IGuild Guild
		{
			get => m_Guild;
			set
			{
				if (m_Guild != value)
				{
					m_Guild = value;
					Model.GuildID = m_Guild.Id.ToString();
				}
			}
		}

		private IGuild m_Guild;

		/// <summary>
		/// Formatted cron expression representing the job's schedule
		/// </summary>
		/// <returns></returns>
		public Cronos.CronExpression Cron => Cronos.CronExpression.Parse(CronString);

		/// <summary>
		/// String representing the job's cron schedule
		/// </summary>
		/// <value></value>
		public string CronString
		{
			get => Model.CronString;
			set => Model.CronString = value;
		}

		/// <summary>
		/// Name of the job
		/// </summary>
		/// <value></value>
		public string Name
		{
			get => Model.Name;
			set => Model.Name = value;
		}

		/// <summary>
		/// Whether the job is active
		/// </summary>
		/// <value></value>
		public bool Active
		{
			get => Model.Active;
			set => Model.Active = value;
		}

		/// <summary>
		/// Job's internal Timer
		/// </summary>
		/// <value></value>
		public Timer Timer { get; private set; }
		
		/// <summary>
		/// Validates the CronJob, ensuring a valid cron schedule is
		/// present
		/// </summary>
		/// <param name="cronString"></param>
		/// <param name="errorDescription"></param>
		/// <returns></returns>
		public static bool TryValidate(string cronString, out string errorDescription)
		{
			var res = false;
			errorDescription = null;

			if (cronString != null && cronString != "")
			{
				try
				{
					var cron = CronExpression.Parse(cronString);
					if (cron != null)
					{
						res = true;
					}
				}
				catch (Exception ex)
				{
					errorDescription = ex.Message;
					res = false;
				}
			}

			return res;
		}

		/// <summary>
		/// Starts the job
		/// </summary>
		public void Start()
		{
			var nextStart = Cron.GetNextOccurrence(DateTime.UtcNow, true);
			var startAfter = Cron.GetNextOccurrence((DateTime)nextStart);
			var wait = (TimeSpan)(nextStart - DateTime.UtcNow);
			var nextWait = (TimeSpan)(startAfter - nextStart);

			Timer = new Timer(
				(state) => Run?.Invoke(this, EventArgs.Empty),
				null,
				wait,
				nextWait);
		}

		/// <summary>
		/// Starts the job at the given time
		/// </summary>
		/// <param name="time"></param>
		/// <param name="interval"></param>
		public void StartAt(DateTime time, TimeSpan interval)
		{
			if (time < DateTime.UtcNow)
			{
				return;
			}

			Timer = new Timer(
				(state) => Run?.Invoke(this, EventArgs.Empty),
				null,
				time - DateTime.UtcNow,
				interval);
		}

		/// <summary>
		/// Stops the job
		/// </summary>
		public void Stop()
		{
			Timer.Change(Timeout.Infinite, Timeout.Infinite);
			Timer.Dispose();
			Timer = null;
		}

		/// <summary>
		/// Saves the CronJob
		/// </summary>
		public override void Save()
		{
			if (TryValidate(out string message))
			{
				base.Save();
			}
			else
			{
				throw new InvalidOperationException($"Error saving CronJob: {message}");
			}
		}

		/// <summary>
		/// Returns a unique hashcode representation of the job
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
			=> (Guild.Id, Name).GetHashCode();

		/// <summary>
		/// Validates the CronJob, checking for valid
		/// cron schedule
		/// </summary>
		/// <param name="errorDescription"></param>
		/// <returns></returns>
		public bool TryValidate(out string errorDescription)
		{
			return TryValidate(CronString, out errorDescription);
		}
	}
}