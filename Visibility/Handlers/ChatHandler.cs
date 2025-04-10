using System;
using System.Linq;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using Visibility.Configuration;


namespace Visibility.Handlers;

public class ChatHandler: IDisposable
{
	private readonly VisibilityConfiguration configuration;

	public ChatHandler(VisibilityConfiguration config)
	{
		this.configuration = config;

		Service.ChatGui.ChatMessage += this.OnChatMessage;
	}

	public void Dispose()
	{
		Service.ChatGui.ChatMessage -= this.OnChatMessage;
	}

	private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message,
		ref bool isHandled)
	{
		if (!this.configuration.Enabled)
		{
			return;
		}

		if (isHandled)
		{
			return;
		}

		PlayerPayload? playerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
		PlayerPayload? emotePlayerPayload =
			message.Payloads.FirstOrDefault(x => x is PlayerPayload) as PlayerPayload;
		bool isEmoteType = type is XivChatType.CustomEmote or XivChatType.StandardEmote;

		if (playerPayload == null &&
		    (!isEmoteType || emotePlayerPayload == null))
		{
			return;
		}

		if (this.configuration.VoidList.Any(x =>
			    x.HomeworldId ==
			    (isEmoteType ? emotePlayerPayload?.World.RowId : playerPayload?.World.RowId)
			    && x.Name == (isEmoteType ? emotePlayerPayload?.PlayerName : playerPayload?.PlayerName)))
		{
			isHandled = true;
		}
	}
}
