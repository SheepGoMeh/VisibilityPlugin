using System.Collections.Generic;

namespace Visibility.Api;

public interface IVisibilityApi
{
	public int ApiVersion { get; }

	/// <summary>
	/// Fetch all entries from VoidList
	/// </summary>
	/// <returns>A collection of strings in the form of (name worldId reason)</returns>
	public IEnumerable<string> GetVoidListEntries();

	/// <summary>
	/// Adds entry to VoidList
	/// </summary>
	/// <param name="name">Full player name</param>
	/// <param name="worldId">World ID</param>
	/// <param name="reason">Reason for adding</param>
	public void AddToVoidList(string name, uint worldId, string reason);

	/// <summary>
	/// Removes entry from VoidList
	/// </summary>
	/// <param name="name">Full player name</param>
	/// <param name="worldId">World ID</param>
	public void RemoveFromVoidList(string name, uint worldId);

	/// <summary>
	/// Fetch all entries from Whitelist
	/// </summary>
	/// <returns>A collection of strings in the form of (name worldId reason)</returns>
	public IEnumerable<string> GetWhitelistEntries();

	/// <summary>
	/// Adds entry to Whitelist
	/// </summary>
	/// <param name="name">Full player name</param>
	/// <param name="worldId">World ID</param>
	/// <param name="reason">Reason for adding</param>
	public void AddToWhitelist(string name, uint worldId, string reason);

	/// <summary>
	/// Removes entry from Whitelist
	/// </summary>
	/// <param name="name">Full player name</param>
	/// <param name="worldId">World ID</param>
	public void RemoveFromWhitelist(string name, uint worldId);

	/// <summary>
	/// Enables or disables Visibility
	/// </summary>
	/// <param name="state">Enabled = true</param>
	public void Enable(bool state);
}
