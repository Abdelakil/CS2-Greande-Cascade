# Grenade Cascade

A SwiftlyS2 plugin that enhances HE grenades to spawn additional grenades on detonation for players with specific permissions.

## Features

- **Permission-based access**: Only players with specified permissions can trigger the cascade effect
- **Configurable grenade count**: Set how many additional grenades spawn
- **Recursion prevention**: Spawned grenades cannot trigger further cascades
- **Natural scattering**: Grenades spawn with randomized velocities for realistic behavior
- **Localization support**: Full translation system with English included
- **Hot-reload configuration**: Change settings without restarting the server

## Installation

1. Place the `GrenadeCascade` folder in your SwiftlyS2 plugins directory
2. Restart your server or reload plugins
3. Configure permissions as needed

## Configuration

The plugin automatically creates `grenade_cascade.jsonc` in `resources/configs/`:

```jsonc
{
  "enabled": true,
  "extra_grenades_count": 2,
  "required_permission": "@grenade_cascade/use"
}
```

### Settings

- **enabled**: Enable or disable the plugin
- **extra_grenades_count**: Number of additional grenades to spawn (1-10 recommended)
- **required_permission**: Permission required to use the feature (empty = no permission needed)

## Permissions

Set up permissions in your SwiftlyS2 configuration:

```json
{
  "permissions": {
    "grenade_cascade.use": {
      "groups": ["vip", "admin"],
      "immunity": 1
    }
  }
}
```

## Translations

Add new languages by creating files in `resources/translations/`:
- `en.json` (English - included)
- `fr.json` (French)
- `de.json` (German)
- etc.

### Translation Keys

```json
{
  "prefix": "[OSTORA]",
  "plugin_loaded": "Plugin loaded successfully",
  "permission_denied": "You don't have permission to use grenade cascade!",
  "cascade_activated": "Grenade cascade activated!"
}
```

## Usage

1. Grant players the `@grenade_cascade/use` permission
2. Players throw HE grenades as normal
3. On detonation, additional grenades spawn automatically
4. Players receive a chat notification when the cascade activates

## Technical Details

- **Plugin ID**: `GrenadeCascade`
- **Author**: Zenjibad
- **Version**: 1.0.0
- **Website**: https://ostora.xyz
- **Target Framework**: .NET 10.0

## Performance Considerations

- Grenade spawning is throttled to prevent server lag
- Entity tracking prevents infinite recursion
- Memory usage is minimal with proper cleanup
- All operations are async-safe

## Support

For issues and support:
- Website: https://ostora.xyz
- Author: Zenjibad
