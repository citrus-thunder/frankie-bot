using System;
using System.Collections.Generic;
using System.Linq;

using SQLite;

using FrankieBot.DB.Model;

namespace FrankieBot.DB
{
	/// <summary>
	/// Generic ViewModel definition
	/// </summary>
	/// <typeparam name="M"></typeparam>
	/// <remarks>
	/// A ViewModel lies between the main application (the "View"), and the
	/// underlying data (the "Model"). The ViewModel helps to translate data
	/// from the Model into useful information that can be consumed by the View,
	/// and translates user actions from the View into database operations in Model-space.
	/// </remarks>
	public class ViewModel<M> where M : DBModel, new()
	{
		protected ViewModel()
		{

		}

		public ViewModel(DBConnection connection)
		{
			Connection = connection;
			Model = new M();
			Create();
		}

		public ViewModel(DBConnection connection, Func<M, bool> expression) : this()
		{
			Connection = connection;
			Model = connection.Table<M>().Where(expression).FirstOrDefault();
			Initialize();
		}

		protected DBConnection Connection { get; private set; }

		/// <summary>
		/// The underlying Model managed by this ViewModel
		/// </summary>
		/// <value></value>
		protected M Model { get; private set; }

		public static T Create<T>(DBConnection connection) where T : ViewModel<M>, new()
		{
			return new T
			{
				Connection = connection
			};
		}

		public static ViewModel<M> FindOne(DBConnection connection, Func<M, bool> expression)
		{
			var model = connection.Table<M>().Where(expression).FirstOrDefault();
			var res = new ViewModel<M>
			{
				Model = model,
				Connection = connection
			};
			res.Initialize();
			return res;
		}

		public static ViewModelContainer<M> Find(DBConnection connection, Func<M, bool> expression)
		{
			var container = new ViewModelContainer<M>();
			var models = connection.Table<M>().Where(expression).ToList<M>();
			var viewModels = new List<ViewModel<M>>();
			foreach (var m in models)
			{
				var newModel = new ViewModel<M>
				{
					Model = m,
					Connection = connection
				};
				newModel.Initialize();
				viewModels.Add(newModel);
			}
			return new ViewModelContainer<M>() { Content = viewModels as List<ViewModel<M>> };
		}

		protected virtual void Initialize()
		{

		}

		protected virtual void Create()
		{

		}

		public virtual void Save()
		{
			Connection.InsertOrReplace(Model);
		}

		public virtual void Delete()
		{
			Connection.Delete(Model);
		}
	}
}