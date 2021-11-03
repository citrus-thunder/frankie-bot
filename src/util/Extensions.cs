using Discord;
using Discord.Commands;
using Discord.WebSocket;

public static class FrankieExtensions
{
	public static bool HasStringPrefix(this IUserMessage message, string[] list, ref int argPos)
	{
		var res = false;
		foreach (var s in list)
		{
			if (res)
			{
				break;
			}
			res = message.HasStringPrefix(s, ref argPos);
		}
		return res;
	}
}