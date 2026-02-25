# v1.5.5

- Refactored some logic to be more robust
- Fixed a bug where under certain conditions the game would crash with a native pointer exception on
  `PUI_LocalPlayerStatus.SetDamageAnim`.
- Fixed an incompatibility issue with Striker Mode where PLOC state would be desynced when dropping in in-level lobby.

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