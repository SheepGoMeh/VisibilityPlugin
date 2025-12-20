using System;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;


// alias vers l’enum du JEU (celui du struct GameObject)
using GameVisibilityFlags = FFXIVClientStructs.FFXIV.Client.Game.Object.VisibilityFlags;

namespace Visibility.Utils;

/// <summary>
/// Manages the visibility state of game objects
/// </summary>
public class ObjectVisibilityManager
{
	private readonly Dictionary<uint, long> hiddenObjectIds = new(capacity: 200);
	private readonly Dictionary<uint, long> objectIdsToShow = new(capacity: 200);
	private readonly Dictionary<uint, long> hiddenMinionObjectIds = new(capacity: 200);
	private readonly Dictionary<uint, long> minionObjectIdsToShow = new(capacity: 200);

	public enum ObjectType
	{
		Character,
		Companion
	}

	private const VisibilityFlags InvisibleFlag = VisibilityFlags.Invisible;

	/// <summary>
	/// Hide a game object by setting its render flags
	/// </summary>
	public unsafe void HideGameObject(Character* thisPtr, ObjectType objectType = ObjectType.Character)
	{
		// On travaille sur un int pour manipuler les bits
		int current = (int)thisPtr->GameObject.RenderFlags;

		switch (objectType)
		{
			case ObjectType.Character when !current.TestFlag(InvisibleFlag):
				this.hiddenObjectIds[thisPtr->GameObject.EntityId] = Environment.TickCount64;
				current |= (int)InvisibleFlag;
				thisPtr->GameObject.RenderFlags = (GameVisibilityFlags)current;
				break;

			case ObjectType.Companion when !current.TestFlag(InvisibleFlag):
				this.hiddenMinionObjectIds[thisPtr->CompanionOwnerId] = Environment.TickCount64;
				current |= (int)InvisibleFlag;
				thisPtr->GameObject.RenderFlags = (GameVisibilityFlags)current;
				break;
		}
	}

	/// <summary>
	/// Show a previously hidden game object
	/// </summary>
	public unsafe bool ShowGameObject(Character* thisPtr, ObjectType objectType = ObjectType.Character)
	{
		int current = (int)thisPtr->GameObject.RenderFlags;

		switch (objectType)
		{
			case ObjectType.Character
				when this.objectIdsToShow.ContainsKey(thisPtr->GameObject.EntityId) &&
					 current.TestFlag(InvisibleFlag):

				this.hiddenObjectIds.Remove(thisPtr->GameObject.EntityId);
				this.objectIdsToShow.Remove(thisPtr->GameObject.EntityId);

				current &= ~(int)InvisibleFlag;
				thisPtr->GameObject.RenderFlags = (GameVisibilityFlags)current;
				return true;

			case ObjectType.Companion
				when this.minionObjectIdsToShow.ContainsKey(thisPtr->CompanionOwnerId) &&
					 current.TestFlag(InvisibleFlag):

				this.hiddenMinionObjectIds.Remove(thisPtr->CompanionOwnerId);
				this.minionObjectIdsToShow.Remove(thisPtr->CompanionOwnerId);

				current &= ~(int)InvisibleFlag;
				thisPtr->GameObject.RenderFlags = (GameVisibilityFlags)current;
				return true;
		}

		return false;
	}

	public bool IsObjectHidden(uint entityId, ObjectType objectType = ObjectType.Character)
	{
		return objectType == ObjectType.Character
			? this.hiddenObjectIds.ContainsKey(entityId)
			: this.hiddenMinionObjectIds.ContainsKey(entityId);
	}

	public void MarkObjectToShow(uint entityId, ObjectType objectType = ObjectType.Character)
	{
		if (objectType == ObjectType.Character)
		{
			if (!this.hiddenObjectIds.ContainsKey(entityId))
				return;

			this.objectIdsToShow[entityId] = Environment.TickCount64;
			this.hiddenObjectIds.Remove(entityId);
		}
		else
		{
			if (!this.hiddenMinionObjectIds.ContainsKey(entityId))
				return;

			this.minionObjectIdsToShow[entityId] = Environment.TickCount64;
			this.hiddenMinionObjectIds.Remove(entityId);
		}
	}

	public void ClearAll()
	{
		this.hiddenObjectIds.Clear();
		this.objectIdsToShow.Clear();
		this.hiddenMinionObjectIds.Clear();
		this.minionObjectIdsToShow.Clear();
	}
}
