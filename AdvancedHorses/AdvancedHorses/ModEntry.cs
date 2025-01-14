using AdvancedHorses.Config;
using AdvancedHorses.Helpers;
using AdvancedHorses.Managers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace AdvancedHorses
{
    public class ModEntry : Mod
    {
        private ConfigManager _configManager;
        private AssetLoader _assetLoader;
        private CompositeGenerator _compositeGenerator;
        private HorseManager _horseManager;
        private GmcmHandler _gmcmHandler;
        private MultiplayerHandler _multiplayerHandler;

        public ModConfig Config { get; private set; }

        public override void Entry(IModHelper helper)
        {
            // Load configuration
            this.Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();

            // Initialize components in the correct order
            _assetLoader = new AssetLoader(Monitor, helper);
            _assetLoader.LoadDynamicAllowedValues();

            _compositeGenerator = new CompositeGenerator(Monitor, helper);

            _horseManager = new HorseManager(Monitor, helper, Config, _assetLoader);

            _configManager = new ConfigManager(Monitor, helper, Config, _compositeGenerator, _assetLoader, _horseManager);

            _multiplayerHandler = new MultiplayerHandler(this.ModManifest, helper, Monitor, Config, _configManager, _horseManager);

            _gmcmHandler = new GmcmHandler(this.ModManifest, helper, Monitor, Config, _configManager, _horseManager, _multiplayerHandler);

            // Register event handlers
                // Gameloop Events
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
                // Multiplayer Events
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
            helper.Events.Multiplayer.ModMessageReceived += OnPlayerConfigUpdateReceived;

            // Register Commands
            this.Helper.ConsoleCommands.Add("refresh_horses", "Refresh all horse configurations and appearances.", (command, args) =>
            {
                _configManager.InitializeOrUpdateHorseConfigs(); // Ensure configs are up-to-date
                _horseManager.ProcessHorses(); // Reapply all configurations
                this.Monitor.Log("Horse configurations refreshed.", LogLevel.Info);
            });

        }
            private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            _gmcmHandler.SetupModConfigMenu();
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            _configManager.InitializeOrUpdateHorseConfigs();
            _horseManager.ProcessHorses();

            if (Context.IsMainPlayer)
            {
                _multiplayerHandler.BroadcastHorseConfig(); // Broadcast updated config to all players
                this.Monitor.Log("Broadcasted updated horse configurations to connected players.", LogLevel.Info);
            }
            else
            {
                _multiplayerHandler.NotifyHostOfConfigChange(); // Notify host about the config change
                this.Monitor.Log("Sent updated horse configurations to the host.", LogLevel.Info);
            }
        }

        private void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            _configManager.InitializeOrUpdateHorseConfigs();
            _horseManager.ProcessHorses();

            if (Context.IsMainPlayer)
            {
                _multiplayerHandler.BroadcastHorseConfig(); // Broadcast updated config to all players
                this.Monitor.Log("Broadcasted updated horse configurations to connected players.", LogLevel.Info);
            }
            else
            {
                _multiplayerHandler.NotifyHostOfConfigChange(); // Notify host about the config change
                this.Monitor.Log("Sent updated horse configurations to the host.", LogLevel.Info);
            }
        }

        public void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if(e.FromModID != this.ModManifest.UniqueID)
                return;

            // Potential future logging or preprocessing
            this.Monitor.Log($"Received message of type '{e.Type}' from mod '{e.FromModID}'.", LogLevel.Debug);

            _multiplayerHandler.OnMessageReceived(this, e);
        }

        public void OnPlayerConfigUpdateReceived(object sender, ModMessageReceivedEventArgs e)
        {
            _multiplayerHandler.OnPlayerConfigUpdateReceived(this, e);
        }
    }
}
