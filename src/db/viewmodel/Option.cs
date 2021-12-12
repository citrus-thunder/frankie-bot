using System.Collections.Generic;

using SQLite;

using Model = FrankieBot.DB.Model;
using Module = FrankieBot.Discord.Modules;
using Service = FrankieBot.Discord.Services;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// Represents a user-defined option
	/// </summary>
	public class Option : ViewModel<Model.Option>
	{
		/// <summary>
		/// Default values for common options
		/// </summary>
		public static readonly Dictionary<string, string> DefaultValues;
		static Option()
		{
			DefaultValues = new Dictionary<string, string>
			{
			{Module.ProgressReportModule.OptionEnabled, "false"},
			{Module.ProgressReportModule.OptionWindowOpen, "0 0 * * 5"},
			{Module.ProgressReportModule.OptionWindowDuration, "96"},
			{Module.ProgressReportModule.OptionRestrictReportChannel, "false"},
			{Module.ProgressReportModule.OptionRanksEnabled, "true"},
			{Service.CommandHandlerService.OptionCommandPrefix, "."}
			};
		}

		/// <summary>
		/// Creates a new Option ViewModel instance
		/// </summary>
		/// <remarks>
		/// It is recommended to use one of the other constructors whenever possible
		/// </remarks>
		public Option() : base()
		{

		}

		/// <summary>
		/// Creates a new Option ViewModel instance
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		//public Option(DBConnection connection) : base(connection)
		public Option(SQLiteConnection connection) : base(connection)
		{

		}

		/// <summary>
		/// The option's name
		/// </summary>
		/// <value></value>
		public string Name
		{
			get => Model.Name;
			set => Model.Name = value;
		}

		/// <summary>
		/// The option's value
		/// </summary>
		/// <value></value>
		public string Value
		{
			get => Model.Value;
			set => Model.Value = value;
		}

		/// <summary>
		/// Initializes a new Option, setting default values if a value is not already set
		/// </summary>
		public override void Initialize()
		{
			if (Model == null)
			{
				return;
			}

			if (Name == null)
			{
				return;
			}

			if (Value == null || Value == string.Empty)
			{
				if (DefaultValues.TryGetValue(Name, out string value))
				{
					Value = value;
				}
			}
		}
	}
}