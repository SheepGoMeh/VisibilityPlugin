using System;
using System.Linq;

using Dalamud.Game.Chat;
using Dalamud.Game.Text;
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

	private void OnChatMessage(IHandleableChatMessage message)
	{
		if (!this.configuration.Enabled)
		{
			return;
		}

		if (message.IsHandled)
		{
			return;
		}

		PlayerPayload? playerPayload = message.Sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
		PlayerPayload? emotePlayerPayload =
			message.Message.Payloads.FirstOrDefault(x => x is PlayerPayload) as PlayerPayload;
		bool isEmoteType = message.LogKind is XivChatType.CustomEmote or XivChatType.StandardEmote;

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
			message.PreventOriginal();
		}
	}
}
