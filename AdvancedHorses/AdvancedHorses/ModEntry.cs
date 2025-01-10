using AdvancedHorses.Config;
using AdvancedHorses.Helpers;
using AdvancedHorses.Managers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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
            Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();

            // Initialize components in the correct order
            _assetLoader = new AssetLoader(Monitor, helper);
            _assetLoader.LoadDynamicAllowedValues();

            _compositeGenerator = new CompositeGenerator(Monitor, helper);

            _horseManager = new HorseManager(Monitor, helper, Config);

            _configManager = new ConfigManager(Monitor, helper, Config, _compositeGenerator, _assetLoader, _horseManager);

            _multiplayerHandler = new MultiplayerHandler(helper, Monitor, Config, _horseManager, _configManager);

            _gmcmHandler = new GmcmHandler(this.ModManifest, helper, Monitor, Config, _configManager, _multiplayerHandler);

            // Register event handlers
                // Gameloop Events
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
                // Multiplayer Events
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
            helper.Events.Multiplayer.ModMessageReceived += OnPlayerConfigUpdateReceived;


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

        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if(e.FromModID != this.ModManifest.UniqueID)
                return;

            // Potential future logging or preprocessing
            this.Monitor.Log($"Received message of type '{e.Type}' from mod '{e.FromModID}'.", LogLevel.Debug);

            _multiplayerHandler.OnMessageReceived(e);
        }

        private void OnPlayerConfigUpdateReceived(object sender, ModMessageReceivedEventArgs e)
        {
            _multiplayerHandler.OnPlayerConfigUpdateReceived(e);
        }

    }
}
