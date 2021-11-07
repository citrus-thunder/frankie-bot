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
		/// File extension used by database files
		/// </summary>
		public const string DATABASE_FILE_EXTENSION = ".db";

		/// <summary>
		/// Root directory for FrankieBot's database filesystem
		/// </summary>
		public static readonly string DATABASE_ROOT;
		
		/// <summary>
		/// Root directory for FrankieBot's global data files
		/// </summary>
		/// <returns></returns>
		public static readonly string GLOBAL_DATA_ROOT;

		/// <summary>
		/// Root directory for FrankieBot's server data files
		/// </summary>
		/// <returns></returns>
		public static readonly string SERVER_DATA_ROOT;

		/// <summary>
		/// Location of FrankieBot's server global data db file
		/// </summary>
		/// <returns></returns>
		public static readonly string GLOBAL_DATA_FILE;

		static DBConfig()
		{
			DATABASE_ROOT = Environment.GetEnvironmentVariable("FRANKIE_DB_ROOT");
			GLOBAL_DATA_ROOT = Path.Combine(DATABASE_ROOT, "global");
			SERVER_DATA_ROOT = Path.Combine(DATABASE_ROOT, "servers");
			GLOBAL_DATA_FILE = Path.Combine(GLOBAL_DATA_ROOT, "global" + DATABASE_FILE_EXTENSION);
			Init();
		}

		private static void Init()
		{
			Directory.CreateDirectory(DATABASE_ROOT);
			Directory.CreateDirectory(GLOBAL_DATA_ROOT);
			Directory.CreateDirectory(SERVER_DATA_ROOT);

			if (!File.Exists(GLOBAL_DATA_FILE))
			{
				var file = File.Create(GLOBAL_DATA_FILE);
				file.Close();
			}
		}
	}
}