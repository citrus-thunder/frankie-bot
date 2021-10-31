using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using SQLite;

namespace FrankieBot.DB
{
	/// <summary>
	/// Manages connection between FrankieBot a database
	/// </summary>
	public class DBConnection : SQLiteConnection
	{
		/// <summary>
		/// Constructs a new DBConnection
		/// </summary>
		/// <param name="path">Path to the target database</param>
		/// <returns></returns>
		public DBConnection(string path) : base(path)
		{
			
		}

		/// <summary>
		/// Finds a DBModel of the matching type with the matching ID
		/// </summary>
		/// <param name="id"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Find<T>(ulong id) where T : DBModel, new()
		{
			return Table<T>().Where(m => ulong.Parse(m.ID) == id).FirstOrDefault();
		}

		/// <summary>
		/// Finds DBModels of the matching type using the provided expression
		/// </summary>
		/// <param name="expression"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public new DBContainer<T> Find<T>(Expression<Func<T, bool>> expression) where T : DBModel, new()
		{
			return DBContainer<T>.Find(this, expression);
		}

		/// <summary>
		/// Finds a single DBModel of the matching type using the given expression
		/// </summary>
		/// <param name="expression"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T FindOne<T>(Expression<Func<T, bool>> expression) where T : DBModel, new()
		{
			return Table<T>().Where(expression).FirstOrDefault();
		}
	}
}