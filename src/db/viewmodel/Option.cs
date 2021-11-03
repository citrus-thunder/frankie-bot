using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// Represents a user-defined option
	/// </summary>
	public class Option : ViewModel<Model.Option>
	{
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
	}
}