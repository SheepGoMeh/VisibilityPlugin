using System;

namespace Visibility.Configuration;

public class TerritoryConfig
{
	[NonSerialized] public ushort TerritoryType;
	public bool HidePet;
	public bool HidePlayer;
	public bool HideMinion;
	public bool HideChocobo;
	public bool HidePetInCombat;
	public bool HidePlayerInCombat;
	public bool HideMinionInCombat;
	public bool HideChocoboInCombat;
	public bool ShowCompanyPet;
	public bool ShowCompanyPlayer;
	public bool ShowCompanyMinion;
	public bool ShowCompanyChocobo;
	public bool ShowPartyPet;
	public bool ShowPartyPlayer;
	public bool ShowPartyMinion;
	public bool ShowPartyChocobo;
	public bool ShowFriendPet;
	public bool ShowFriendPlayer;
	public bool ShowFriendMinion;
	public bool ShowFriendChocobo;
	public bool ShowDeadPlayer;

	public TerritoryConfig Clone() => (TerritoryConfig)this.MemberwiseClone();
}
