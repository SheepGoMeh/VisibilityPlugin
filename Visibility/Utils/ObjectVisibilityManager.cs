using System;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

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

	/// <summary>
	/// Enum to distinguish between different object types
	/// </summary>
	public enum ObjectType
	{
		Character,
		Companion
	}

	/// <summary>
	/// Hide a game object by setting its render flags
	/// </summary>
	public unsafe void HideGameObject(Character* thisPtr, ObjectType objectType = ObjectType.Character)
	{
		switch (objectType)
		{
			case ObjectType.Character when !thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenObjectIds[thisPtr->GameObject.EntityId] = Environment.TickCount64;
				thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
				break;
			case ObjectType.Companion when !thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenMinionObjectIds[thisPtr->CompanionOwnerId] = Environment.TickCount64;
				thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
				break;
		}
	}

	/// <summary>
	/// Show a previously hidden game object
	/// </summary>
	public unsafe bool ShowGameObject(Character* thisPtr, ObjectType objectType = ObjectType.Character)
	{
		switch (objectType)
		{
			case ObjectType.Character when this.objectIdsToShow.ContainsKey(thisPtr->GameObject.EntityId) &&
			                               thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenObjectIds.Remove(thisPtr->GameObject.EntityId);
				this.objectIdsToShow.Remove(thisPtr->GameObject.EntityId);
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				return true;
			case ObjectType.Companion when this.minionObjectIdsToShow.ContainsKey(thisPtr->CompanionOwnerId) &&
			                               thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenMinionObjectIds.Remove(thisPtr->CompanionOwnerId);
				this.minionObjectIdsToShow.Remove(thisPtr->CompanionOwnerId);
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				return true;
		}

		return false;
	}

	/// <summary>
	/// Check if an object is currently hidden
	/// </summary>
	public bool IsObjectHidden(uint entityId, ObjectType objectType = ObjectType.Character)
	{
		return objectType == ObjectType.Character
			? this.hiddenObjectIds.ContainsKey(entityId)
			: this.hiddenMinionObjectIds.ContainsKey(entityId);
	}

	/// <summary>
	/// Mark an object to be shown
	/// </summary>
	public void MarkObjectToShow(uint entityId, ObjectType objectType = ObjectType.Character)
	{
		if (objectType == ObjectType.Character)
		{
			if (!this.hiddenObjectIds.ContainsKey(entityId)) return;

			this.objectIdsToShow[entityId] = Environment.TickCount64;
			this.hiddenObjectIds.Remove(entityId);
		}
		else
		{
			if (!this.hiddenMinionObjectIds.ContainsKey(entityId)) return;

			this.minionObjectIdsToShow[entityId] = Environment.TickCount64;
			this.hiddenMinionObjectIds.Remove(entityId);
		}
	}

	/// <summary>
	/// Clear all visibility states
	/// </summary>
	public void ClearAll()
	{
		this.hiddenObjectIds.Clear();
		this.objectIdsToShow.Clear();
		this.hiddenMinionObjectIds.Clear();
		this.minionObjectIdsToShow.Clear();
	}
}
