using System;

using SQLite;

using Discord;
using Discord.WebSocket;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// ViewModel object container for the <see cref="FrankieBot.DB.Model.Rank"/> model
	/// </summary>
	public class Rank : ViewModel<Model.Rank>
	{
		/// <summary>
		/// Creates a new Rank ViewModel instance
		/// </summary>
		/// <remarks>
		/// It is recommended to use one of the other constructors whenever possible
		/// </remarks>
		public Rank() : base()
		{

		}

		/// <summary>
		/// Creates a new Rank ViewModel instance
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public Rank(SQLiteConnection connection) : base(connection)
		{

		}

		/// <summary>
		/// Creates and initializes a new Rank instance
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="connection"></param>
		/// <returns></returns>
		public Rank(IGuild guild, SQLiteConnection connection) : this(connection)
		{
			Initialize(guild);
		}

		/// <summary>
		/// Role for this rank
		/// </summary>
		/// <value></value>
		public IRole Role
		{
			get => m_Role;
			set
			{
				if (m_Role != value)
				{
					m_Role = value;
					Model.RoleID = m_Role.Id.ToString();
				}
			}
		}
		private IRole m_Role;

		/// <summary>
		/// Word count threshold to qualify for this rank
		/// </summary>
		/// <value></value>
		public int Threshold
		{
			get => Model.Threshold;
			set => Model.Threshold = value;
		}

		/// <summary>
		/// Initializes this Rank instance
		/// </summary>
		/// <param name="guild"></param>
		public void Initialize(IGuild guild)
		{
			if (guild is SocketGuild socketGuild)
			{
				Role = socketGuild.GetRole(ulong.Parse(Model.RoleID));
			}
		}

		/// <summary>
		/// Saves the Rank record
		/// </summary>
		public override void Save()
		{
			try
			{
				base.Save();
			}
			catch (Exception)
			{
				throw new ConstraintViolationException(message: $"Unable to save Rank.");
			}
		}
	}
}