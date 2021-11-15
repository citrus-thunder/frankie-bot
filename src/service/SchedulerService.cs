using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using SQLite;

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
		/// Initializes jobs for all guilds
		/// </summary>
		/// <returns></returns>
		public async Task Initialize()
		{
			// todo: loop through guilds and init all jobs (call other oberload for each guild)
			await Task.CompletedTask; // temp
		}

		/// <summary>
		/// Initializes jobs for the given guild
		/// </summary>
		/// <param name="guild"></param>
		/// <returns></returns>
		public async Task Initialize(IGuild guild)
		{
			await ClearJobs(guild);
			// todo: loop through guild's jobs and get them started if active
			// - we may need an OnResume() method in the CronJob viewmodel to
			// special cases when picking up a job after it's started
			await Task.CompletedTask; // temp
		}

		/// <summary>
		/// Adds a job to the scheduler
		/// </summary>
		/// <param name="job"></param>
		/// <param name="autoStart"></param>
		/// <returns></returns>
		public async Task AddJob(CronJob job, bool autoStart = true)
		{
			if (!_jobs.Contains(job))
			{
				_jobs.Add(job);
				if (autoStart)
				{
					job.Start();
				}
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
		/// <param name="autoStart"></param>
		/// <returns></returns>
		public async Task<CronJob> AddJob(SocketCommandContext context, string name, string cron, bool autoStart = true)
		{
			CronJob res = null;
			await _db.RunGuildDBAction(context.Guild, connection =>
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
			});
			await AddJob(res, autoStart);
			return res;
		}

		/// <summary>
		/// Stops a job and removes it from the scheduler
		/// </summary>
		/// <param name="context"></param>
		/// <param name="name"></param>
		public void RemoveJob(SocketCommandContext context, string name)
		=> RemoveJob(context.Guild, name);

		/// <summary>
		/// Stops a job and removes it from the scheduler
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="name"></param>
		public void RemoveJob(IGuild guild, string name)
		{
			var job = _jobs.Where(j => j.Name == name).FirstOrDefault();
			if (job != null)
			{
				job.Stop();
				_jobs.Remove(job);
			}
		}

		/// <summary>
		/// Finds a matching job in the scheduler
		/// </summary>
		/// <param name="context"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public CronJob GetJob(SocketCommandContext context, string name)
		=> GetJob(context.Guild, name);

		/// <summary>
		/// Finds a matching job in the scheduler
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public CronJob GetJob(IGuild guild, string name)
			=> _jobs.Where(j => j.Guild == guild && j.Name == name).FirstOrDefault();

		private async Task ClearJobs(IGuild guild)
		{
			await Task.Run(() =>
			{
				_jobs.Where(j => j.Guild.Id == guild.Id).ToList().ForEach((job) =>
				{
					RemoveJob(guild, job.Name);
				});
			});
		}
	}
}