using System;

using Discord;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	public class ProgressReportWindow : ViewModel<Model.ProgressReportWindow>
	{
		public DateTime StartTime
		{
			get => Model.StartTime;
			set => Model.StartTime = value;
		}

		public int Duration
		{
			get => Model.Duration;
			set => Model.Duration = value;
		}
	}
}