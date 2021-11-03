using System;
using System.Collections.Generic;
using System.Linq;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;

namespace FrankieBot.DB.Containers
{
	public class Options : ViewModelContainer<Option>
	{
		public void Set(string name, string value)
		{
			var option = Content.Where(o => o.Name == name).FirstOrDefault();

			if (option != null)
			{
				option.Value = value;
				option.Save();
			}
		}

		public string Get(string name)
		{
			return Content.Where(o => o.Name == name).FirstOrDefault().Value;
		}

		public Dictionary<string, string> Get()
		{
			var res = new Dictionary<string, string>();
			foreach (var option in Content)
			{
				res.Add(option.Name, option.Value);
			}
			return res;
		}

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
	}
}