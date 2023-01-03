# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2023-01-02

- add fragile memory state
- add retroactive refund for hardened memory
- remove non-time text from buffs for cleanliness
- remove references to lives in positive outlook
- update journal entry to reflect new flow
- update memory boosters to cure fragile memory
- update memory loss to start at ltm level
- update respawn window to reflect new flow
- update to add fragile memory on death
- update to lose memory if fragile on death

## [1.0.0] - 2022-12-23

- add admin configurable xp boosts for zombie kills
- add amnesia mod reminder to respawn screen
- add configurable enable/reset level floor
- modify amnesia buffs
- overhaul core mechanics
- refactor loops to enhance performance
- remove hardcoded xp boosts for zombie kills
- remove remaining lives mechanic
- remove second amnesia journal entry
- remove xp bonus for golden juggernaut
- rename smelling salts -> memory booster
- update journal entry
- update newbie coat to continue until amnesia start

## [0.9.4] - 2022-11-16

- fix colors in text

## [0.9.3] - 2022-10-15

- add extra journal notes explaining mechanics

## [0.9.2] - 2022-10-03

- add bonuses for scorcher and demo zombies

## [0.9.1] - 2022-09-26

- fix null reference exception onEntityKill handler

## [0.9.0] - 2022-09-26

- add new positive outlook trigger buffs for admins
- add xp boost when defeating juggernauts

## [0.8.1] - 2022-09-03

- fix post-bloodmoon grace period
- update so disabling pmdb also removes grace buff

## [0.8.0] - 2022-08-19

- add grace period after bloodmoon for cleanup
- fix tooltips for amnesia-related reminders
- update for a20.6

## [0.7.0] - 2022-08-05

- add remaining lives to amnesia status buffs
- add tooltip when life protection ends
- fix deploy script
- update smelling salts description

## [0.6.0] - 2022-08-01

- add journal entry
- add option to protect lives during bloodmoon
- add reference to memory protection config
- fix issue with warn-at-life not updating
- fix typos in readme and command help
- update readme action badges

## [0.5.1] - 2022-07-23

- finally update readme

## [0.5.0] - 2022-07-22

- add test command for admins to confirm settings
- add options for resetting books
- add options for resetting schematics
- add options for resetting kdr
- fix missing config options exception

## [0.4.1] - 2022-07-21

- fix initial load in brand new map

## [0.4.0] - 2022-07-21

- update config file to amnesia.xml
- add more admin options
- add support for quest reset (buggy)
- disable loss of life when dying to pvp
- remove player kill/death stats on mem loss
- update positive outlook to double xp

## [0.3.0] - 2022-07-02

- add mechanic to recover lives

## [0.2.1] - 2022-07-01

- fix readme link, retry release pipeline

## [0.2.0] - 2022-07-01

- add buffs to remind/warn of remaining lives
- add xp boost to encourage recently reset players

## [0.1.0] - 2022-06-30

- add method to support existing games
- add logic to give 3 lives
- add loss of 1 life per death
- add mechanic to wipe character on final death
