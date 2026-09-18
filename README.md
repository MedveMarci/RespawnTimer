# RespawnTimer

![Downloads](https://img.shields.io/github/downloads/MedveMarci/RespawnTimer/total)
[![Version](https://img.shields.io/github/v/release/MedveMarci/RespawnTimer?&label=Version&color=blue)](https://github.com/MedveMarci/AutoEvent/releases/latest)
![Framework](https://img.shields.io/badge/.NET-4.8-purple)
![License](https://img.shields.io/badge/license-MIT-green)

> **SCP: Secret Laboratory LabAPI plugin** that shows when the next respawn wave will happen.

## Support

<a href='https://discord.gg/KmpA8cfaSA'><img src='https://www.allkpop.com/upload/2021/01/content/262046/1611711962-discord-button.png' height="80"></a>

---

## Features

- **Fully customizable timer** — displays round time, server TPS, spectator count, active generators and more
- **Custom hints** — add advertisements or gameplay tips that cycle through the interface
- **Toggle per player** — players can show or hide the timer via Server-Specific Settings
- **Multiple variants** — compatible with [HintServiceMeow](https://github.com/MedveMarci/HintServiceMeow/releases/latest), [RueI](https://github.com/pawslee/RueI) and the base game hint system
- **Public API** — full C# API for other plugins to register custom placeholders

---

## Installation

1. Download the release that matches your setup from [GitHub Releases](https://github.com/MedveMarci/RespawnTimer/releases/latest):
   - `RespawnTimer.dll` — base game hint system
   - `RespawnTimer-HSM.dll` — HintServiceMeow (my fork of it you can get it, from [here](https://github.com/MedveMarci/HintServiceMeow/releases/latest))
   - `RespawnTimer-RueI.dll` — RueI
2. Place the `.dll` in your server's plugins folder.
   - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`
   - Windows: `%appdata%/SCP Secret Laboratory/LabAPI/plugins/global/`
3. Start the server — the timer files are generated automatically.

---

## Configuration

Timer files are stored in:
- Linux: `~/.config/SCP Secret Laboratory/LabAPI/configs/RespawnTimer/`
- Windows: `%appdata%/SCP Secret Laboratory/LabAPI/configs/RespawnTimer/`

```
configs/
└── RespawnTimer/
    ├── TimerBeforeSpawn.txt
    ├── TimerDuringSpawn.txt
    ├── Properties.yml
    └── Hints.txt
```

All of these are generated on first launch with their default contents. The plugin checks them on every start, so
deleting a single file (to reset it, for example) is enough — only that file is regenerated, the rest are left
untouched. No internet connection is needed.

> **Upgrading from an older version?** The plugin will automatically migrate your files from the old `DefaultTimer/` folder to the new location.

---

## Placeholders

| Placeholder | Description |
|-------------|-------------|
| `{cminutes}` / `{cseconds}` | Chaos Insurgency spawn countdown |
| `{nminutes}` / `{nseconds}` | NTF spawn countdown |
| `{mcminutes}` / `{mcseconds}` | Mini CI spawn countdown |
| `{mnminutes}` / `{mnseconds}` | Mini NTF spawn countdown |
| `{mctoken}` / `{mntoken}` | Mini wave respawn tokens |
| `{sminutes}` / `{sseconds}` | Spawn countdown during wave (all factions) |
| `{round_hours}` / `{round_minutes}` / `{round_seconds}` | Current round time |
| `{spectators_num}` | Number of spectators |
| `{team}` | Currently spawning team name (only while a wave is spawning) |
| `{next_team}` | Name of the wave that can spawn next, colored via `Properties.yml` |
| `{warhead_status}` | Current warhead status |
| `{detonation_time}` | Warhead detonation countdown |
| `{generator_engaged}` / `{generator_count}` | Generator counts |
| `{tps}` / `{tickrate}` | Server TPS and tickrate |
| `{hint}` | Current cycling hint from `Hints.txt` |
| `{RANDOM_COLOR}` | Random hex color code |

---

## API

Other plugins can register custom placeholders via `TimerAPI`:

```csharp
// Register a custom placeholder
TimerAPI.RegisterProperty("my_placeholder", player => player.Nickname);

// Unregister it
TimerAPI.UnregisterProperty("my_placeholder");
```

Once registered, `{my_placeholder}` can be used in `TimerBeforeSpawn.txt` and `TimerDuringSpawn.txt`. The value provider receives the **spectated player** as the argument.

| Method | Description |
|--------|-------------|
| `RegisterProperty(string placeholder, Func<Player, string> valueProvider)` | Registers a new custom placeholder |
| `UnregisterProperty(string placeholder)` | Removes a previously registered placeholder |

### Custom waves

Plugins that add their own `TimeBasedWave` can register it so the timer can display it:

```csharp
// displayName is used for {team} and {next_team}, "x" enables {xminutes}/{xseconds}/{xtoken},
// and 14f is the length of the wave's spawn animation, used for the {s...} countdown.
TimerAPI.RegisterWave<MyWave>("<color=#FF96DE>My Wave</color>", "x", 14f);

// Unregister it
TimerAPI.UnregisterWave<MyWave>();
```

Register RespawnTimer as a **soft** dependency where possible, so your plugin keeps working when it
is not installed. RespawnTimer ships as three separate assemblies (`RespawnTimer`, `RespawnTimer-HSM`
and `RespawnTimer-RueI`) that all expose the same `RespawnTimer.API.TimerAPI` type, so a hard
assembly reference binds your plugin to one specific variant. See
[SerpentsHand](https://github.com/MedveMarci/SerpentsHand) for a reflection-based example.

| Method | Description |
|--------|-------------|
| `RegisterWave<T>(string displayName, string placeholder = null, float spawnDuration = 18f)` | Registers a wave with a fixed display name |
| `RegisterWave<T>(Func<string> displayNameProvider, string placeholder = null, float spawnDuration = 18f)` | Registers a wave whose display name is resolved on each update |
| `RegisterWave(Type waveType, ...)` | Same as above, for when the wave type is not known at compile time |
| `UnregisterWave<T>()` / `UnregisterWave(Type waveType)` | Removes a previously registered wave |

---

## Credits

- Original plugin by [Michal78900](https://github.com/Michal78900)
- Maintained by **MedveMarci**
