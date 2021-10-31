using System;
using System.Linq;
using System.Linq.Expressions;

using SQLite;
namespace FrankieBot.DB
{
	/// <summary>
	/// Model representing a database record
	/// </summary>
	public abstract class DBModel
	{
		/// <summary>
		/// Constructs a new DBModel object
		/// </summary>
		protected DBModel() { }

		/// <summary>
		/// Constructs a new DBModel object
		/// </summary>
		/// <param name="connection"></param>
		public DBModel(SQLiteConnection connection)
		{
			SetConnection(connection);
		}
		
		/// <summary>
		/// Unique ID for this DBModel
		/// </summary>
		/// <value></value>
		[PrimaryKey, AutoIncrement]
		public string ID {get; protected set;}

		/// <summary>
		/// DBConnection reference for this DBModel
		/// </summary>
		/// <value></value>
		public SQLiteConnection Connection { get; private set; }

		/// <summary>
		/// Sets the DBConnection reference for this DBModel
		/// </summary>
		/// <param name="connection"></param>
		/// <remarks>
		/// 
		/// </remarks>
		public void SetConnection(SQLiteConnection connection)
		{
			if (Connection == null)
			{
				Connection = connection;
			}
		}
		
		/// <summary>
		/// Saves changes to the database
		/// </summary>
		public virtual void Save()
		{
			Connection.InsertOrReplace(this);
		}

		/// <summary>
		/// Deletes the DBModel record for the database
		/// </summary>
		public virtual void Delete()
		{
			Connection.Delete(this);
		}
	}
}