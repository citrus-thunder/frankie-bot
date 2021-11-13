using System;
using System.Collections.Generic;
using System.Linq;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;

namespace FrankieBot.DB.Container
{
	/// <summary>
	/// Container of <see cref="FrankieBot.DB.ViewModel.Option"/>s
	/// </summary>
	public class Options : ViewModelContainer<Option>
	{
		/// <summary>
		/// Sets an option to the specified value
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void Set(string name, string value)
		{
			var option = Content.Where(o => o.Name == name).FirstOrDefault();

			if (option != null)
			{
				option.Value = value;
				option.Save();
			}
		}

		/// <summary>
		/// Gets the value of the specified option
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public string Get(string name)
		{
			return Content.Where(o => o.Name == name).FirstOrDefault()?.Value;
		}

		/// <summary>
		/// Gets a dictionary containing all options' values
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, string> Get()
		{
			var res = new Dictionary<string, string>();
			foreach (var option in Content)
			{
				res.Add(option.Name, option.Value);
			}
			return res;
		}

		/// <summary>
		/// Gets a dictionary containing the values of the specified options
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public Dictionary<string, string> Get(params string[] options)
		{
			var res = new Dictionary<string, string>();
			foreach (var option in Content)
			{
				if (options.Contains(option.Name))
				{
					res.Add(option.Name, option.Value);
				}
			}
			return res;
		}

		/// <summary>
		/// Attempts to get an option with the given title.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns>True if an option is found, otherise false</returns>
		public bool TryGet(string name, out string value)
		{
			value = Content.Where(o => o.Name == name).FirstOrDefault()?.Value;
			return value != null;
		}
	}
}