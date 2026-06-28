## [1.4.1]

### Fixed
- Properties.yml file fixer erroneously triggering on every load due to naming convention mismatch.

## [1.4.0]

### Added
- **UCR (Uncomplicated Custom Roles) integration** for displaying if the spectated player is a custom role. Add `{custom_role}` tag into the .txt files.
- Added DeadManSwitch checker, if it is active it will display a different text.
- **Custom spawn wave registration** via `TimerAPI.RegisterWave`, so plugins (e.g. CustomRespawnWaves) can register their own `TimeBasedWave` types. The timer now shows the wave's `{team}` name, an optional `{<prefix>minutes}`/`{<prefix>seconds}` countdown and its during-spawn `{s...}` offset instead of throwing on unknown waves.
- Added `properties.yml` file fixer, it will add missing properties.

### Removed
- Unused `ShOffset` property and its references.

### Fixed
- Blinking hint issue.
- Timer not showing immediately when a player joins the server.