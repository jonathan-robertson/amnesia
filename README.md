# Amnesia [![Tested with A20.5 b2](https://img.shields.io/badge/A20.5%20b2-tested-blue.svg)](https://7daystodie.com/) [![Automated Release](https://github.com/jonathan-robertson/only-three-chances/actions/workflows/main.yml/badge.svg)](https://github.com/jonathan-robertson/only-three-chances/actions/workflows/main.yml)

*Reset player progress after a configurable number of deaths.*

## Features

After dying some configurable number of times, the player will experience `Memory Loss`. This will immediately reset "their progress" back to zero.

What "their progress" refers to is largely up to the admin. Several options are available to fine-tune the `Memory Loss` experience.

> :warning: NOTE: This mod is in progress; plans for many more options are being worked on

### Admin Controls

- view/adjust remaining lives per player
- adjust configs as follows:

Option | Default | Description
--- | :---: | ---
maxLives | 2 | how many lives players start with
warnAtLife | 1 | when to start warning players about amnesia
enablePositiveOutlook | true | whether to grant temporary buff that boosts xp growth after memory loss

## What is Reset?

### Supported

Value | Description
--- | ---
Player Level | Return to level 1
Assigned Skills | Reset All
Book Skills | Reset All
Unspent Skills | Reset to 0
Learned Recipes | Reset All
EXP To Next Level | Reset to 0
ExpDeficit | Reset to 0

### Planned

Value | Description
--- | ---
Variable Bag Drop | Drop nothing, bag, toolbelt, bag & toolbelt, or delete all on death

### Considering

Value | Description
--- | ---
Vehicles Owned | Forget all Vehicles
LCBs Owned | Forget & Deactivate all LCBs
Bed Owned | Clear Respawn Point
Map Explored | Reset map exploration
Map Waypoints | Clear all waypoints from map
Starting Quest | Give player starting quest
Quests | Forget all active quests and quest history
Trader Relationship | Reset all quest tier progress made with all traders
Vending Machines | Reset ownership for all
Score | Reset total zombies killed, players killed, deaths, and more
Party | Remove from active party
Allies | Remove all friends (2-way removal)
Name | Scramble player name following first death

### Not Considering

Value | Description
--- | ---
Mark Base on Map | If LCB is reset, the base will no longer be defended and can be raided by anyone online
Base Destroyed | Chunks containing player LCBs are reset
Vehicles Destroyed | Destroying all vehicles seems overboard
