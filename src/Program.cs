using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace FrankieBot
{
	class Program
	{
		public static void Main(string[] args)
		{
			new Bot().Run().GetAwaiter().GetResult();
		}
	}
}
