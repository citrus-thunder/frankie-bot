using System;
using System.Collections.Generic;

using FrankieBot.DB.Model;

namespace FrankieBot.DB
{
	/// <summary>
	/// Contains multiple ViewModels and eases performing similar operations over the ViewModels contained within
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ViewModelContainer<T> where T : IViewModel
	{
		/// <summary>
		/// The ViewModel's contained in this ViewModelContainer
		/// </summary>
		/// <returns></returns>
		public List<T> Content { get; set; } = new List<T>();

		/// <summary>
		/// Converts the ViewModelContainer's contents to more concrete equivalent ViewModels
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <returns></returns>
		public ViewModelContainer<K> As<K>() where K : IViewModel, new()
		{
			var container = new ViewModelContainer<K>();
			foreach (var item in Content)
			{
				var c = new K();
				c.Model = item.Model;
				c.Connection = item.Connection;
				c.Initialize();
				container.Content.Add(c);
			}
			return container;
		}

		public K ToContainer<K>() where K : ViewModelContainer<T>, new()
		{
			var container = new K();
			foreach (var item in Content)
			{
				container.Content.Add(item);
			}
			return container;
		}

		/// <summary>
		/// Saves the current state of all contained ViewModels to the database
		/// </summary>
		public void Save() => Content.ForEach(c => c.Save());

		/// <summary>
		/// Deletes all contained ViewModels from the database
		/// </summary>
		public void Delete() => Content.ForEach(c => c.Delete());
		
	}
}