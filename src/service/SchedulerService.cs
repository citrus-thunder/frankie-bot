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
	public class SchedulerService
	{
		private IServiceProvider _services;
		private DataBaseService _db;

		private List<CronJob> _jobs = new List<CronJob>();

		public SchedulerService(IServiceProvider services)
		{
			_services = services;
			_db = services.GetRequiredService<DataBaseService>();
		}

		public async Task Initialize()
		{
			// Build _jobs from db(s)
		}

		public async Task AddJob(CronJob job)
		{
			if (!_jobs.Contains(job))
			{
				_jobs.Add(job);
				job.Start();
			}
		}

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

		public CronJob GetJob(IGuild guild, string name)
			=> _jobs.Where(j => j.Guild == guild && j.Name == name).FirstOrDefault();

		private async Task ClearJobs()
		{
			foreach (var job in _jobs)
			{
				job.Stop();
			}
			_jobs.Clear();
		}
	}
}