using System;
using System.IO;

namespace FrankieBot.DB
{
	/// <summary>
	/// Static class containing database configuration definitions
	/// </summary>
	public static class DBConfig
	{
		/// <summary>
		/// Root directory for FrankieBot's database filesystem
		/// </summary>
		public static readonly string DATABASE_ROOT;
		
		/// <summary>
		/// Root directory for FrankieBot's metadata files
		/// </summary>
		/// <returns></returns>
		public static string METADATA_ROOT => Path.Combine(DATABASE_ROOT, "meta");

		/// <summary>
		/// Root directory for FrankieBot's server data files
		/// </summary>
		/// <returns></returns>
		public static string SERVER_DATA_ROOT => Path.Combine(DATABASE_ROOT, "servers");

		/// <summary>
		/// Location of FrankieBot's server metadata file
		/// </summary>
		/// <returns></returns>
		public static string SERVER_META_FILE = Path.Combine(METADATA_ROOT, "servers.db");

		static DBConfig()
		{
			DATABASE_ROOT = Environment.GetEnvironmentVariable("FRANKIE_DB_ROOT");
			Init();
		}

		private static void Init()
		{
			if (!Directory.Exists(DATABASE_ROOT))
			{
				Directory.CreateDirectory(DATABASE_ROOT);
			}

			if (!File.Exists(SERVER_META_FILE))
			{
				var file = File.Create(SERVER_META_FILE);
				file.Close();
			}
		}
	}
}