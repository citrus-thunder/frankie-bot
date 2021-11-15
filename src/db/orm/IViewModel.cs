using FrankieBot.DB;
using FrankieBot.DB.Model;

using SQLite;

/// <summary>
/// ViewModel Interface
/// </summary>
public interface IViewModel
{
	/// <summary>
	/// Underlying Model managed by the ViewModel
	/// </summary>
	/// <value></value>
	DBModel Model { get; set; }

	/// <summary>
	/// Database Connection
	/// </summary>
	/// <value></value>
	//DBConnection Connection { get; set; }
	SQLiteConnection Connection { get; set; }

	/// <summary>
	/// Initializes the ViewModel
	/// </summary>
	/// <remarks>
	/// Call after the creation of a new ViewModel, after <see cref="Model"/> and
	/// <see cref="Connection"/> are assigned.
	/// </remarks>
	void Initialize();

	/// <summary>
	/// Saves the current state of the <see cref="Model"/> to the database specified by <see cref="Connection"/>
	/// </summary>
	void Save();

	/// <summary>
	/// Deletes the <see cref="Model"/> from the database specified by <see cref="Connection"/>
	/// </summary>
	void Delete();
}