using System.Collections.Generic;

namespace Visibility.Utils;

public class UnitContainer
{
	public readonly HashSet<long> AllUnitIds = [];
	public readonly HashSet<long> FriendUnitIds = [];
	public readonly HashSet<long> PartyUnitIds = [];
	public readonly HashSet<long> CompanyUnitIds = [];

	public void ClearAllUnitIds()
	{
		this.AllUnitIds.Clear();
		this.FriendUnitIds.Clear();
		this.PartyUnitIds.Clear();
		this.CompanyUnitIds.Clear();
	}
}
