<div align="center">

<p align="center">
<img src="https://git.takina.io/nitrojector/stuff/-/raw/main/pfp/takina_idiot_sandwich.png" width="200" alt="Takina">
</p>

<h1 align="center">Spectate</h1>

<p align="center">Spectate your teammates</p>

<p align="center"><i>Sensitivities, keybinds, and multiple behaviors are customizable through the config</i></p>

<p align="center">For feedback and issues, DM <code>@uwufood</code> or message <a href="https://discord.com/channels/782438773690597389/1468851402339258499">the feedback thread</a> on the GTFO Modding Discord Server</p>

</div>

<hr/>

*Known Issues*

- Players do not prevent respawn rooms from respawning when spectating (i.e. their in-game position is effectively
  whomever they are spectating).
- Bots, a lot of times, do not shoot their weapon when the player is spectating.

*Attributions*

- This mod uses assets from [Bootstrap Icons](https://icons.getbootstrap.com/) licensed under [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/).

## Building

### Assets (if changed)

1. Follow instructions in `emojis_pack` to build packed emoji atlases (Linux or wsl).
2. Build asset bundles in `SpectateUnityAssetProject` Unity project.

### Mod

3. Install mod dependencies (EEC_H, PlayerSync, ClonesoftJson)
4. Build the solution `Spectate.sln`.
