using SQLite;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// ViewModel container for the <see cref="FrankieBot.DB.Model.Redemption"/> model
	/// </summary>
	public class Redemption : ViewModel<Model.Redemption>
	{
		/// <summary>
		/// Name of the redemption
		/// </summary>
		/// <value></value>
		public string Name
		{
			get => Model.Name;
			set => Model.Name = value;
		}

		/// <summary>
		/// The redemption's detailed description
		/// </summary>
		/// <value></value>
		public string Description
		{
			get => Model.Description;
			set => Model.Description = value;
		}

		public override void Delete()
		{
			if (Connection != null)
			{
				// todo: remove prices assocated with this redemption
				base.Delete();
			}
		}
	}
}