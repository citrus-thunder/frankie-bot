using System;

using Discord;
using Discord.WebSocket;

using Model = FrankieBot.DB.Model;

namespace FrankieBot.DB.ViewModel
{
	/// <summary>
	/// ViewModel object container for the <see cref="FrankieBot.DB.Model.Quote"/> model
	/// </summary>
	public class Quote : ViewModel<Model.Quote>
	{
		private SocketGuild _guild;

		/// <summary>
		/// Constructs a new Quote
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		public Quote(SocketGuild guild, Model.Quote model) : base(model)
		{
			_guild = guild;
			User = _guild.GetUser(ulong.Parse(Model.AuthorID));
			Recorder = _guild.GetUser(ulong.Parse(Model.RecorderID));
		}

		/// <summary>
		/// User whom the quote is attributed to
		/// </summary>
		/// <value></value>
		public IUser User { get; private set; }

		/// <summary>
		/// User responsible for recording the quote
		/// </summary>
		/// <value></value>
		public IUser Recorder { get; private set; }

		/// <summary>
		/// The quote's body text
		/// </summary>
		public string Content => Model.Content;

		/// <summary>
		/// Timestamp representing when the quote was recorded
		/// </summary>
		public DateTime RecordTimeStamp => Model.RecordTimeSamp;

		/// <summary>
		/// Timestamp representing when the quote was said
		/// </summary>
		public DateTime QuoteTimeStamp => Model.QuoteTimeStamp;
	}
}