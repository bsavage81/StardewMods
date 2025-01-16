using AdvancedHorses.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;

namespace AdvancedHorses.Managers
{
    public class MultiplayerManager(
        IManifest modManifest,
        IModHelper helper,
        IMonitor monitor,
        ModConfig config,
        ConfigManager ConfigManager)
    {
        private readonly IManifest _modManifest = modManifest;
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly ConfigManager _configManager = ConfigManager;

        public ModConfig _config = config;

        public void BroadcastHorseConfig()
        {
            if (!Context.IsMainPlayer) return;

            string farmName = Game1.player.farmName.Value ?? "Unknown";
            if (this._config.HorseConfigs.TryGetValue(farmName, out var horseConfigs))
            {
                var message = new HorseConfigSyncMessage
                {
                    FarmName = farmName,
                    HorseConfigs = horseConfigs
                };

                this._helper.Multiplayer.SendMessage(
                    message: message,
                    messageType: "HorseConfigSync",
                    modIDs: new[] { this._modManifest.UniqueID }
                );

                this._monitor.Log($"Broadcasted horse configurations for farm '{farmName}' to connected players.", LogLevel.Info);
            }

            this._configManager.InitializeOrUpdateHorseConfigs();

        }

        public void NotifyHostOfConfigChange()
        {
            if (Context.IsMainPlayer) return;

            string farmName = Game1.player.farmName.Value ?? "Unknown";
            if (this._config.HorseConfigs.TryGetValue(farmName, out var horseConfigs))
            {
                var message = new HorseConfigSyncMessage
                {
                    FarmName = farmName,
                    HorseConfigs = horseConfigs
                };

                this._helper.Multiplayer.SendMessage(
                    message: message,
                    messageType: "HorseConfigUpdate",
                    modIDs: new[] { this._modManifest.UniqueID }
                );

                this._monitor.Log($"Sent updated horse configurations for farm '{farmName}' to the host.", LogLevel.Info);
            }
        }

        public void OnMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != this._modManifest.UniqueID || e.Type != "HorseConfigSync") return;

            var message = e.ReadAs<HorseConfigSyncMessage>();
            this._monitor.Log($"Received horse configuration for farm '{message.FarmName}' from host.", LogLevel.Info);

            this._config.HorseConfigs[message.FarmName] = message.HorseConfigs;
            this._configManager.InitializeOrUpdateHorseConfigs();
        }

        public void OnPlayerConfigUpdateReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (!Context.IsMainPlayer || e.FromModID != this._modManifest.UniqueID || e.Type != "HorseConfigUpdate") return;

            var message = e.ReadAs<HorseConfigSyncMessage>();
            this._monitor.Log($"Received updated horse configuration for farm '{message.FarmName}' from player.", LogLevel.Info);

            this._config.HorseConfigs[message.FarmName] = message.HorseConfigs;
            this.BroadcastHorseConfig(); // Re-broadcast to all players
        }

        public class HorseConfigSyncMessage
        {
            public string FarmName { get; set; }
            public Dictionary<string, HorseConfig> HorseConfigs { get; set; }
        }
    }
}
