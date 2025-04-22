using System;
using System.Collections.Generic;
using System.Linq;

namespace Visibility.Utils;

/// <summary>
/// Manages containers for different unit types and their relationships
/// </summary>
public class ContainerManager
{
	private readonly Dictionary<UnitType, Dictionary<ContainerType, Dictionary<uint, long>>> containers;
	private readonly HashSet<uint> idsToDelete = new(capacity: 200);

	public ContainerManager()
	{
		this.containers = new Dictionary<UnitType, Dictionary<ContainerType, Dictionary<uint, long>>>
		{
			{
				UnitType.Players, new Dictionary<ContainerType, Dictionary<uint, long>>
				{
					{ ContainerType.All, new Dictionary<uint, long>() },
					{ ContainerType.Friend, new Dictionary<uint, long>() },
					{ ContainerType.Party, new Dictionary<uint, long>() },
					{ ContainerType.Company, new Dictionary<uint, long>() },
				}
			},
			{
				UnitType.Pets, new Dictionary<ContainerType, Dictionary<uint, long>>
				{
					{ ContainerType.All, new Dictionary<uint, long>() },
					{ ContainerType.Friend, new Dictionary<uint, long>() },
					{ ContainerType.Party, new Dictionary<uint, long>() },
					{ ContainerType.Company, new Dictionary<uint, long>() },
				}
			},
			{
				UnitType.Chocobos, new Dictionary<ContainerType, Dictionary<uint, long>>
				{
					{ ContainerType.All, new Dictionary<uint, long>() },
					{ ContainerType.Friend, new Dictionary<uint, long>() },
					{ ContainerType.Party, new Dictionary<uint, long>() },
					{ ContainerType.Company, new Dictionary<uint, long>() },
				}
			},
			{
				UnitType.Minions, new Dictionary<ContainerType, Dictionary<uint, long>>
				{
					{ ContainerType.All, new Dictionary<uint, long>() },
					{ ContainerType.Friend, new Dictionary<uint, long>() },
					{ ContainerType.Party, new Dictionary<uint, long>() },
					{ ContainerType.Company, new Dictionary<uint, long>() },
				}
			},
		};
	}

	/// <summary>
	/// Add an entity to a specific container
	/// </summary>
	public void AddToContainer(UnitType unitType, ContainerType containerType, uint entityId)
	{
		this.containers[unitType][containerType][entityId] = Environment.TickCount64;
	}

	/// <summary>
	/// Remove an entity from a specific container
	/// </summary>
	public void RemoveFromContainer(UnitType unitType, ContainerType containerType, uint entityId)
	{
		this.containers[unitType][containerType].Remove(entityId);
	}

	/// <summary>
	/// Check if an entity exists in a specific container
	/// </summary>
	public bool IsInContainer(UnitType unitType, ContainerType containerType, uint entityId)
	{
		return this.containers[unitType][containerType].ContainsKey(entityId);
	}

	/// <summary>
	/// Cleanup expired entries from all containers
	/// </summary>
	public void CleanupContainers()
	{
		foreach ((UnitType _, Dictionary<ContainerType, Dictionary<uint, long>>? unitContainer) in this.containers)
		{
			foreach ((ContainerType _, Dictionary<uint, long>? container) in unitContainer)
			{
				foreach ((uint id, long ticks) in container)
					if (ticks > Environment.TickCount64 + 5000)
						this.idsToDelete.Add(id);

				foreach (uint id in this.idsToDelete) container.Remove(id);

				this.idsToDelete.Clear();
			}
		}
	}

	/// <summary>
	/// Get all entities in a specific container
	/// </summary>
	public IEnumerable<KeyValuePair<uint, long>> GetContainerEntities(UnitType unitType, ContainerType containerType)
	{
		return this.containers[unitType][containerType].ToList();
	}

	/// <summary>
	/// Clear a specific container
	/// </summary>
	public void ClearContainer(UnitType unitType, ContainerType containerType)
	{
		this.containers[unitType][containerType].Clear();
	}

	/// <summary>
	/// Clear all containers
	/// </summary>
	public void ClearAllContainers()
	{
		foreach ((UnitType _, Dictionary<ContainerType, Dictionary<uint, long>>? unitContainer) in this.containers)
		foreach ((ContainerType _, Dictionary<uint, long>? container) in unitContainer)
			container.Clear();
	}
}
