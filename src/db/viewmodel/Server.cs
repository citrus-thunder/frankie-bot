using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// ViewModel object container for the <see cref="FrankieBot.DB.Model.Server"/> model
	/// </summary>
	public class Server : ViewModel<Model.Server>
	{
		/// <summary>
		/// Constructs a new server Viewmodel
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public Server(Model.Server model) : base(model)
		{
			// NYI
		}
	}
}