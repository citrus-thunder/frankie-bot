using System;

using Discord;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	public class ProgressReport : ViewModel<Model.ProgressReport>
	{
		public IUser User
		{
			get => m_User;
			set
			{
				if (m_User != value)
				{
					m_User = value;
					OnUpdateUser();
				}
			}
		}
		private IUser m_User;

		public ProgressReportWindow Window
		{
			get => m_Window;
			set
			{
				if (m_Window != value)
				{
					m_Window = value;
					OnUpdateWindow();
				}
			}
		}
		private ProgressReportWindow m_Window;

		public int WordCount
		{
			get => Model.WordCount;
			set => Model.WordCount = value;
		}

		public string Note
		{
			get => Model.Note;
			set => Model.Note = value;
		}

		public DateTime TimeStamp
		{
			get => Model.TimeStamp;
			set => Model.TimeStamp = value;
		}

		private void OnUpdateUser()
		{
			Model.UserID = User.Id.ToString();
		}

		private void OnUpdateWindow()
		{
			Model.WindowID = m_Window.ID;
		}
	}
}