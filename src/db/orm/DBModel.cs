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
		/// Unique ID
		/// </summary>
		/// <value></value>
		[PrimaryKey, AutoIncrement]
		public virtual int ID {get; set;} = -1;
	}
}