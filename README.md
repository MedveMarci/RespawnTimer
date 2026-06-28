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
- **Multiple variants** — compatible with [HintServiceMeow](https://github.com/MeowServer/HintServiceMeow/releases/latest), [RueI](https://github.com/pawslee/RueI) and the base game hint system
- **Public API** — full C# API for other plugins to register custom placeholders

---

## Installation

1. Download the release that matches your setup from [GitHub Releases](https://github.com/MedveMarci/RespawnTimer/releases/latest):
   - `RespawnTimer.dll` — base game hint system
   - `RespawnTimer-HSM.dll` — HintServiceMeow
   - `RespawnTimer-RueI.dll` — RueI
2. Place the `.dll` in your server's plugins folder.
   - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`
   - Windows: `%appdata%/SCP Secret Laboratory/LabAPI/plugins/global/`
3. Start the server — timer files are downloaded automatically.

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

On first launch all required files are downloaded automatically. If a single file is missing, only that file is re-downloaded.

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
| `{team}` | Next spawning team name |
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

---

## Credits

- Original plugin by [Michal78900](https://github.com/Michal78900)
- Maintained by **MedveMarci**
