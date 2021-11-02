using SQLite;
namespace FrankieBot.DB.Model
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
		/// Unique ID for this DBModel
		/// </summary>
		/// <value></value>
		[PrimaryKey, AutoIncrement]
		public string ID {get; protected set;}
	}
}