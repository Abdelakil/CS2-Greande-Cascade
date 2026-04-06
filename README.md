# Grenade Cascade

A SwiftlyS2 plugin that enhances HE grenades to spawn additional grenades on detonation for players with specific permissions.

## Features

- **Permission-based access**: Only players with specified permissions can trigger the cascade effect
- **Configurable grenade count**: Set how many additional grenades spawn
- **Configurable throw distances**: Control minimum and maximum throw distances for cascade grenades
- **Player toggle command**: `!cascade` command for players to enable/disable personal cascade
- **Global enable/disable**: Server-wide toggle for the entire feature
- **Recursion prevention**: Spawned grenades cannot trigger further cascades
- **Natural scattering**: Grenades spawn with randomized velocities for realistic behavior
- **Localization support**: Full translation system with English included
- **Hot-reload configuration**: Change settings without restarting the server
- **Console logging**: Detailed logging for configuration changes and player actions

## Installation

1. Download the latest release from [GitHub Releases](https://github.com/Abdelakil/CS2-Greande-Cascade/releases)
2. Extract the `GrenadeCascade` folder to your SwiftlyS2 plugins directory
3. Create the configuration file as described below
4. Restart your server or reload plugins

## Configuration

Create `resources/configs/grenade_cascade.jsonc` in your server's GrenadeCascade plugin directory:

```jsonc
{
  "GrenadeCascade": {
    "enabled": true,
    "allow_player_toggle": true,
    "extra_grenades_count": 3,
    "max_throw_distance": 300.0,
    "min_throw_distance": 150.0,
    "required_permission": "grenade_cascade.use",
    "enable_debug_logging": true,
    "log_spawn_attempts": true
  }
}
```

### Settings

- **enabled**: Enable or disable the entire grenade cascade feature globally
- **allow_player_toggle**: Allow players to use the `!cascade` command to toggle personal cascade
- **extra_grenades_count**: Number of additional grenades to spawn on detonation (1-10 recommended)
- **max_throw_distance**: Maximum throw distance for cascade grenades in units
- **min_throw_distance**: Minimum throw distance for cascade grenades in units
- **required_permission**: Permission required to use the feature (empty = no permission needed)
- **enable_debug_logging**: Enable detailed debug logging to server console
- **log_spawn_attempts**: Log grenade spawn attempts for troubleshooting

## Commands

### !cascade

Allows players to toggle their personal grenade cascade setting on/off.

**Requirements:**
- Player must have `grenade_cascade.use` permission
- Global cascade must be enabled (`enabled: true`)
- Player toggle must be allowed (`allow_player_toggle: true`)

**Usage:**
```
!cascade
```

**Messages:**
- `[red][OSTORA][red] [white]Grenade cascade [green]enabled[white]!`
- `[red][OSTORA][red] [white]Grenade cascade [red]disabled[white]!`
- `[red][OSTORA][red] [white]You don't have permission to use grenade cascade!`
- `[red][OSTORA][red] [white]Grenade cascade is currently disabled by server configuration.`

## Translations

Add new languages by creating files in `resources/translations/`:
- `en.jsonc` (English - included)
- `fr.jsonc` (French)
- `de.jsonc` (German)
- etc.

### Translation Keys

```jsonc
{
  "grenadecascade.general.prefix": "[red][OSTORA][red]",
  "grenadecascade.messages.permission_denied": "[white]You don't have permission to use grenade cascade!",
  "grenadecascade.messages.cascade_activated": "[white]Grenade cascade activated!",
  "grenadecascade.messages.cascade_enabled": "[white]Grenade cascade [green]enabled[white]!",
  "grenadecascade.messages.cascade_disabled": "[white]Grenade cascade [red]disabled[white]!",
  "grenadecascade.messages.cascade_globally_disabled": "[white]Grenade cascade is currently disabled by server configuration."
}
```

## Usage

1. Grant players the `grenade_cascade.use` permission
2. Configure the plugin settings in `grenade_cascade.jsonc`
3. Players throw HE grenades as normal
4. On detonation, additional grenades spawn automatically with realistic physics
5. Players can use `!cascade` to toggle their personal setting (if allowed)

## Technical Details

- **Plugin ID**: `GrenadeCascade`
- **Author**: Zenjibad
- **Version**: 1.0.0
- **Website**: https://ostora.xyz
- **Target Framework**: .NET 10.0
- **Config Hot Reload**: Supported with console logging
- **Safe API Usage**: Uses only SwiftlyS2 safe APIs

## Performance Considerations

- Grenade spawning is staggered to prevent server lag (0.3s, 0.7s, 1.1s intervals)
- Entity tracking prevents infinite recursion
- Memory usage is minimal with proper cleanup
- All operations are async-safe
- Config hot reload without server restart

## Logging

The plugin provides detailed console logging for:
- Configuration changes with old vs new values
- Player toggle actions with SteamID and username
- Global enable/disable status changes
- Debug information (if enabled)

Example log output:
```
[GrenadeCascade] Configuration reloaded!
[GrenadeCascade] Previous: Enabled=True, Grenades=3, MaxDistance=300.0, MinDistance=150.0
[GrenadeCascade] New: Enabled=False, Grenades=3, MaxDistance=300.0, MinDistance=150.0
[GrenadeCascade] Global cascade DISABLED
[GrenadeCascade] Player Zenjibad (76561198012345678) ENABLED personal cascade
```

## Support

For issues and support:
- **GitHub**: https://github.com/Abdelakil/CS2-Greande-Cascade
- **Website**: https://ostora.xyz
- **Author**: Zenjibad
