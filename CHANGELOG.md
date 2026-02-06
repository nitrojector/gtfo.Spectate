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

- Added a keybind `T` to toggle auto transition to temporary follow view in Freecam mode. Note that changes in game to this option will be reflected in the config.
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