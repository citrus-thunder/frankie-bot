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
	public class ViewModel<M> : IViewModel where M : DBModel, new()
	{
		/// <summary>
		/// Constructs a new empty ViewModel instance
		/// </summary>
		/// <remarks>
		/// It is recommended to use one of the other constructors
		/// whenever possible
		/// </remarks>
		public ViewModel()
		{

		}

		/// <summary>
		/// Constructs a new ViewModel instance and establishes a
		/// database connection
		/// </summary>
		/// <param name="connection"></param>
		/// <remarks>
		/// This constructor is recommended when manually creating a new ViewModel.
		/// Calls the virtual Create method upon completion
		/// </remarks>
		public ViewModel(DBConnection connection)
		{
			Connection = connection;
			Model = new M();
			Create();
		}

		/// <summary>
		/// Creates a new ViewModel instance and populates it with a model from
		/// the database matching the parameters in the provided expression
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="expression"></param>
		/// <returns></returns>
		public ViewModel(DBConnection connection, Func<M, bool> expression) : this()
		{
			Connection = connection;
			Model = connection.Table<M>().Where(expression).FirstOrDefault();
			Initialize();
		}

		/// <summary>
		/// Whether the ViewModel contains a valid Model
		/// </summary>
		/// <remarks>
		/// An empty ViewModel suggests no valid records were found when creating it, or its internal Model
		/// was otherwise never set
		/// </remarks>
		public bool IsEmpty => Model == null;

		/// <summary>
		/// Unique ID
		/// </summary>
		/// <returns></returns>
		public int ID => Model.ID;

		/// <summary>
		/// The connection to the database
		/// </summary>
		/// <value></value>
		public DBConnection Connection { get; set; }

		/// <summary>
		/// The underlying Model managed by this ViewModel
		/// </summary>
		/// <value></value>
		protected M Model { get; private set; }

		DBModel IViewModel.Model
		{
			get => Model;
			set
			{
				if (Model == null)
				{
					Model = value as M;
				}
			}
		}

		/// <summary>
		/// Creates a new ViewModel
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public static ViewModel<M> Create(DBConnection connection)
		{
			var res = new ViewModel<M>()
			{
				Connection = connection,
				Model = new M()
			};
			res.Create();
			return res;
		}

		/// <summary>
		/// Finds the first ViewModel matching the given expression
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="expression"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Finds all ViewModels
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public static ViewModelContainer<IViewModel> FindAll(DBConnection connection)
		{
			var container = new ViewModelContainer<IViewModel>();
			var models = connection.Table<M>();
			foreach (var m in models)
			{
				var newModel = new ViewModel<M>
				{
					Model = m,
					Connection = connection
				};
				newModel.Initialize();
				container.Content.Add(newModel);
			}
			return container;
		}

		/// <summary>
		/// Finds a ViewModel by a given ID
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public static ViewModel<M> Find(DBConnection connection, int id)
		{
			var model = connection.Table<M>().Where((model) => model.ID == id).FirstOrDefault();
			ViewModel<M> res = new ViewModel<M>();
			if (model != null)
			{
				res = new ViewModel<M>
				{
					Model = model,
					Connection = connection
				};
				res.Initialize();
			}
			return res;
		}

		/// <summary>
		/// Finds all ViewModels matching the given expression
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="expression"></param>
		/// <returns></returns>
		public static ViewModelContainer<IViewModel> Find(DBConnection connection, Func<M, bool> expression)
		{
			var container = new ViewModelContainer<IViewModel>();
			var models = connection.Table<M>().Where(expression).ToList<M>();
			foreach (var m in models)
			{
				var newModel = new ViewModel<M>
				{
					Model = m,
					Connection = connection
				};
				newModel.Initialize();
				container.Content.Add(newModel);
			}
			return container;
		}

		/// <summary>
		/// Converts a generic ViewModel to a concrete equivalent
		/// </summary>
		/// <typeparam name="K"></typeparam>
		/// <returns></returns>
		public K As<K>() where K : ViewModel<M>, new()
		{
			var c = new K();
			if (this.Model != null)
			{
				c.Model = this.Model;
				c.Connection = this.Connection;
				c.Initialize();
			}
			return c;
		}

		/// <summary>
		/// Initializes the ViewModel
		/// </summary>
		public virtual void Initialize()
		{

		}

		/// <summary>
		/// Called when a new ViewModel is created
		/// </summary>
		/// <remarks>
		/// Called after <see cref="Connection"/> is assigned and
		/// a new default <see cref="Model"/> is instantiated
		/// </remarks>
		protected virtual void Create()
		{

		}

		/// <summary>
		/// Saves the Model to the Database
		/// </summary>
		public virtual void Save()
		{
			if (Connection.Table<M>().Any(m => m.ID == Model.ID))
			{
				Connection.Update(Model);
			}
			else
			{
				Connection.Insert(Model);
			}
		}

		/// <summary>
		/// Deletes the Model from the Database
		/// </summary>
		public virtual void Delete()
		{
			Connection.Delete(Model);
		}
	}
}