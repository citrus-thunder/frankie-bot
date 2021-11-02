using System;
using System.Collections.Generic;

using FrankieBot.DB.Model;

namespace FrankieBot.DB
{
	public class ViewModelContainer<M> where M : DBModel, new()
	{
		/// <summary>
		/// The ViewModel's contained in this ViewModelContainer
		/// </summary>
		/// <returns></returns>
		public List<ViewModel<M>> Content { get; set; } = new List<ViewModel<M>>();

		/// <summary>
		/// Converts this container to its less-generic version
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ViewModelContainer<T> ToConcrete<T>() where T : ViewModel<M>, new()
		{
			var list = new List<ViewModel<T>>();

			var container = new ViewModelContainer<T>();
		}
	}
}