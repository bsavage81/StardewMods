using System;
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
        HorseManager horseManager,
        MultiplayerHandler multiplayerHandler)
    {
        private readonly IManifest _modManifest = modManifest;
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly ConfigManager _configManager = ConfigManager;
        private readonly HorseManager _horseManager = horseManager;
        private readonly MultiplayerHandler _multiplayerHandler = multiplayerHandler;

        public ModConfig _config = config;

        public void SetupModConfigMenu()
        {
            var api = this._helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api == null)
            {
                this._monitor.Log("Generic Mod Config Menu not found. Skipping configuration menu setup.", LogLevel.Warn);
                return;
            }

            // Add event listener for when GMCM is opened
            this._helper.Events.Display.MenuChanged += (sender, e) =>
            {
                if (e.NewMenu?.GetType().FullName == "GenericModConfigMenu.Framework.ModConfigMenu")
                {
                    this._monitor.Log("GMCM opened. Re-registering menu to include updated horse list.", LogLevel.Debug);

                    // Re-register the menu
                    this.RegisterModConfigMenu(api);
                }
            };

            // Register the base menu
            this.RegisterModConfigMenu(api);

        }

        private void RegisterModConfigMenu(IGenericModConfigMenuApi api)
        {
            api.Register(
                mod: this._modManifest,
                reset: () =>
                {
                    this._config = new ModConfig();
                    this._helper.WriteConfig(this._config);
                    this._configManager.InitializeOrUpdateHorseConfigs();
                    this._horseManager.ProcessHorses(); // Apply changes after saving
                },
                save: () =>
                {
                    this._helper.WriteConfig(this._config);
                    if (Context.IsMainPlayer)
                    {
                        this._multiplayerHandler.BroadcastHorseConfig(); // Broadcast updated config to all players
                        this._monitor.Log("Broadcasted updated horse configurations to connected players.", LogLevel.Info);
                    }
                    else
                    {
                        this._multiplayerHandler.NotifyHostOfConfigChange(); // Notify host about the config change
                        this._monitor.Log("Sent updated horse configurations to the host.", LogLevel.Info);
                    }

                    this._configManager.InitializeOrUpdateHorseConfigs();
                    this._horseManager.ProcessHorses(); // Apply changes after saving
                }
            );

            // Register default settings
            this.RegisterDefaultSettings(api);

            // Register horse-specific pages
            this.RegisterHorsePages(api);

            // Add dynamic update listener
            api.OnFieldChanged(this._modManifest, (fieldId, newValue) =>
            {
                this._helper.WriteConfig(this._config);
                this._configManager.InitializeOrUpdateHorseConfigs();
                this._horseManager.ProcessHorses(); // Apply changes after saving
            });
        }

        private void RegisterDefaultSettings(IGenericModConfigMenuApi api)
        {
            api.AddSectionTitle(mod: this._modManifest, text: () => "Default Horse Settings");

            // Load the texture from the specified path
            Texture2D defaulttexture = this._helper.ModContent.Load<Texture2D>("assets/Generated/DefaultFarm_DefaultHorse.png");

            // Add the image to the GMCM menu
            api.AddImage(
                mod: this._modManifest,
                texture: () => defaulttexture, // Provide the loaded texture
                texturePixelArea: new Microsoft.Xna.Framework.Rectangle(0, 32, 32, 32), // Specify the 32x32 area starting at (0, 32)
                scale: 4 // Scale the image for display
            );

            api.AddTextOption(
                mod: this._modManifest,
                name: () => "Default Base Horse Skin",
                getValue: () => this._config.DefaultHorseConfig.BaseSkin,
                setValue: value =>
                {
                    this._config.DefaultHorseConfig.BaseSkin = value;
                    this._horseManager.ProcessHorses();
                },
                allowedValues: ModConstants.ValidBaseSkins.ToArray()
            );

            api.AddTextOption(
                mod: this._modManifest,
                name: () => "Default Pattern",
                getValue: () => this._config.DefaultHorseConfig.Pattern,
                setValue: value =>
                {
                    this._config.DefaultHorseConfig.Pattern = value;
                    this._horseManager.ProcessHorses();
                },
                allowedValues: ModConstants.ValidPatterns.ToArray()
            );

            api.AddTextOption(
                mod: this._modManifest,
                name: () => "Default Hair",
                getValue: () => this._config.DefaultHorseConfig.Hair,
                setValue: value =>
                {
                    this._config.DefaultHorseConfig.Hair = value;
                    this._horseManager.ProcessHorses();
                },
                allowedValues: ModConstants.ValidHairOptions.ToArray()
            );

            api.AddTextOption(
                mod: this._modManifest,
                name: () => "Default Saddle Color",
                getValue: () => this._config.DefaultHorseConfig.SaddleColor,
                setValue: value =>
                {
                    this._config.DefaultHorseConfig.SaddleColor = value;
                    this._horseManager.ProcessHorses();
                },
                allowedValues: ModConstants.ValidSaddleColors.ToArray()
            );

            // Add links to farm pages
            foreach (var farmName in this._config.HorseConfigs.Keys.Where(k => k != "DefaultFarm"))
            {
                api.AddPageLink(
                    mod: this._modManifest,
                    pageId: farmName,
                    text: () => farmName,
                    tooltip: () => $"Configure horses on {farmName}"
                );
            }
        }

        private void RegisterHorsePages(IGenericModConfigMenuApi api)
        {
            foreach (var farmEntry in this._config.HorseConfigs)
            {
                string farmName = farmEntry.Key;
                if (farmName == "DefaultFarm") continue;

                api.AddPage(mod: this._modManifest, pageId: farmName, pageTitle: () => $"{farmName} Horses");

                foreach (var horseEntry in farmEntry.Value)
                {
                    string horseName = horseEntry.Key;
                    var horseConfig = horseEntry.Value;

                    api.AddSectionTitle(mod: this._modManifest, text: () => $"Settings for {horseName}");

                    // Try to load the texture, fall back to default if not found
                    Texture2D texture;
                    try
                    {
                        texture = this._helper.ModContent.Load<Texture2D>($"assets/Generated/{farmName}_{horseName}.png");
                    }
                    catch (Exception ex)
                    {
                        this._monitor.Log($"Failed to load texture for {farmName}_{horseName}.png. Falling back to default texture. Error: {ex.Message}", LogLevel.Warn);
                        texture = this._helper.ModContent.Load<Texture2D>("assets/Generated/DefaultFarm_DefaultHorse.png");
                    }


                    // Add the image to the GMCM menu
                    api.AddImage(
                        mod: this._modManifest,
                        texture: () => texture, // Provide the loaded texture
                        texturePixelArea: new Microsoft.Xna.Framework.Rectangle(0, 32, 32, 32), // Specify the 32x32 area starting at (0, 32)
                        scale: 2 // Scale the image for display
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Base Horse Skin",
                        getValue: () => horseConfig.BaseSkin,
                        setValue: value =>
                        {
                            horseConfig.BaseSkin = value;
                            this._horseManager.ProcessHorses();
                        },
                        allowedValues: ModConstants.ValidBaseSkins.ToArray()
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Pattern",
                        getValue: () => horseConfig.Pattern,
                        setValue: value =>
                        {
                            horseConfig.Pattern = value;
                            this._horseManager.ProcessHorses();
                        },
                        allowedValues: ModConstants.ValidPatterns.ToArray()
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Hair",
                        getValue: () => horseConfig.Hair,
                        setValue: value =>
                        {
                            horseConfig.Hair = value;
                            this._horseManager.ProcessHorses();
                        },
                        allowedValues: ModConstants.ValidHairOptions.ToArray()
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Saddle Color",
                        getValue: () => horseConfig.SaddleColor,
                        setValue: value =>
                        {
                            horseConfig.SaddleColor = value;
                            this._horseManager.ProcessHorses();
                        },
                        allowedValues: ModConstants.ValidSaddleColors.ToArray()
                    );
                }
            }
        }
    }
}
