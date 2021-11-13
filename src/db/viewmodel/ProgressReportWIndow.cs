using System;

using Discord;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// Represents a Progress Report submission window
	/// </summary>
	public class ProgressReportWindow : ViewModel<Model.ProgressReportWindow>
	{
		/// <summary>
		/// Time the ProgressReportWindow is set to open
		/// </summary>
		/// <value></value>
		public DateTime StartTime
		{
			get => Model.StartTime;
			set => Model.StartTime = value;
		}

		/// <summary>
		/// Duration the ProgressReportWindow will remain open, in hours
		/// </summary>
		/// <value></value>
		public int Duration
		{
			get => Model.Duration;
			set => Model.Duration = value;
		}

		/// <summary>
		/// Time the ProgressReportWindow will close
		/// </summary>
		/// <remarks>
		/// Derived from the <see cref="StartTime"/> and <see cref="Duration"/> properties
		/// </remarks>
		public DateTime EndTime
		{
			get => StartTime.AddHours(Duration);
		}

		/// <summary>
		/// Checks if the given ProgressReportWindow overlaps with this one
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool CheckOverlap(ProgressReportWindow other)
		{
			return (StartTime < other.EndTime && other.StartTime < EndTime);
		}

		/// <summary>
		/// Saves the ProgressReportWindow to the database
		/// </summary>
		public override void Save()
		{
			var windows = ProgressReportWindow.FindAll(Connection).ContentAs<ProgressReportWindow>();
			foreach (var window in windows.Content)
			{
				if (CheckOverlap(window))
				{
					throw new ConstraintViolationException($"Cannot save new Progress Report Window. Overlapping window exists ({window.ID})");
				}
			}
			base.Save();
		}
	}
}