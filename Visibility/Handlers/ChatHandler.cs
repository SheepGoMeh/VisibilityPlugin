using System;
using System.Linq;

using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using Visibility.Configuration;
using Visibility.Void;


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

		PlayerPayload? payload = isEmoteType ? emotePlayerPayload : playerPayload;
		uint? worldId = payload?.World.RowId;
		string? name = payload?.PlayerName;

		VoidItem? match = this.configuration.VoidList
			.FirstOrDefault(x => x.HomeworldId == worldId && x.Name == name);

		if (match == null)
		{
			return;
		}

		if (match.ShowPublicChat && IsPublicChannel(message.LogKind))
		{
			return;
		}

		message.PreventOriginal();
	}

	private static bool IsPublicChannel(XivChatType chatType) => chatType is
		XivChatType.Say or
		XivChatType.Shout or
		XivChatType.Yell or
		XivChatType.CustomEmote or
		XivChatType.StandardEmote or
		XivChatType.Party or
		XivChatType.CrossParty or
		XivChatType.Alliance;
}
