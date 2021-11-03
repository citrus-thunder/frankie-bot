using Discord;
using Discord.Commands;
using Discord.WebSocket;

/// <summary>
/// Extension Class for FrankieBot
/// </summary>
public static class FrankieExtensions
{
	/// <summary>
	/// Checks if a message has a prefix matching any strings in the given list
	/// </summary>
	/// <param name="message"></param>
	/// <param name="list"></param>
	/// <param name="argPos"></param>
	/// <returns></returns>
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