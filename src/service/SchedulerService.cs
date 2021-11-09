using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.Webhook;

using Cronos;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;

namespace FrankieBot.Discord.Services
{
	/// <summary>
	/// Provides resources for scheduling tasks
	/// </summary>
	public class SchedulerService
	{
		private IServiceProvider _services;
		private DataBaseService _db;

		private List<CronJob> _jobs = new List<CronJob>();

		/// <summary>
		/// Creates a new SchedulerService instance
		/// </summary>
		/// <param name="services"></param>
		public SchedulerService(IServiceProvider services)
		{
			_services = services;
			_db = services.GetRequiredService<DataBaseService>();
		}

		/// <summary>
		/// Adds a job to the scheduler
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		public async Task AddJob(CronJob job)
		{
			if (!_jobs.Contains(job))
			{
				_jobs.Add(job);
				job.Start();
			}
			else
			{
				// remove "equivalent" job and replace with new version
				var j = _jobs[_jobs.IndexOf(job)];
				j.Stop();
				await AddJob(job);
			}
		}

		/// <summary>
		/// Adds a job to the scheduler
		/// </summary>
		/// <param name="context"></param>
		/// <param name="name"></param>
		/// <param name="cron"></param>
		/// <returns></returns>
		public async Task<CronJob> AddJob(SocketCommandContext context, string name, string cron)
		{
			CronJob res = null;
			await _db.RunDBAction(context, c =>
			{
				using (var connection = new DBConnection(c, _db.GetServerDBFilePath(c.Guild)))
				{
					var job = CronJob.FindOne(connection, j => j.Name == name).As<CronJob>();

					if (job.IsEmpty)
					{
						job = new CronJob(connection);
					}

					job.Name = name;
					job.CronString = cron;
					job.Guild = context.Guild;
					job.Save();

					res = job;
				}
			});
			await AddJob(res);
			return res;
		}

		/// <summary>
		/// Finds a matching job in the scheduler
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public CronJob GetJob(IGuild guild, string name)
			=> _jobs.Where(j => j.Guild == guild && j.Name == name).FirstOrDefault();

		private void ClearJobs()
		{
			foreach (var job in _jobs)
			{
				job.Stop();
			}
			_jobs.Clear();
		}
	}
}