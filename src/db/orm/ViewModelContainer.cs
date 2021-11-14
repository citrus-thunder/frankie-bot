using System;
using System.Collections.Generic;

using FrankieBot.DB.Model;

namespace FrankieBot.DB
{
	/// <summary>
	/// Contains multiple ViewModels and eases performing similar operations over the ViewModels contained within
	/// </summary>
	/// <typeparam name="V"></typeparam>
	public class ViewModelContainer<V> where V : IViewModel
	{
		/// <summary>
		/// The ViewModel's contained in this ViewModelContainer
		/// </summary>
		/// <returns></returns>
		public List<V> Content { get; set; } = new List<V>();

		/// <summary>
		/// Whether this container contains any ViewModels
		/// </summary>
		public bool IsEmpty => Content.Count == 0;

		/// <summary>
		/// Converts the ViewModelContainer to a container of a specified type
		/// containing a specified ViewModel type
		/// </summary>
		/// <typeparam name="C">Container type</typeparam>
		/// <typeparam name="K">Content type</typeparam>
		/// <returns></returns>
		public C As<C, K>()
			where C : ViewModelContainer<K>, new() 
			where K : IViewModel, new()
		=> this.ContentAs<K>().ContainerAs<C>();

		/// <summary>
		/// Converts the ViewModelContainer's contents to more concrete equivalent ViewModels
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <returns></returns>
		public ViewModelContainer<K> ContentAs<K>() where K : IViewModel, new()
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

		/// <summary>
		/// Returns a copy of this container as the specified type
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <remarks>
		/// Use this to cast a generic container to a more useful user-defined container
		/// </remarks>
		public K ContainerAs<K>() where K : ViewModelContainer<V>, new()
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