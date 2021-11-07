using System;
using System.Threading;

using Discord;
using Discord.WebSocket;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	public class CronJob : ViewModel<Model.CronJob>
	{

		public CronJob() { }

		public CronJob(DBConnection connection) : base (connection)
		{

		}

		public event EventHandler Run;

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

		public Cronos.CronExpression Cron => Cronos.CronExpression.Parse(CronString);

		public string CronString
		{
			get => Model.CronString;
			set => Model.CronString = value;
		}

		public string Name
		{
			get => Model.Name;
			set => Model.Name = value;
		}

		public bool Active
		{
			get => Model.Active;
			set => Model.Active = value;
		}

		public Timer Timer { get; private set; }

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

		public void Stop()
		{
			Timer.Dispose();
		}

		public override int GetHashCode()
			=> (Guild.Id, Name).GetHashCode();
	}
}