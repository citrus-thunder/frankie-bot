using System;

using Discord;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// Represents a user-submitted progress report
	/// </summary>
	public class ProgressReport : ViewModel<Model.ProgressReport>
	{
		/// <summary>
		/// Creates a new ProgressReport instance
		/// </summary>
		/// <remarks>
		/// It is recommended to use the other constructor whenever possible
		/// </remarks>
		public ProgressReport() : base()
		{

		}

		/// <summary>
		/// Creates a new ProgressReport instance
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public ProgressReport(DBConnection connection) : base(connection)
		{

		}

		/// <summary>
		/// User who submitted the progress report
		/// </summary>
		/// <value></value>
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

		/// <summary>
		/// The <see cref="ProgressReportWindow"/> this report was submitted to
		/// </summary>
		/// <value></value>
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

		/// <summary>
		/// Word count recorded as part of this progress report
		/// </summary>
		/// <value></value>
		public int WordCount
		{
			get => Model.WordCount;
			set => Model.WordCount = value;
		}

		/// <summary>
		/// Optional user note recorded as part of this progress report
		/// </summary>
		/// <value></value>
		public string Note
		{
			get => Model.Note;
			set => Model.Note = value;
		}

		/// <summary>
		/// Time the progress report was submitted
		/// </summary>
		/// <value></value>
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