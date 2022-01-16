using SQLite;

using Discord;
using Discord.WebSocket;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// ViewModel container for the <see cref="FrankieBot.DB.Model.Users2Currency"/> model
	/// </summary>
	public class Users2Currency : ViewModel<Model.Users2Currency>
	{
		/// <summary>
		/// User represented in this association
		/// </summary>
		/// <value></value>
		public IUser User
		{
			get => _user;
			set
			{
				if (_user != value)
				{
					_user = value;
					Model.UserID = value.Id.ToString();
				}
			}
		}
		private IUser _user = null;

		/// <summary>
		/// Currency represented in this association
		/// </summary>
		/// <value></value>
		public Currency Currency
		{
			get => _currency;
			set
			{
				if (_currency != value)
				{
					_currency = value;
					Model.CurrencyID = _currency.ID;
				}
			}
		}
		private Currency _currency = null;

		/// <summary>
		/// Amount of the associated currency owned by the associated user
		/// </summary>
		/// <value></value>
		public int Amount
		{
			get => Model.Amount;
			set => Model.Amount = value;
		}

		/// <summary>
		/// Initializes the ViewModel, populating complex contextual fields
		/// based on the given guild
		/// </summary>
		/// <param name="guild"></param>
		/// <remarks>
		/// Call this whenever possible after retrieving this ViewModel from a query!
		/// </remarks>
		public void Initialize(IGuild guild)
		{
			if (guild is SocketGuild g)
			{
				User = g.GetUser(ulong.Parse(Model.UserID));
			}
		}
	}
}