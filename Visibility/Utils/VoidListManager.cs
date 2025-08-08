using System;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Visibility.Void;

namespace Visibility.Utils;

/// <summary>
/// Manages void and whitelist functionality for game objects
/// </summary>
public class VoidListManager
{
	private readonly Dictionary<uint, long> checkedVoidedObjectIds = new(capacity: 1000);
	private readonly Dictionary<uint, long> checkedWhitelistedObjectIds = new(capacity: 1000);
	private readonly Dictionary<uint, long> voidedObjectIds = new(capacity: 1000);
	private readonly Dictionary<uint, long> whitelistedObjectIds = new(capacity: 1000);

	/// <summary>
	/// Check if an object is in the void list
	/// </summary>
	public unsafe bool CheckAndProcessVoidList(Character* characterPtr)
	{
		if (this.checkedVoidedObjectIds.ContainsKey(characterPtr->GameObject.EntityId))
			return this.voidedObjectIds.ContainsKey(characterPtr->GameObject.EntityId);

		if (!VisibilityPlugin.Instance.Configuration.VoidDictionary.TryGetValue(characterPtr->ContentId,
			    out VoidItem? voidedPlayer))
		{
			voidedPlayer = VisibilityPlugin.Instance.Configuration.VoidList.Find(x =>
				characterPtr->GameObject.Name.StartsWith(x.NameBytes) &&
				x.HomeworldId == characterPtr->HomeWorld);
		}

		if (voidedPlayer != null)
		{
			if (voidedPlayer.Id == 0)
			{
				voidedPlayer.Id = characterPtr->ContentId;
				VisibilityPlugin.Instance.Configuration.Save();
				VisibilityPlugin.Instance.Configuration.VoidDictionary[characterPtr->ContentId] = voidedPlayer;
			}

			voidedPlayer.ObjectId = characterPtr->GameObject.EntityId;
			this.voidedObjectIds[characterPtr->GameObject.EntityId] = Environment.TickCount64;
		}

		this.checkedVoidedObjectIds[characterPtr->GameObject.EntityId] = Environment.TickCount64;

		return this.voidedObjectIds.ContainsKey(characterPtr->GameObject.EntityId);
	}

	/// <summary>
	/// Check if an object is in the whitelist
	/// </summary>
	public unsafe bool CheckAndProcessWhitelist(Character* characterPtr)
	{
		if (this.checkedWhitelistedObjectIds.ContainsKey(characterPtr->GameObject.EntityId))
			return this.whitelistedObjectIds.ContainsKey(characterPtr->GameObject.EntityId);

		if (!VisibilityPlugin.Instance.Configuration.WhitelistDictionary.TryGetValue(characterPtr->ContentId,
			    out VoidItem? whitelistedPlayer))
		{
			whitelistedPlayer = VisibilityPlugin.Instance.Configuration.Whitelist.Find(x =>
				characterPtr->GameObject.Name.StartsWith(x.NameBytes) &&
				x.HomeworldId == characterPtr->HomeWorld);
		}

		if (whitelistedPlayer != null)
		{
			if (whitelistedPlayer.Id == 0)
			{
				whitelistedPlayer.Id = characterPtr->ContentId;
				VisibilityPlugin.Instance.Configuration.Save();
				VisibilityPlugin.Instance.Configuration.WhitelistDictionary[characterPtr->ContentId] =
					whitelistedPlayer;
			}

			whitelistedPlayer.ObjectId = characterPtr->GameObject.EntityId;
			this.whitelistedObjectIds[characterPtr->GameObject.EntityId] = Environment.TickCount64;
		}

		this.checkedWhitelistedObjectIds[characterPtr->GameObject.EntityId] = Environment.TickCount64;

		return this.whitelistedObjectIds.ContainsKey(characterPtr->GameObject.EntityId);
	}

	/// <summary>
	/// Check if an object ID is in the void list
	/// </summary>
	public bool IsObjectVoided(uint objectId)
	{
		return this.voidedObjectIds.ContainsKey(objectId);
	}

	/// <summary>
	/// Check if an object ID is in the whitelist
	/// </summary>
	public bool IsObjectWhitelisted(uint objectId)
	{
		return this.whitelistedObjectIds.ContainsKey(objectId);
	}

	/// <summary>
	/// Remove an object from the checked lists
	/// </summary>
	public void RemoveChecked(uint id)
	{
		this.voidedObjectIds.Remove(id);
		this.whitelistedObjectIds.Remove(id);
		this.checkedVoidedObjectIds.Remove(id);
		this.checkedWhitelistedObjectIds.Remove(id);
	}

	/// <summary>
	/// Clear all lists
	/// </summary>
	public void ClearAll()
	{
		this.checkedVoidedObjectIds.Clear();
		this.checkedWhitelistedObjectIds.Clear();
		this.voidedObjectIds.Clear();
		this.whitelistedObjectIds.Clear();
	}
}
