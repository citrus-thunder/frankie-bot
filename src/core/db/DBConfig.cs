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
		/// Root directory for FrankieBot's metadata files
		/// </summary>
		/// <returns></returns>
		public static readonly string METADATA_ROOT;

		/// <summary>
		/// Root directory for FrankieBot's server data files
		/// </summary>
		/// <returns></returns>
		public static readonly string SERVER_DATA_ROOT;

		/// <summary>
		/// Location of FrankieBot's server metadata file
		/// </summary>
		/// <returns></returns>
		public static readonly string SERVER_META_FILE;

		static DBConfig()
		{
			DATABASE_ROOT = Environment.GetEnvironmentVariable("FRANKIE_DB_ROOT");
			METADATA_ROOT = Path.Combine(DATABASE_ROOT, "meta");
			SERVER_DATA_ROOT = Path.Combine(DATABASE_ROOT, "servers");
			SERVER_META_FILE = Path.Combine(METADATA_ROOT, "servers.db");
			Init();
		}

		private static void Init()
		{
			Directory.CreateDirectory(DATABASE_ROOT);
			Directory.CreateDirectory(METADATA_ROOT);
			Directory.CreateDirectory(SERVER_DATA_ROOT);

			if (!File.Exists(SERVER_META_FILE))
			{
				var file = File.Create(SERVER_META_FILE);
				file.Close();
			}
		}
	}
}