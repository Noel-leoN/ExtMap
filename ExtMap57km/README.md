# Cities Skylines 2 Extended Map Mod (Beta)

## Introduction

- 57km^2 Extended Map

## Requirements

- Game version 1.1.5f1.
- BepInEx 5.4.21

## Install

- install BepInEx 5.4.21 to you game root, run the game once then exit.
- put ExtMap.PDX to your game local pdx mod folder.
- put ExtMap.Patch to you BepInEx\patchers folder.

## Usage

- create map in game editor by import 57.344km heightmap and 229.376km worldmap.

## Compatibility

- Modifies:
  - most of the game systems which cellmapsystem is referenced.
  - maptile system\areatool system.
  - water system.
- Most simulation systems use the postfix/updateafter method, which generally does not encounter serious conflicts.

## Changelog

- 1.0.5
  - Compatibility with game version 1.1.5f1.
  - fix some simulation systems.

- 1.0.0
  - Original version.  
  
## Notes
 - Bugs with all vanilla maps, and you have to use a custom 57km*57km map.

## Issues
 - some simulation systems may not be working properly.If you found issues please report in github,thank you.

## Disclaimer

- it's experimental. SAVE YOUR GAME before use this. Please use at your own risk.

## Credits

- [Captain-Of-Coit](https://github.com/Captain-Of-Coit/cities-skylines-2-mod-template): A Cities: Skylines 2 mod template.
- [BepInEx](https://github.com/BepInEx/BepInEx): Unity / XNA game patcher and plugin framework.
- [Harmony](https://github.com/pardeike/Harmony): A library for patching, replacing and decorating .NET and Mono methods during runtime.
- [CSLBBS](https://www.cslbbs.net): A chinese Cities: Skylines 2 community.
- [Discord](https://discord.gg/ABrJqdZJNE): Cities 2 Modding
