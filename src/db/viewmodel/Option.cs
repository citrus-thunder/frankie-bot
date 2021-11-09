using System.Collections.Generic;

using Model = FrankieBot.DB.Model;
using Module = FrankieBot.Discord.Modules;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// Represents a user-defined option
	/// </summary>
	public class Option : ViewModel<Model.Option>
	{
		public static readonly Dictionary<string, string> DefaultValues;
		static Option()
		{
			DefaultValues = new Dictionary<string, string>
			{
			{Module.ProgressReportModule.OptionEnabled, "false"},
			{Module.ProgressReportModule.OptionWindowOpen, "0 0 * * 6"},
			{Module.ProgressReportModule.OptionWindowDuration, "72"}
			};
		}

		public Option() : base()
		{

		}

		public Option(DBConnection connection) : base(connection)
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

		public override void Initialize()
		{
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