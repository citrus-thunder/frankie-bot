using FrankieBot.Discord;

class Program
{
	public static void Main(string[] args)
	{
		new Bot().Run().GetAwaiter().GetResult();
	}
}
