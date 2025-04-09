# Changelog

## [1.1.8.3] (2025-03-28)
* Updated plugin for 7.2
* API 12

## [1.1.8.2] (2024-11-15)
* Updated plugin for 7.1

## [1.1.8.1] (2024-10-27)
* Fixes a loading error with `VoidList` and `Whitelist`

## [1.1.8.0] (2024-09-30)
* Updated VoidList to support account tracking (You will not be able to see character name changes or account information)
* Updated Whitelist to track characters by their ID

## [1.1.7.5] (2024-08-08)
* Added a feature to show target of target when hidden by other settings

## [1.1.7.4] (2024-08-07)
* Fixes incorrect check for names in VoidList and Whitelist

## [1.1.7.3] (2024-07-24)
* Fixed FC and VoidList/Whitelist string comparison which led to them not functioning

## [1.1.7.2] (2024-07-24)
* Fixed FC check for local player

## [1.1.7.1] (2024-07-05)
* Support Dawntrail (Thanks @Infiziert90)
* Removed ContextMenu support until further notice
* API 10

## [1.1.6.0] (2023-10-04)
* API 9

## [1.1.5.1] (2023-07-05)
* Fix Whitelist UI adding entries to VoidList

## [1.1.5.0] (2023-07-02)
* Refactored UI code to address disappearing menu issues

## [1.1.4.10] (2023-06-01)
* Replace `ContainsKey` with `TryGetValue` to avoid accessing `itemsDictionary` twice
* Rework TerritoryTypeWhitelist and populate it from `Lumina.Excel.GeneratedSheets` by using `TerritoryIntendedUse`

## [1.1.4.9] (2023-05-25)
* Change friend checks
* Remove unused code

## [1.1.4.8] (2023-01-22)
* Fix bound by duty check to exclude the treasure hunt duty

## [1.1.4.7] (2023-01-14)
* Added Sylphstep to the TerritoryTypeWhitelist

## [1.1.4.6] (2023-01-10)
* Update to net7

## [1.1.4.5] (2023-01-07)
* Improve performance
* Change how VoidList and Whitelist lookup works in frameworkHandler and add RemoveChecked method and ShowPlayer method supporting name lookup in ObjectTable
* Add a Disable flag check to frameworkHandler
* Add missing ShowPlayer method call to DrawVoidList
* Remove counter hack
* Move MinionHandler to PlayerHandler as minions will not be shown when the attached player is hidden
* Introduce ShowGameObject and handle minions in HideGameObject
* Fix incorrect container in ShowAll and stop setting RenderFlag
* Add support for bard performance mode

## [1.1.4.0] (2022-11-04)
* Fix own player being hidden in cutscenes
* Removed dependency on SigScanner and moved code to be handled on framework update

## [1.1.3.7] (2022-10-20)
* Make VoidList function regardless of the Enabled state
* Use NamePlate widget instead of NowLoading widget to figure out if objects should be hidden

## [1.1.3.6] (2022-06-23)
* Fix incorrect boolean assignment
* Remove private accessor from Language property to allow loading from configuration file

## [1.1.3.5] (2022-06-12)
* Change dead player detection
* Make settings code less hacky
* Update to net6

## [1.1.3.3] (2022-07-20)
* Prevent hiding characters while using Duty recorder playback

## [1.1.3.2] (2022-07-19)
* Improve friend detection
* Reintroduce context menus
* Change HideStar to a global setting

## [1.1.3.0] (2022-07-11)
* Change how cross world party objectid is detected and unify with groupmanager into a single method
* Fix party member detection for CharacterDisableDrawDetour
* Move DrawVoidList and DrawWhitelist into their own files
* Implement configuration per territory
* Add Chocobo Square and Gold Saucer to the list of allowedTerritory
* Add currentEditedConfig to not affect local settings while editing other area configs
* Add localization for new options

## [1.1.2.1] (2022-07-07)
* Remove context menus until further notice
* Change how voided player logic is handled and prevent hiding pets inside duties
* Improve party member detection and support cross-world parties

## [1.1.1.5] (2022-04-15)
* Remove XivCommon and ILRepack dependencies and switch to Dalamud ContextMenu
* Update signatures
* Update DalamudPackager to 2.1.6 and remove special packing step
* API 6

## [1.1.1.4] (2022-01-01)
* Make sure condition WatchingCutscene is not set when cleaning up
* Fix up relevant StatusFlags

## [1.1.1.3] (2021-12-29)
* IPC Implementation
* Hack in a fix for character disappearing in cutscenes
* Use NowLoading widget instead of FadeMiddle to address #18

## [1.1.1.0] (2021-09-09)
* Add localization
* Add visibility icon
* API 4

## [1.1.0.0] (2021-09-09)
* Change project to SDK style
* Change TargetFramework to net5.0

## [1.0.4.2] (2021-06-09)
* Completely remove checking for ActorId in favor of Name and Homeworld
* Call ImGui.End() even if ImGui.Begin() returns false

## [1.0.4.1] (2021-05-31)
* Disable while viewing cutscenes
* Add DalamudPackager package
* Rename _contextMenu to PluginContextMenu and make it public
* Add ability to toggle context menu functionality

## [1.0.4.0] (2021-05-18)
* Change framework version to 4.8
* Refactor variable names, add ContextMenu (XivCommon) and add emote filtering to VoidList message filter

## [1.0.3.5] (2021-05-07)
* Fix incorrect command names for whitelist
* Allow hiding players in ocean fishing
* Allow hiding players in The Diadem

## [1.0.3.4] (2021-04-27)
* Unhide everything when plugin gets disabled

## [1.0.3.3] (2021-04-19)
* Simplify refresh check
* Add refresh command
* Rework voidlist and whitelist interface to use sortable tables

## [1.0.3.2] (2021-04-16)
* Only clear buffer array when target is a PlayerCharacter
* Rework configuration interface
* Add setting to toggle operation of plugin

## [1.0.3.1] (2021-04-14)
* Rework Refresh functionality

## [1.0.3.0] (2021-04-14)
* Change UnsafeArrayEqual to require a single length value
* Stop checking ActorId and use Name and Homeworld instead for VoidItem
* Use payloads to handle messages correctly
* Discard unused code
* Use reflection to get automatic backing field
* Use IconPayload instead of raw bytes
* Introduce option to hide Earthly Star in combat
* Use LocalPlayer instead of `Actors[0]`
* Add Whitelist functionality
* API 3

## [1.0.2.7] (2021-01-12)
* Implemented signatures

## [1.0.1.2] (2020-09-30)
* Changed timer to 20ms
* Reduced error logging for Release build
* Added icons to chat for `/voidplayer` and `/voidtargetplayer`
* Corrected datacenter check for `/voidplayer`

## [1.0.1.1] (2020-09-20)
* Added a whitelist for areas like Leap of Faith
* Added an event to show configuration
* Reduced performance impact
* Added a check to prevent hiding Earthly Star
* Added a check to prevent duplicate entries of worlds when manually adding a name to the VoidList

## [1.0.1.0] (2020-09-14)
* Removed the ability to hide minions individually

## [1.0.0.9] (2020-09-12)
* Address a null referencing bug by adding a proper null check for the reason argument in `/void` command
* Added own player's actor to the list of actors to be refreshed when pressing `Refresh` option
* Added a check to verify that the player's own actor is not in the collection of actors to be modified
* Added the option to filter hidden actors by free company

## [1.0.0.8] (2020-09-11)
* Added a check to make sure player is not bound by duty before hiding other players
* Removed space checking for Reason input box
* Made VoidList button open or close based on whether or not the window is visible

## [1.0.0.7] (2020-09-11)
* Add a check to make sure that player is not bound by duty before hiding other players

## [1.0.0.6] (2020-09-11)
* Fix incorrect order in PlaceholderResolver Init method
* Change friends collection to Dictionary for faster lookup
* Move expression outside of predicate for better readability and usability
* Change friends to HashSet and remove partyMembers collection in favor of accessing _partyActorId.Contains instead.

## [1.0.0.0] (2020-09-10)
* Initial release