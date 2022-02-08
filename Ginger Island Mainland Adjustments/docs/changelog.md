﻿Changelog
===================

[← back to readme](../../README.md)

#### Todo

1. Make groups more meaningful.
2. Nonreplacing dialogue for the Resort keys? (flatten the ones that already exist, but add a way to add more that's less likely to clobber).
3. Handle roommates, like actually.
4. See if NPCs can go *into* Professor Snail's Tent?
<!-- Move this mod's scheduler earlier so I can add in CP tokens. (so OnDayStarted or before?). Sadly, this is not feasible because CustomNPCExclusions expects the island schedules to be generated *after* CP is done updating tokens, and I would need to move it *before*. Would be a compat nightmare. see: https://github.com/Esca-MMC/CustomNPCExclusions/blob/master/CustomNPCExclusions/HarmonyPatch_IslandVisit.cs -->
<!-- Finish the locations console command: https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences to add bold -->
<!-- More schedule debugging tools: get arbitrary schedule from X day? -->
<!-- Telephone: Lock behind a 10 heart event or something. Move phone dialogue to go through Strings/Characters -->
<!-- Figure out why Emily dances *in* the changing room? -->
<!-- IslandNorth and AntiSocial lines for George/Evelyn/Willy-->
<!-- ATTENTION: REMOVE THE #if DEBUG letting children go to the resort! -->

##### Known Issues

1. NPCs may vanish if they go to `IslandSouthEast`. They reappear the next day. Therefore, that's been tempoarily removed until I figure out why they disappear from `IslandSouthEast`. `IslandNorth` is fine.
2. If you pause time, NPCs will tend to get stuck at schedule points. Unfortunately for Ginger Island, this usually ends with NPCs trapped in the changing room. If you go to Ginger Island and see no one there, try unpausing time. Or just leave them trapped in the changing room....

### Version 1.1.3
 
* In theory: integration with Child2NPC.
* You can now call Pam, who will tell you if she's headed to Ginger Island.

### Version 1.1.2

* Adds Willy as a possible resort attendee.
* Adds fishing as a possible resort activity. (Updated content pack - Pam should now be able to fish).
* Removed children going to the resort when not with a group. Added back in the Penny/Vincent/Jas group I accidentally left out....
* Add in a way to exclude characters from visiting the island alone: they'll be able to go as a group or not at all. (Thanks for the suggestion, tiakall#4802!)
* Fix issue where spouses were not using the married schedules.

Internal
* Fixed up the console command for listing `NPC.routesFromLocationToLocation`.

### Version 1.1.1

* Fix issue where spouses would say their `GILeave` lines a day after they were supposed to.

### Version 1.1.0

* Initial upload.