namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// Abstract ViewModel definition
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	/// A ViewModel lies between the main application (the "View"), and the
	/// underlying data (the "Model"). The ViewModel helps to translate data
	/// from the Model into useful information that can be consumed by the View,
	/// and translates user actions from the View into database operations in Model-space.
	/// </remarks>
	public abstract class ViewModel<T> where T : DBModel
	{
		/// <summary>
		/// Constructs a new ViewModel
		/// </summary>
		/// <param name="model"></param>
		public ViewModel(T model)
		{
			Model = model;
		}

		/// <summary>
		/// The underlying Model managed by this ViewModel
		/// </summary>
		/// <value></value>
		public T Model { get; private set; }
	}
}