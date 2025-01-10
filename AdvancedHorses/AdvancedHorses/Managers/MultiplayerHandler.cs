using AdvancedHorses.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;

namespace AdvancedHorses.Managers
{
    public class MultiplayerHandler(IModHelper helper, IMonitor monitor, ModConfig config, HorseManager horseManager, ConfigManager ConfigManager)
    {
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly ModConfig _config = config;
        private readonly HorseManager _horseManager = horseManager;
        private readonly ConfigManager _ConfigManager = ConfigManager;

        public void BroadcastHorseConfig()
        {
            if (!Context.IsMainPlayer) return;

            string farmName = Game1.player.farmName.Value ?? "Unknown";
            if (_config.HorseConfigs.TryGetValue(farmName, out var horseConfigs))
            {
                var message = new HorseConfigSyncMessage
                {
                    FarmName = farmName,
                    HorseConfigs = horseConfigs
                };

                _helper.Multiplayer.SendMessage(
                    message: message,
                    messageType: "HorseConfigSync",
                    modIDs: new[] { _helper.ModRegistry.ModID }
                );

                _monitor.Log($"Broadcasted horse configurations for farm '{farmName}' to connected players.", LogLevel.Info);
            }
        }

        public void OnMessageReceived(ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != _helper.ModRegistry.ModID || e.Type != "HorseConfigSync") return;

            var message = e.ReadAs<HorseConfigSyncMessage>();
            _monitor.Log($"Received horse configuration for farm '{message.FarmName}' from host.", LogLevel.Info);

            _config.HorseConfigs[message.FarmName] = message.HorseConfigs;

            // Delegate to HorseManager
            _ConfigManager.InitializeOrUpdateHorseConfigs();
            _horseManager.ProcessHorses();
        }

        public void NotifyHostOfConfigChange()
        {
            if (Context.IsMainPlayer) return;

            string farmName = Game1.player.farmName.Value ?? "Unknown";
            if (_config.HorseConfigs.TryGetValue(farmName, out var horseConfigs))
            {
                var message = new HorseConfigSyncMessage
                {
                    FarmName = farmName,
                    HorseConfigs = horseConfigs
                };

                _helper.Multiplayer.SendMessage(
                    message: message,
                    messageType: "HorseConfigUpdate",
                    modIDs: new[] { _helper.ModRegistry.ModID }
                );

                _monitor.Log($"Sent updated horse configurations for farm '{farmName}' to the host.", LogLevel.Info);
            }
        }

        public void OnPlayerConfigUpdateReceived(ModMessageReceivedEventArgs e)
        {
            if (!Context.IsMainPlayer || e.FromModID != _helper.ModRegistry.ModID || e.Type != "HorseConfigUpdate") return;

            var message = e.ReadAs<HorseConfigSyncMessage>();
            _monitor.Log($"Received updated horse configuration for farm '{message.FarmName}' from player.", LogLevel.Info);

            _config.HorseConfigs[message.FarmName] = message.HorseConfigs;

            // Re-broadcast updated config to all players
            BroadcastHorseConfig();
        }
    }

    public class HorseConfigSyncMessage
    {
        public string FarmName { get; set; }
        public Dictionary<string, HorseConfig> HorseConfigs { get; set; }
    }
}
