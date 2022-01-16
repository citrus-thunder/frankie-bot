using SQLite;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// ViewModel container for the <see cref="FrankieBot.DB.Model.Price"/> model
	/// </summary>
	public class Price : ViewModel<Model.Price>
	{
		/// <summary>
		/// Redemption represented in this association
		/// </summary>
		/// <value></value>
		public Redemption Redemption
		{
			get => _redemption;
			set
			{
				if (_redemption != value)
				{
					_redemption = value;
					Model.RedemptionID = value.ID;
				}
			}
		}
		private Redemption _redemption = null;

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
					Model.CurrencyID = value.ID;
				}
			}
		}
		private Currency _currency = null;

		/// <summary>
		/// Amount of the associated currency required to purchase the
		/// associated redemption
		/// </summary>
		/// <value></value>
		public int Amount
		{
			get => Model.Amount;
			set => Model.Amount = value;
		}

		/// <summary>
		/// Saves the ViewModel to the database
		/// </summary>
		public override void Save()
		{
			if (Amount < 0)
			{
				throw new ConstraintViolationException("Error updating price: Cost must be greater than zero.");
			}
			base.Save();
		}
	}
}