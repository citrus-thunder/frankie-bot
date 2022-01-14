using System;

using SQLite;

using Discord;
using Discord.WebSocket;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// Represents a word tracker module subscriber
	/// </summary>
	public class WTSubscriber : ViewModel<Model.WTSubscriber>
	{
		/// <summary>
		/// Creates a new WTSubscriber instance
		/// </summary>
		/// <remarks>
		/// It is recommend to use the other constructor(s) whenever possible
		/// </remarks>
		public WTSubscriber() : base()
		{

		}

		/// <summary>
		/// Creates a new WTSubscriber instance
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public WTSubscriber(SQLiteConnection connection) : base(connection)
		{

		}

		/// <summary>
		/// Represents the subscriber
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
		/// The subscriber's optional custom word count goal
		/// </summary>
		/// <remarks>
		/// If less than 1, it is assumed that the user has not
		/// set a custom goal
		/// </remarks>
		public int CustomGoal
		{
			get => Model.CustomGoal;
			set => Model.CustomGoal = value;
		}

		/// <summary>
		/// Whether the subscriber has set a custom word count goal
		/// </summary>
		public bool HasCustomGoal => CustomGoal > 0;

		/// <summary>
		/// The subscriber's daily word count goal
		/// </summary>
		/// <value></value>
		public int WordCountGoal
		{
			get => Model.WordCountGoal;
			set => Model.WordCountGoal = value;
		}

		/// <summary>
		/// The subscriber's progress toward today's word count goal
		/// </summary>
		/// <value></value>
		public int WordCountProgress
		{
			get => Model.WordCountProgress;
			set => Model.WordCountProgress = value;
		}

		/// <summary>
		/// Initializes this WTSubscriber instance
		/// </summary>
		/// <param name="guild"></param>
		/// <remarks>
		/// Sets the User field based on the provided guild.
		/// </remarks>
		public void Initialize(IGuild guild)
		{
			if (guild is SocketGuild sg)
			{
				User = sg.GetUser(ulong.Parse(Model.UserID));
			}
		}

		private void OnUpdateUser()
		{
			Model.UserID = User.Id.ToString();
		}
	}
}