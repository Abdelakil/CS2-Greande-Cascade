using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Permissions;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.EntitySystem;
using SwiftlyS2.Shared.Schemas;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Commands;
using System.Numerics;
using System.Linq;

// Define the config class inline since we removed the Config folder
namespace GrenadeCascade.Config;

public class GrenadeCascadeConfig
{
    public bool Enabled { get; set; } = true;
    public bool AllowPlayerToggle { get; set; } = true;
    public int ExtraGrenadesCount { get; set; } = 2;
    public float MaxThrowDistance { get; set; } = 300.0f;
    public float MinThrowDistance { get; set; } = 150.0f;
    public string RequiredPermission { get; set; } = "grenade_cascade.use";
    public bool EnableDebugLogging { get; set; } = true;
    public bool LogSpawnAttempts { get; set; } = true;
}

[PluginMetadata(
    Id = "GrenadeCascade",
    Name = "Grenade Cascade",
    Author = "Zenjibad",
    Version = "1.0.0",
    Description = "Enhances HE grenades to spawn additional grenades on detonation for players with specific permissions.",
    Website = "https://ostora.xyz"
)]
public sealed class GrenadeCascade(ISwiftlyCore core) : BasePlugin(core)
{
    public static new ISwiftlyCore Core { get; private set; } = null!;
    
    private GrenadeCascadeConfig? _config;
    private readonly HashSet<uint> _spawnedGrenades = new();
    private readonly Dictionary<ulong, bool> _playerCascadeEnabled = new(); // Player toggle state
    
    private IEntitySystemService? _entitySystem;
    private Guid? _heGrenadeHook;
    
    public override void Load(bool hotReload)
    {
        Core = core;
        
        // Initialize configuration file with SwiftlyS2's configuration system
        _configMonitor = BuildConfigService<GrenadeCascadeConfig>("grenade_cascade.jsonc", "GrenadeCascade");
        _config = _configMonitor.CurrentValue;
        
        // Subscribe to config changes for hot reload
        _configMonitor.OnChange((newConfig, name) =>
        {
            var oldEnabled = _config?.Enabled ?? true;
            var oldGrenadeCount = _config?.ExtraGrenadesCount ?? 2;
            var oldMaxDistance = _config?.MaxThrowDistance ?? 300.0f;
            var oldMinDistance = _config?.MinThrowDistance ?? 150.0f;
            
            _config = newConfig;
            
            // Log configuration changes
            Core.Logger.LogInformation("[GrenadeCascade] Configuration reloaded!");
            Core.Logger.LogInformation($"[GrenadeCascade] Previous: Enabled={oldEnabled}, Grenades={oldGrenadeCount}, MaxDistance={oldMaxDistance}, MinDistance={oldMinDistance}");
            Core.Logger.LogInformation($"[GrenadeCascade] New: Enabled={newConfig.Enabled}, Grenades={newConfig.ExtraGrenadesCount}, MaxDistance={newConfig.MaxThrowDistance}, MinDistance={newConfig.MinThrowDistance}");
            
            // Log specific changes
            if (oldEnabled != newConfig.Enabled)
            {
                Core.Logger.LogInformation($"[GrenadeCascade] Global cascade {(newConfig.Enabled ? "ENABLED" : "DISABLED")}");
            }
            
            if (oldGrenadeCount != newConfig.ExtraGrenadesCount)
            {
                Core.Logger.LogInformation($"[GrenadeCascade] Extra grenades count changed: {oldGrenadeCount} → {newConfig.ExtraGrenadesCount}");
            }
            
            if (oldMaxDistance != newConfig.MaxThrowDistance || oldMinDistance != newConfig.MinThrowDistance)
            {
                Core.Logger.LogInformation($"[GrenadeCascade] Throw distance changed: {oldMinDistance}-{oldMaxDistance} → {newConfig.MinThrowDistance}-{newConfig.MaxThrowDistance}");
            }
        });
        
        // Get required services
        _entitySystem = Core.EntitySystem;
        
        // Hook into HE grenade detonation events
        _heGrenadeHook = Core.GameEvent.HookPre<EventHegrenadeDetonate>(OnHegrenadeDetonate);
        
        // Register cascade command
        Core.Command.RegisterCommand("cascade", HandleCascadeCommand);
    }
    
    private IOptionsMonitor<T> BuildConfigService<T>(string fileName, string sectionName) where T : class, new()
    {
        Core.Configuration
            .InitializeJsonWithModel<T>(fileName, sectionName)
            .Configure(cfg => cfg.AddJsonFile(Core.Configuration.GetConfigPath(fileName), optional: false, reloadOnChange: true));

        ServiceCollection services = new();
        services.AddSwiftly(Core)
            .AddOptions<T>()
            .BindConfiguration(sectionName);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<T>>();
    }
    
    private HookResult OnHegrenadeDetonate(EventHegrenadeDetonate @event)
    {
        // Check if cascade is globally disabled
        if (_config?.Enabled != true)
        {
            return HookResult.Continue;
        }
        
        var originalPlayer = @event.UserIdPlayer;
        if (originalPlayer == null)
        {
            return HookResult.Continue;
        }
        
        // Check permissions
        if (!string.IsNullOrEmpty(_config?.RequiredPermission) && 
            !Core.Permission.PlayerHasPermission(originalPlayer.SteamID, _config.RequiredPermission))
        {
            return HookResult.Continue;
        }
        
        // Check if player has cascade enabled (if player toggle is allowed)
        if (_config?.AllowPlayerToggle == true)
        {
            var playerSteamId = originalPlayer.SteamID;
            // Default to enabled for now (command system removed)
            if (!_playerCascadeEnabled.GetValueOrDefault(playerSteamId, true))
            {
                return HookResult.Continue;
            }
        }
        
        // Prevent recursion - skip if this grenade was spawned by us
        if (_spawnedGrenades.Contains((uint)@event.EntityID))
        {
            _spawnedGrenades.Remove((uint)@event.EntityID);
            return HookResult.Continue;
        }
        
        // Activate cascade
        SpawnAdditionalGrenades(@event);
        
        return HookResult.Continue;
    }
    
    private void SpawnAdditionalGrenades(EventHegrenadeDetonate @event)
    {
        var explosionOrigin = new Vector3(@event.X, @event.Y, @event.Z);
        var count = _config?.ExtraGrenadesCount ?? 2;
        
        for (int i = 0; i < count; i++)
        {
            try
            {
                // Get the original player who threw the grenade
                var originalPlayer = @event.UserIdPlayer;
                
                // Calculate staggered delays for natural grenade cascade
                var random = new Random();
                var cascadeDelay = 0.3f + (i * 0.4f) + (float)(random.NextDouble() * 0.2f); // 0.3s, 0.7s, 1.1s + random
                
                // CRITICAL: Ensure delay is always positive to prevent scheduler errors
                cascadeDelay = Math.Max(0.1f, cascadeDelay); // Minimum 0.1s delay
                var grenadeIndex = i;
                
                // Schedule the creation of actual HE grenades
                Core.Scheduler.DelayBySeconds(cascadeDelay, () => {
                    try {
                        // Create random position for this grenade - SPAWN INSIDE AREA
                        var randomPos = new Random();
                        var grenadeOffset = new Vector3(
                            (float)(randomPos.NextDouble() * 60 - 30),    // -30 to 30 (close, inside)
                            20.0f,                                        // Start slightly above ground for throw
                            (float)(randomPos.NextDouble() * 60 - 30)    // -30 to 30 (close, inside)
                        );
                        var grenadePosition = explosionOrigin + grenadeOffset;
                        
                        // Create random velocity for realistic THROW physics - CONFIGURABLE DISTANCE
                        var maxDistance = _config?.MaxThrowDistance ?? 300.0f;
                        var minDistance = _config?.MinThrowDistance ?? 150.0f;
                        
                        var grenadeVelocity = new SwiftlyS2.Shared.Natives.Vector(
                            (float)(randomPos.NextDouble() * maxDistance - (maxDistance / 2)),  // Configurable horizontal
                            (float)(randomPos.NextDouble() * (maxDistance * 0.8f) + (maxDistance * 0.5f)), // Configurable upward arc
                            (float)(randomPos.NextDouble() * maxDistance - (maxDistance / 2))    // Configurable directional
                        );
                        
                        // Create random angle for realistic throw orientation
                        var grenadeAngle = new SwiftlyS2.Shared.Natives.QAngle(
                            (float)(randomPos.NextDouble() * 45 - 22.5), // -22.5 to 22.5 degrees (throw angle)
                            (float)(randomPos.NextDouble() * 360),       // 0 to 360 degrees (throw direction)
                            (float)(randomPos.NextDouble() * 30 - 15)    // -15 to 15 degrees (spin)
                        );
                        
                        // Create ACTUAL HE grenade that you can see and hear
                        var cascadeGrenade = SwiftlyS2.Shared.SchemaDefinitions.CHEGrenadeProjectile.EmitGrenade(
                            new SwiftlyS2.Shared.Natives.Vector(grenadePosition.X, grenadePosition.Y, grenadePosition.Z),
                            grenadeAngle,
                            grenadeVelocity,
                            originalPlayer?.PlayerPawn
                        );
                        
                        // Track the cascade grenade to prevent recursion
                        if (cascadeGrenade != null)
                        {
                            var handle = Core.EntitySystem.GetRefEHandle(cascadeGrenade!);
                            if (handle.IsValid)
                            {
                                _spawnedGrenades.Add(handle.EntityIndex);
                            }
                        }
                    } catch (Exception ex) {
                        // Error handling without logging
                    }
                });
            }
            catch (Exception ex)
            {
                // Error handling without logging
            }
        }
    }
    
    public override void Unload()
    {
        if (_heGrenadeHook.HasValue)
        {
            Core.GameEvent.Unhook(_heGrenadeHook.Value);
        }
    }
    
    private void HandleCascadeCommand(ICommandContext context)
    {
        // Check if sender is a player
        if (context.Sender is not IPlayer player)
        {
            Core.Logger.LogInformation("[GrenadeCascade] Console tried to use cascade command - only players can use this command");
            return;
        }
        
        // Check if global cascade is enabled
        if (_config?.Enabled != true)
        {
            var localizer = Core.Translation.GetPlayerLocalizer(player);
            player.SendMessage(MessageType.Chat, $"{localizer["grenadecascade.general.prefix"]} {localizer["grenadecascade.messages.cascade_globally_disabled"]}");
            return;
        }
        
        // Check if player toggle is allowed
        if (_config?.AllowPlayerToggle != true)
        {
            var localizer = Core.Translation.GetPlayerLocalizer(player);
            player.SendMessage(MessageType.Chat, $"{localizer["grenadecascade.general.prefix"]} Player toggle is disabled by server configuration.");
            return;
        }
        
        // Check permissions
        if (!string.IsNullOrEmpty(_config?.RequiredPermission) && 
            !Core.Permission.PlayerHasPermission(player.SteamID, _config.RequiredPermission))
        {
            var localizer = Core.Translation.GetPlayerLocalizer(player);
            player.SendMessage(MessageType.Chat, $"{localizer["grenadecascade.general.prefix"]} {localizer["grenadecascade.messages.permission_denied"]}");
            return;
        }
        
        // Toggle player's cascade setting
        var currentState = _playerCascadeEnabled.GetValueOrDefault(player.SteamID, true);
        var newState = !currentState;
        _playerCascadeEnabled[player.SteamID] = newState;
        
        // Send message to player
        var playerLocalizer = Core.Translation.GetPlayerLocalizer(player);
        var messageKey = newState ? "grenadecascade.messages.cascade_enabled" : "grenadecascade.messages.cascade_disabled";
        player.SendMessage(MessageType.Chat, $"{playerLocalizer["grenadecascade.general.prefix"]} {playerLocalizer[messageKey]}");
        
        // Log the toggle
        Core.Logger.LogInformation($"[GrenadeCascade] Player {player.Controller.PlayerName} ({player.SteamID}) {(newState ? "ENABLED" : "DISABLED")} personal cascade");
    }
    
    public void OnConfigUpdated(GrenadeCascadeConfig config)
    {
        _config = config;
    }
    
    private IOptionsMonitor<GrenadeCascadeConfig> _configMonitor = null!;
}
