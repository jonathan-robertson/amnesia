# Amnesia [![Tested with A20.5 b2](https://img.shields.io/badge/A20.5%20b2-tested-blue.svg)](https://7daystodie.com/) [![Automated Release](https://github.com/jonathan-robertson/amnesia/actions/workflows/main.yml/badge.svg)](https://github.com/jonathan-robertson/amnesia/actions/workflows/main.yml)

![amnesia social image](https://github.com/jonathan-robertson/amnesia/raw/media/amnesia-logo-social.jpg)

## Summary

This mod introduces a new player stat called Lives. After the provided number of lives are lost, the player is reset in some configurable way (see [Admin Configuration](#admin-configuration)). Lives can be restored with the help of `Smelling Salts` (non-craftable, but often purchasable from traders for a very high price... i.e. it requires work).

The Goal: improve long-term engagement for various kinds of players and take a step toward 'evergreen' servers by reducing how often map wipes need to occur.

The Target: players are severely turned off by the requirement for client-side mods, so **this mod is entirely server-side and requires no client-side downloads**.

> :mag: If you're only looking for a client-side experience, consider [Mostly Dead](https://7daystodiemods.com/mostly-dead/) by [Khzmusik](https://7daystodiemods.com/tag/khzmusik/).

The Effect: support a heavier risk/reward play style meant to be a balance for servers with a higher XP boost. Fast player growth with the risk of dynamic, configurable player reset.

> :thought_balloon: Regular xp growth can be used as well for a more hardcore experience. Not sure this would attract as many players, though...

### Compatibility

Type | Compatible | Details
:---: | :---: | ---
Local | No | This is a server-side mod not meant for local play. If you're looking for an experience similar to this but for a local game, consider checking out [Khzmusik](https://7daystodiemods.com/tag/khzmusik/)'s [Mostly Dead](https://7daystodiemods.com/mostly-dead/) modlet.
P2P | No | Locally hosting a multiplayer game is not supported
Dedicated | Yes | This mod is designed for dedicated server games. You can install 7 Days to Die server locally and configure a server on your own game box to run both the server and your 7DTD client, you'd just need to connect from your client. I might support P2P and Local games in the future, but they would require EAC to be disabled and I'm more focused on EAC-Enabled server-side mods.

### Philosophy

I want for there to be a way to keep maps/players in a 'healthy' state of play for 6 months or beyond - perhaps even getting to the point where maps can be evergreen: keeping a map active until the following 7 Days to Die Alpha Release (barring patches that The Fun Pimps determine will require map wipes).

As an 7DTD admin and modder for a few years now, I've noticed some problematic patterns and this mod was created to take a stab at addressing some of them.

> :bar_chart: Progression is a core piece of any RPG. Once the opportunity to progress has faded or become convoluted, player engagement/enjoyment evaporates with time.

Observation | Problem | How Amnesia attempts to address it
--- | --- | ---
Many admins wipe maps within 1-2 months - often for reasons related to map corruption (not addressed in this mod) or player balance. | New players are often discouraged the steep level difference between them and others who started earlier while more casual players are discouraged from the raging progress of highly engaged players. | Dynamic rebalancing of the risk/reward structure that directly impacts player progress offers a new challenge for engaged players and rewards the more cautious play style of casuals. It also provides hope for the new players that if they play carefully, they can grow in level and meet other more aggressive players where they are. More details about each of these player types in subsequent rows.
Wasteland GameStage changes are... game breaking: Level 50 players in Wasteland will be looting end-game, max level gear. | Wasteland is really cool and I love the changes TFP made, but dying does not currently provide a significant deterrent to acquiring all the end-game gear. | There is NO REASON to avoid Wasteland since it's a treasure-trove. Rather than nerfing Wasteland, I find it much more fulfilling to provide a meaningful but not debilitating drawback to death: dynamic, configurable player state reset. Additional suggestions are to gate the Wasteland behind instant-death radiation without a full rad suit similar to Darkness Falls (see standalone mod [Radiation Wasteland – Server Side](https://7daystodiemods.com/radiation-wasteland-server-side/)) or add an environmental effect to limit time spent in the Wasteland (see [Immersive Radiation for the Wasteland](https://7daystodiemods.com/immersive-radiation-for-the-wasteland/)).
Highly Engaged Players log in regularly and level up quickly. | These same players burn out of maps quickly and become disengaged. | Amnesia rewards the risky behavior and thrill-seeking growth that Highly Engaged Players pursue and meets them with a greater challenge to avoid dying. Dying enough will reset their levels (and more, if configured), but will also encourage them with a one-time hour-long XP boost to encourage further engagement.
New/Casual/Cautious Players have a hard time feeling like they can't catch up to others. | This discourages new players from sticking with a server community for long since they don't have time time to invest in keeping up with highly engaged players and encourages. So they leave for another server that's more "fresh" or they leave 7DTD entirely. | Amnesia supports variable progression that can be tuned by the admin. Level and KDR stat resets help to keep these players feeling like they have the opportunity to advance in meaningful ways. Cautious players will simply die less often since they're more risk adverse. While their growth will be slower, they will maintain their growth more often.

> :warning: *I will admit Amnesia is not the kind of thing I expect all players will prefer. Keep this in mind and discuss with your community if this is something they're comfortable trying out. I'd encourage an open dialog about options/settings so there are no unpleasant surprises or unnecessary drama when player characters eventually suffer memory loss.*

## Admin Configuration

Option Name | Default | Description
--- | :---: | ---
MaxLives | 2 | The number of lives a player can lose before being reset. Also represents the maximum cap that a player cannot exceed even with `Smelling Salts`. Reducing this number will reduce remaining lives for all players only if remaining lives are below the new max. Increasing this number will also increase remaining lives for all players by the difference between the old max lives and new max lives. (these adjustments take effect even for players who are offline; current lives changes will be applied on next login).
WarnAtLife | 1 | The level at which the player receives a casual reminder buff to let them know they may want to pursue `Smelling Salts` and play a little more carefully to avoid losing more lives. Set this to `0` to disable this warning reminder. Also note that with `0` remaining lives, players will always receive a special warning buff that cannot be disabled.
EnablePositiveOutlook | true | Whether to grant temporary buff that boosts xp growth at initial server join and on memory loss.
ForgetLevelsAndSkills | true | Whether to forget levels, skills, and skill points on memory loss.
ForgetBooks | false | Whether books should be forgotten on memory loss. It's recommended to keep this as `false` because A21 is expected to have hundreds of books to collect for crafting purposes.
ForgetSchematics | false | Whether schematics should be forgotten on memory loss. *It's recommended to keep this as `false` because A21 is expected to no longer grant crafting recipes when learning skills, so finding/using schematics will be the only way to learn how to craft things.* ***Note that `false` can cause some confusion in A20 because schematics will appear to have been read if the corresponding recipe was already unlocked in a Skill/Perk. The code for how this works is inside C# on the client's end, so changing it for a server-side mod does nto appear to be possible... but again, all the confusion will be gone in A21.***
ForgetKDR | false | Whether players/zombies killed and times died should be forgotten on memory loss. *I'd strongly recommend setting this to `true`, but have left it as `false` by default only because these metrics can't be recovered once wiped and some admins might not want them to reset for that reason.*

> :pencil: This mod is in progress, so plans for many more options are being worked on. As they're added and admins update their servers, the default values will be added for new options without negatively impacting the older options admins have already set.

### Experimental/Volatile Options

> :warning: THESE OPTIONS WILL DISCONNECT THE PLAYER FROM THE SERVER ON FINAL DEATH. THEY ARE TO BE CONSIDERED EXPERIMENTAL AND MAY NOT WORK EXACTLY AS YOU'D LIKE.

The reason for the disconnection requirement has to do with Quests currently being managed in an isolated way on the client - not the server. For me to adjust quests for a player, it's necessary to disconnect that player and manipulate the PlayerDataFile (player save) while the player is offline.

> :thought_balloon: I personally feel that this is a gross solution, but it does actually work. Hopefully A21 will provide new NetPackages to allow the server to communicate quest adjustments back to the client in realtime so disconnecting will no longer be necessary.

Option Name | Default | Description
--- | :---: | ---
ForgetActiveQuests | false | Whether ongoing quests should be forgotten on memory loss.
ForgetInactiveQuests | false | Whether completed quests (AND TRADER TIER LEVELS) should be forgotten on memory loss.
ForgetIntroQuests | false | Whether the intro quests should be forgotten/reset on memory loss.

## Attribution

- Much thanks to [all-free-download.com](https://all-free-download.com/free-vector/download/brain_icon_shiny_dark_blue_design_6833698.html) for the brain image used in this logo's background.
