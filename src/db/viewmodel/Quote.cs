using System;

using SQLite;

using Discord;
using Discord.WebSocket;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// ViewModel object container for the <see cref="FrankieBot.DB.Model.Quote"/> model
	/// </summary>
	public class Quote : ViewModel<Model.Quote>
	{
		/// <summary>
		/// Creates a new Quote instance
		/// </summary>
		public Quote() { }

		/// <summary>
		/// Creates a new Quote instance
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		//public Quote(DBConnection connection) : base(connection)
		public Quote(SQLiteConnection connection) : base(connection)
		{
			
		}

		/// <summary>
		/// Creates and initializes a new Quote instance
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="connection"></param>
		/// <returns></returns>
		public Quote(IGuild guild, SQLiteConnection connection) : this(connection)
		{
			Initialize(guild);
		}

		/// <summary>
		/// User whom the quote is attributed to
		/// </summary>
		/// <value></value>
		public IUser Author
		{
			get => m_Author;
			set
			{
				if (m_Author != value)
				{
					m_Author = value;
					OnUpdateAuthor();
				}
			}
		}
		private IUser m_Author;

		/// <summary>
		/// User responsible for recording the quote
		/// </summary>
		/// <value></value>
		public IUser Recorder
		{
			get => m_Recorder;
			set
				{
					if (m_Recorder != value)
					{
						m_Recorder = value;
						OnUpdateRecorder();
					}
				}
		}
		private IUser m_Recorder;

		/// <summary>
		/// The quote's body text
		/// </summary>
		public string Content
		{
			get => Model.Content;
			set => Model.Content = value;
		}

		/// <summary>
		/// Timestamp representing when the quote was recorded
		/// </summary>
		public DateTime RecordTimeStamp
		{
			get => Model.RecordTimeStamp;
			set => Model.RecordTimeStamp = value;
		}

		/// <summary>
		/// Timestamp representing when the quote was said
		/// </summary>
		public DateTime QuoteTimeStamp
		{
			get => Model.RecordTimeStamp;
			set => Model.RecordTimeStamp = value;
		}

		/// <summary>
		/// Initializes this Quote instance
		/// </summary>
		/// <param name="guild"></param>
		public void Initialize(IGuild guild)
		{
			if (guild is SocketGuild socketGuild)
			{
				Author = socketGuild.GetUser(ulong.Parse(Model.AuthorID));
				Recorder = socketGuild.GetUser(ulong.Parse(Model.RecorderID));
			}
		}

		private void OnUpdateAuthor()
		{
			Model.AuthorID = Author.Id.ToString();
		}

		private void OnUpdateRecorder()
		{
			Model.RecorderID = Recorder.Id.ToString();
		}
	}
}