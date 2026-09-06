# v1.7.1

- fix: adjust spectator count display anchored offset so it doesn't overlap with warden timer text.
- fix: PlayerStatus health bar damage animation now resets when entering spectate
- docs: adjust README known issues to reflect current state of the mod.

# v1.7.0

- feat: spectator count display
- feat: Simplified and Traditional Chinese localization

# v1.6.4

- feat: add more details to logs; adjust logging levels.
- feat: add check for IsPlayerInHub for player update outside of error case.

# v1.6.3

- feat: align ammo display to be correct wrt v1.1.1 of PlayerSync where reserve ammo includes clip ammo for non-local players.

# v1.6.2

- fix: some issues caused by reflection with UE

# v1.6.1

- Re-release v1.6.0 with release build.

# v1.6.0

- Add networking
    - Peer version awareness and feature negotiation
- Added an icon indicating Spectate support on other players in the lobby screen.
- Clip size is now displayed for supporting players.
- Stamina BPM is now representative of the spectating player for supporting clients.
- Add dependency to [PlayerSync](https://thunderstore.io/c/gtfo/p/food/PlayerSync/) to allow for stamina and better ammo information/updates when spectating players.
- Add dependency to [Clonesoft Json](https://thunderstore.io/c/gtfo/p/AuriRex/Clonesoft_Json/) for JSON support (with attributes).
- Added compatibility with [ExcellentObjectiveSetup](https://thunderstore.io/c/gtfo/p/Amorously/ExcellentObjectiveSetup/).
    - `PUI_Inventory` display state no longer interferes.
- Added an option to apply compatibility patches with direct reflection instead of Harmony. Switched compatibility patches all to Harmony instead of reflection by default.

# v1.5.13

- Added compatibility with [EEC](https://thunderstore.io/c/gtfo/p/hirnukuono/EEC_H/)
    - Inventory display state no longer interferes.

# v1.5.12

- Update PlayerStatus booster display for spectating targets.
- Clear visor liquids on spectate enter.

# v1.5.11

- Now respects if player inventory UI is enabled in vanilla settings.

*Known Issues*

- Players do not prevent respawn rooms from respawning when spectating (i.e. their in-game position is effectively
  whomever they are spectating).
- Bots, a lot of times, do not shoot their weapon when the player is spectating.

# v1.5.10

- Fix what `v1.5.9` should have fixed.
- Added some additional checks to prevent weird null cases which cause exceptions

# v1.5.9

- Fixed an issue on interaction with checkpoints: player status and inventory might not be properly reset after
  restarting from checkpoint.
    - This seems to only happen sometimes with previous versions.

*Special Notes*

- Thanks to Sp3ctre(@sp3c7re_) for reporting and providing details on a case inventory issue.

# v1.5.8

- Fix a bug causing players to be unable to spectate even when they should
    - This was caused my stupidity of checking whether player is captured by depending on a state which is only
      guaranteed valid if the player is spectating.

# v1.5.7

- Spectates the capturing snatcher if the spectated player is snatched by a Pouncer.
- Spectate now select the closest player to the local player on first spectate switch. Resets after revive.

# v1.5.6

- Implemented a safeguard so that players cannot spectate when they are snatched by a Pouncer (i.e. they are in the
  Pouncer dimension)
    - This prevents a bug of the player entering a state where enemies cannot detect the player and teammates cannot see
      the player model or pointer on map.
    - This is not a fix of the root cause, but is good enough for now.

*Special Notes*

- This issue was discovered by Lakana(@lakana0451); thanks to them for providing details.
- Thanks to Nivoe(@pssucks) and Sp3ctre(@sp3c7re_) for help with testing.

# v1.5.5

- Refactored some logic to be more robust
- Fixed a bug where under certain conditions the game would crash with a native pointer exception on
  `PUI_LocalPlayerStatus.SetDamageAnim`.

*Special Notes*

- Thanks to Nikita(@nikita_2414) for finding a reproducible scenario for the native crash on
  `PUI_LocalPlayerStatus.SetDamageAnim`.

# v1.5.4

- Fixed vanilla player pings when spectating.
- Fixed UI updating artifacts for infection.
- Improved health UI animation logic.
- Support for auto centering map to spectating player.
    - Respects vanilla setting: \[HUD\] >> \[Auto Center Map on Player\]

# v1.5.3

- Add preference in spectating alive players (enabled by default, can be disabled in the config), which:
    - Tries to switch to an alive player when currently spectating player is downed.
    - Makes LMB/RMB next/prev player switching keybinds switch only between alive players.
- Add compatibility with [EOSExt EMP](https://thunderstore.io/c/gtfo/p/Inas07/EOSExt_EMP/)
    - EOSExt EMP tries to set the state of the player inventory ui every frame, causing spectate UI management to fail.
    - The patch disables the EOSExt EMP's player UI update when spectate is active.

# v1.5.2

- Fixed UI state desync issues.

# v1.5.1

- Fixed a bug where spectate inventory ui persists across game sessions if the session wipes while spectating.

# v1.5.0

- Added full spectate inventory display
- Fixed a bug where immediately after exiting spectate, the player sees void (incorrect cull node setting).
- Fixed a bug where player body is visible in FPS downed view for modded character models.
- Adjusted behavior so that pressing number keys while the comms menu is active no longer switches players.

*Known Issues*

1. Player pings are broken when spectating players.
    - i.e. pings are emitted relative to the local player position rather than the spectating camera
    - **Current Solution**: Use [PingEverything](https://thunderstore.io/c/gtfo/p/Localia/PingEverything/)
2. Spectated players don't display clip size
    - Per limitations of the game, clip size are not synced across clients, so this is currently not possible.
    - There is a method of possibly estimating the current clip size, this is wip and still being tested.

*Special Notes*

- Thanks to the following individuals for help with testing and/or discussing possible solutions.
    - randomuserhi(@randomuserhi)
    - JarheadHME(@jarheadhme)
    - Microchips(@microchipsndip)
    - BlueGuy(@blue_guy_or_something_idk)

# v1.4.3

- Add a config option to hide the version watermark.

# v1.4.2

- Fixed a bug where player downed/revived messages are broadcasted twice in chat.
- Fixed/adjusted related animations during down/revive

# v1.4.1

- Added a thing :P

# v1.4.0

- Now shows the local player's model in-game (can be disabled in the config).
- Add a beacon to mark the local player's position (can be disabled in the config).
- Players now exit spectate the instant they are revived, rather than until interactable.

# v1.3.0

- Add the ability to customize certain keybinds in the config file.
- Fixed a bug where spectate UI will inherit the FOV before the player is downed.
    - FOV during spectate now respects FOV value defined in game settings and updates dynamically.
    - e.g. If the player gets downed while ADSing, which has a lower FOV, spectate will have the same narrow FOV.

# v1.2.1

- Fixed a bug where checkpoints would break spectate.

# v1.2.0

- Added a keybind `T` to toggle auto transition to temporary follow view in Freecam mode. Note that changes in game to
  this option will be reflected in the config.
- Added an option in the config to disable lerping when switching spectated players in Freecam mode.
- Follow mode no longer lerps position when switching targets.
- Fixed a bug where player hands might be invisible when exiting spectate.
- Fixed a bug where player pings when spectating are misaligned and broken.

# v1.1.1

- Add player position smoothing as well as config options to adjust the smoothing rate.
- Fixed a bug where player helmet flashlight is still shown when spectating.
- Adjust some configuration value defaults and limits.
- Adjust positioning of UI shadow for better visibility.

# v1.1.0

- Rewrote UI management to support more behaviors.
    - Auto spectate on down is now false by default.
    - Text now shows the option to spectate when in FPS downed state.
- Fixed a bug where cull nodes are not checked against null when detaching camera.

# v1.0.3

- Fixed a bug where auto transition to temporary follow view in Freecam mode does not trigger sometimes.

# v1.0.2

- Fixed a bug where health animations play and stack when switching between spectated players.

# v1.0.1

- Fixed a bug where some config options will not be hot-reloaded correctly.

# v1.0.0

- Initial release