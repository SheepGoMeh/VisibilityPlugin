using System;
using System.Collections.Generic;
using System.Linq;

namespace Visibility.Utils;

/// <summary>
/// Manages containers for different unit types and their relationships
/// </summary>
public class ContainerManager
{
	// Flat [unitType, containerType] lookup instead of nested dictionaries
	private readonly Dictionary<uint, long>[,] containers = new Dictionary<uint, long>[4, 4];
	private readonly HashSet<uint> idsToDelete = new(capacity: 200);

	public ContainerManager()
	{
		for (int u = 0; u < 4; ++u)
		for (int c = 0; c < 4; ++c)
			this.containers[u, c] = new Dictionary<uint, long>();
	}

	private Dictionary<uint, long> Get(UnitType unitType, ContainerType containerType) =>
		this.containers[(int)unitType, (int)containerType];

	/// <summary>
	/// Add an entity to a specific container
	/// </summary>
	public void AddToContainer(UnitType unitType, ContainerType containerType, uint entityId)
	{
		this.Get(unitType, containerType)[entityId] = Environment.TickCount64;
	}

	/// <summary>
	/// Remove an entity from a specific container
	/// </summary>
	public void RemoveFromContainer(UnitType unitType, ContainerType containerType, uint entityId)
	{
		this.Get(unitType, containerType).Remove(entityId);
	}

	/// <summary>
	/// Check if an entity exists in a specific container
	/// </summary>
	public bool IsInContainer(UnitType unitType, ContainerType containerType, uint entityId)
	{
		return this.Get(unitType, containerType).ContainsKey(entityId);
	}

	/// <summary>
	/// Cleanup expired entries from all containers
	/// </summary>
	public void CleanupContainers()
	{
		long expiry = Environment.TickCount64 - 5000;

		foreach (Dictionary<uint, long> container in this.containers)
		{
			foreach ((uint id, long ticks) in container)
				if (ticks < expiry)
					this.idsToDelete.Add(id);

			foreach (uint id in this.idsToDelete) container.Remove(id);

			this.idsToDelete.Clear();
		}
	}

	/// <summary>
	/// Get all entities in a specific container
	/// </summary>
	public IEnumerable<KeyValuePair<uint, long>> GetContainerEntities(UnitType unitType, ContainerType containerType)
	{
		return this.Get(unitType, containerType).ToList();
	}

	/// <summary>
	/// Clear a specific container
	/// </summary>
	public void ClearContainer(UnitType unitType, ContainerType containerType)
	{
		this.Get(unitType, containerType).Clear();
	}

	/// <summary>
	/// Clear all containers
	/// </summary>
	public void ClearAllContainers()
	{
		foreach (Dictionary<uint, long> container in this.containers)
			container.Clear();
	}
}
