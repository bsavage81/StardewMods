using System.Linq;
using AdvancedHorses.Config;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace AdvancedHorses.Managers
{
    public class GmcmHandler(
        IManifest modManifest,
        IModHelper helper,
        IMonitor monitor,
        ModConfig config,
        ConfigManager ConfigManager,
        MultiplayerHandler multiplayerHandler)
    {
        private readonly IManifest _modManifest = modManifest;
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly ModConfig _config = config;
        private readonly ConfigManager _ConfigManager = ConfigManager;
        private readonly MultiplayerHandler _multiplayerHandler = multiplayerHandler;

        public void SetupModConfigMenu()
        {
            var api = _helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api == null)
            {
                _monitor.Log("Generic Mod Config Menu not found. Skipping configuration menu setup.", LogLevel.Warn);
                return;
            }

            // Add event listener for when GMCM is opened
            _helper.Events.Display.MenuChanged += (sender, e) =>
            {
                if (e.NewMenu?.GetType().FullName == "GenericModConfigMenu.Framework.ModConfigMenu")
                {
                    _monitor.Log("GMCM opened. Re-registering menu to include updated horse list.", LogLevel.Debug);

                    // Re-register the menu
                    RegisterModConfigMenu(api);
                }
            };

            // Register the base menu
            RegisterModConfigMenu(api);
        }

        private void RegisterModConfigMenu(IGenericModConfigMenuApi api)
        {
            api.Register(
                mod: _modManifest, // Use the IManifest passed in the constructor
                reset: () =>
                {
                    _config.DefaultHorseConfig = new HorseConfig();
                    _helper.WriteConfig(_config);
                    _ConfigManager.InitializeOrUpdateHorseConfigs();
                },
                save: () =>
                {
                    _helper.WriteConfig(_config);
                    if (Context.IsMainPlayer)
                    {
                        _multiplayerHandler.BroadcastHorseConfig();
                        _monitor.Log("Broadcasted updated horse configurations to connected players.", LogLevel.Info);
                    }
                    else
                    {
                        _multiplayerHandler.NotifyHostOfConfigChange();
                        _monitor.Log("Sent updated horse configurations to the host.", LogLevel.Info);
                    }

                    _ConfigManager.InitializeOrUpdateHorseConfigs();
                }
            );

            // Register default settings
            RegisterDefaultSettings(api);

            // Register horse-specific pages
            RegisterHorsePages(api);

            // Add dynamic update listener
            api.OnFieldChanged(_modManifest, (fieldId, newValue) =>
            {
                _helper.WriteConfig(_config);
                _ConfigManager.InitializeOrUpdateHorseConfigs();
            });
        }

        private void RegisterDefaultSettings(IGenericModConfigMenuApi api)
        {
            api.AddSectionTitle(mod: _modManifest, text: () => "Default Horse Settings");

            // Load the texture from the specified path
            Texture2D defaultTexture = _helper.ModContent.Load<Texture2D>("assets/Generated/default.png");

            // Add the image to the GMCM menu
            api.AddImage(
                mod: _modManifest,
                texture: () => defaultTexture,
                texturePixelArea: new Microsoft.Xna.Framework.Rectangle(0, 32, 32, 32),
                scale: 4
            );

            api.AddTextOption(
                mod: _modManifest,
                name: () => "Default Base Horse Skin",
                getValue: () => _config.DefaultHorseConfig.BaseSkin,
                setValue: value =>
                {
                    _config.DefaultHorseConfig.BaseSkin = value;
                    _ConfigManager.InitializeOrUpdateHorseConfigs();
                },
                allowedValues: ModConstants.ValidBaseSkins.ToArray()
            );

            api.AddTextOption(
                mod: _modManifest,
                name: () => "Default Pattern",
                getValue: () => _config.DefaultHorseConfig.Pattern,
                setValue: value =>
                {
                    _config.DefaultHorseConfig.Pattern = value;
                    _ConfigManager.InitializeOrUpdateHorseConfigs();
                },
                allowedValues: ModConstants.ValidPatterns.ToArray()
            );

            api.AddTextOption(
                mod: _modManifest,
                name: () => "Default Hair",
                getValue: () => _config.DefaultHorseConfig.Hair,
                setValue: value =>
                {
                    _config.DefaultHorseConfig.Hair = value;
                    _ConfigManager.InitializeOrUpdateHorseConfigs();
                },
                allowedValues: ModConstants.ValidHairOptions.ToArray()
            );

            api.AddTextOption(
                mod: _modManifest,
                name: () => "Default Saddle Color",
                getValue: () => _config.DefaultHorseConfig.SaddleColor,
                setValue: value =>
                {
                    _config.DefaultHorseConfig.SaddleColor = value;
                    _ConfigManager.InitializeOrUpdateHorseConfigs();
                },
                allowedValues: ModConstants.ValidSaddleColors.ToArray()
            );

            // Add links to farm pages
            foreach (var farmName in _config.HorseConfigs.Keys.Where(k => k != "DefaultHorseConfig"))
            {
                api.AddPageLink(
                    mod: _modManifest,
                    pageId: farmName,
                    text: () => farmName,
                    tooltip: () => $"Configure horses on {farmName}"
                );
            }
        }

        private void RegisterHorsePages(IGenericModConfigMenuApi api)
        {
            foreach (var farmEntry in _config.HorseConfigs)
            {
                string farmName = farmEntry.Key;
                if (farmName == "DefaultHorseConfig") continue;

                api.AddPage(mod: _modManifest, pageId: farmName, pageTitle: () => $"{farmName} Horses");

                foreach (var horseEntry in farmEntry.Value)
                {
                    string horseName = horseEntry.Key;
                    var horseConfig = horseEntry.Value;

                    api.AddSectionTitle(mod: _modManifest, text: () => $"Settings for {horseName}");

                    // Load the texture from the specified path
                    Texture2D texture = _helper.ModContent.Load<Texture2D>($"assets/Generated/{farmName}_{horseName}.png");

                    // Add the image to the GMCM menu
                    api.AddImage(
                        mod: _modManifest,
                        texture: () => texture,
                        texturePixelArea: new Microsoft.Xna.Framework.Rectangle(0, 32, 32, 32),
                        scale: 2
                    );

                    api.AddTextOption(
                        mod: _modManifest,
                        name: () => "Base Horse Skin",
                        getValue: () => horseConfig.BaseSkin,
                        setValue: value =>
                        {
                            horseConfig.BaseSkin = value;
                            _ConfigManager.InitializeOrUpdateHorseConfigs();
                        },
                        allowedValues: ModConstants.ValidBaseSkins.ToArray()
                    );

                    api.AddTextOption(
                        mod: _modManifest,
                        name: () => "Pattern",
                        getValue: () => horseConfig.Pattern,
                        setValue: value =>
                        {
                            horseConfig.Pattern = value;
                            _ConfigManager.InitializeOrUpdateHorseConfigs();
                        },
                        allowedValues: ModConstants.ValidPatterns.ToArray()
                    );

                    api.AddTextOption(
                        mod: _modManifest,
                        name: () => "Hair",
                        getValue: () => horseConfig.Hair,
                        setValue: value =>
                        {
                            horseConfig.Hair = value;
                            _ConfigManager.InitializeOrUpdateHorseConfigs();
                        },
                        allowedValues: ModConstants.ValidHairOptions.ToArray()
                    );

                    api.AddTextOption(
                        mod: _modManifest,
                        name: () => "Saddle Color",
                        getValue: () => horseConfig.SaddleColor,
                        setValue: value =>
                        {
                            horseConfig.SaddleColor = value;
                            _ConfigManager.InitializeOrUpdateHorseConfigs();
                        },
                        allowedValues: ModConstants.ValidSaddleColors.ToArray()
                    );
                }
            }
        }
    }
}
