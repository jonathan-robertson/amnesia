# Amnesia [![Tested with A20.5 b2](https://img.shields.io/badge/A20.5%20b2-tested-blue.svg)](https://7daystodie.com/) [![Automated Release](https://github.com/jonathan-robertson/amnesia/actions/workflows/main.yml/badge.svg)](https://github.com/jonathan-robertson/amnesia/actions/workflows/main.yml)

![amnesia social image](https://github.com/jonathan-robertson/amnesia/raw/media/amnesia-logo-social.jpg)

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

Value | Description
--- | ---
Player Level | Return to level 1
Assigned Skills | Reset All
Unspent Skill Points | Reset to 0
Learned Books | Reset All
Learned Recipes | Reset All
EXP To Next Level | Reset to 0
ExpDeficit | Reset to 0
Score | Reset total zombies killed, players killed, deaths, and more
Quests | Forget all active quests and quest history
Trader Relationship | Reset all quest tier progress made with all traders; this is a calculated value based on completed quests
Starting Quest | Give player starting quest

### What *Isn't* Reset?

Value | Description
--- | ---
Bag | Your server determines what to do with items on death in the same way it always does
Map Explored | You'll remember your map
Map Waypoints | You'll remember your waypoints
Vehicles Owned | Vehicles not forgotten
LCBs Owned | You'll remember all waypoints you currently posses
Bed Owned | Your respawn point will remain
Vending Machines | Any vending machines you own or have rented will not be forgotten
Party | Removing from a party will not happen
Allies | You would never forget your friends
Name | Nah. Everyone knows you by your name
Mark Base on Map | We will not shout out your base location to players upon your final death
Base Destroyed | We will not delete your base - you worked hard for that!
Vehicles Destroyed | Losing all your stats is hard enough - removing your vehicle would be a step too far

## Attribution

- Much thanks to [all-free-download.com](https://all-free-download.com/free-vector/download/brain_icon_shiny_dark_blue_design_6833698.html) for the brain image used in this logo's background.
