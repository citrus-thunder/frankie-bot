using SQLite;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// ViewModel object container for the <see cref="FrankieBot.DB.Model.Currency"/> model
	/// </summary>
	public class Currency : ViewModel<Model.Currency>
	{
		/// <summary>
		/// Creates a new Currency ViewModel instance
		/// </summary>
		/// <remarks>
		/// It is recommended to use the other constructor(s) whenever possible
		/// </remarks>
		public Currency() : base()
		{

		}

		/// <summary>
		/// Creates a new Currency ViewModel instance
		/// </summary>
		/// <param name="connection"></param>
		public Currency(SQLiteConnection connection) : base(connection)
		{

		}

		/// <summary>
		/// Name of the Currency
		/// </summary>
		/// <value></value>
		public string Name
		{
			get => Model.Name;
			set => Model.Name = value;
		}

		/// <summary>
		/// The Currency's detailed description
		/// </summary>
		/// <value></value>
		public string Description
		{
			get => Model.Description;
			set => Model.Description = value;
		}

		/// <summary>
		/// Priority for using this currency over other
		/// equivalent currency
		/// </summary>
		/// <remarks>
		/// Higher value equals higher priority
		/// </remarks>
		public int Priority
		{
			get => Model.Priority;
			set => Model.Priority = value;
		}
	}
}