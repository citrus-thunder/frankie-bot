using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	public class Option : ViewModel<Model.Option>
	{
		public string Name
		{
			get => Model.Name;
			set => Model.Name = value;
		}

		public string Value
		{
			get => Model.Value;
			set => Model.Value = value;
		}
	}
}