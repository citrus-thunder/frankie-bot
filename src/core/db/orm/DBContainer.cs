using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using SQLite;

namespace FrankieBot.DB
{
	/// <summary>
	/// Contains a set of homogenous <see cref="FrankieBot.DB.DBModel"/> objects
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DBContainer<T> where T : DBModel, new()
	{
		private List<T> _content;

		/// <summary>
		/// Constructs a new DBContainer
		/// </summary>
		public DBContainer()
		{
			_content = new List<T>();
			Content = _content.AsReadOnly();
		}

		/// <summary>
		/// Constructs a new DBContainer
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public DBContainer(SQLiteConnection connection) : this()
		{
			SetConnection(connection);
		}

		/// <summary>
		/// The DBContainer's DBConnection
		/// </summary>
		/// <value></value>
		public SQLiteConnection Connection { get; private set; }

		/// <summary>
		/// The DBModel objects within this Container
		/// </summary>
		/// <value></value>
		public IList<T> Content { get; private set; }

		/// <summary>
		/// Creates a new DBContainer and populates with with DBModels matching the expression
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="expression"></param>
		/// <returns></returns>
		public static DBContainer<T> Find(SQLiteConnection connection, Expression<Func<T, bool>> expression)
		{
			var container = new DBContainer<T>(connection);
			container.Add(connection.Table<T>().Where(expression).ToList<T>());
			return container;
		}

		/// <summary>
		/// Sets this container's DBConnection
		/// </summary>
		/// <param name="connection"></param>
		/// <remarks>
		/// Will only set the DBConnection reference if
		/// a reference isn't set already
		/// </remarks>
		public void SetConnection(SQLiteConnection connection)
		{
			if (Connection == null)
			{
				Connection = connection;
			}
		}

		/// <summary>
		/// Adds a DBModel to this container
		/// </summary>
		/// <param name="item"></param>
		public void Add(T item) => _content.Add(item);

		/// <summary>
		/// Adds a DBModel to this container
		/// </summary>
		/// <param name="items"></param>
		public void Add(params T[] items) => _content.AddRange(items);

		/// <summary>
		/// Adds a DBModel to this container
		/// </summary>
		/// <param name="items"></param>
		public void Add(List<T> items) => Add(items.ToArray());

		/// <summary>
		/// Performs the given action for each item in this container
		/// </summary>
		/// <param name="action"></param>
		public void ForEach(Action<T> action)
		{
			_content.ForEach(action);
		}

		/// <summary>
		/// Saves changes to all objects in this container
		/// </summary>
		public void Save() => ForEach((m) => m.Save());
	}
}