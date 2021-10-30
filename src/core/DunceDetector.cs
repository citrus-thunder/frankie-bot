using System.Collections.Generic;

namespace FrankieBot
{
	/// <summary>
	/// Detects phrases that should result in being dunced
	/// </summary>
	public static class DunceDetector
	{
		/// <summary>
		/// List of exact 
		/// </summary>
		public static readonly List<string> TriggerPhrases;

		static DunceDetector()
		{
			TriggerPhrases = new List<string>
			{
				"i should be writing",
				"i should write"
			};
		}

		/// <summary>
		/// Finds trigger phrases in the given string
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static bool Detect(string input)
		{
			var res = false;
			foreach (var trigger in TriggerPhrases)
			{
				res = input.ToLower().Contains(trigger.ToLower());
				if (res)
				{
					break;
				}
			}
			return res;
		}
	}
}