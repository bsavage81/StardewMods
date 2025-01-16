using System;
using System.Linq;
using AdvancedHorses.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace AdvancedHorses.Managers
{
    public class GMCMManager(
        IManifest modManifest,
        IModHelper helper,
        IMonitor monitor,
        ModConfig config,
        ConfigManager ConfigManager,
        TextureManager compositeGenerator,
        MultiplayerManager multiplayerHandler)
    {
        private readonly IManifest _modManifest = modManifest;
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly ConfigManager _configManager = ConfigManager;
        private readonly TextureManager _compositeGenerator = compositeGenerator;
        private readonly MultiplayerManager _multiplayerHandler = multiplayerHandler;

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
                    this._configManager.InitializeOrUpdateHorseConfigs();
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
                    this._helper.WriteConfig(_config);
                    this._configManager.InitializeOrUpdateHorseConfigs();
                },
                save: () =>
                {
                    this._helper.WriteConfig(_config);
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
                }
            );

            // Register horse-specific pages
            this.RegisterHorsePages(api);

            // Add dynamic update listener
            api.OnFieldChanged (_modManifest, (fieldId, newValue) =>
            {
                this._helper.WriteConfig(_config);
                this._configManager.InitializeOrUpdateHorseConfigs();
            });
        }

        private void RegisterHorsePages(IGenericModConfigMenuApi api)
        {
            // Add links to farm pages
            foreach (var farmEntry in this._config.HorseConfigs)
            {
                string farmName = farmEntry.Key;

                api.AddPageLink(
                    mod: this._modManifest,
                    pageId: farmName,
                    text: () => farmName,
                    tooltip: () => $"Configure horses on {farmName}"
                );

                foreach (var horseEntry in farmEntry.Value)
                {
                    string horseName = horseEntry.Key;
                    var horseConfig = horseEntry.Value;

                    // Add dynamic image preview
                    api.AddComplexOption(
                        mod: this._modManifest,
                        name: () => $"{horseName}",
                        draw: (SpriteBatch spriteBatch, Vector2 position) =>
                        {
                            // Define the texture path based on current config
                            string texturePath = $"assets/Generated/{farmName}_{horseName}.png";

                            try
                            {
                                Texture2D texture = this._helper.ModContent.Load<Texture2D>(texturePath);

                                // Define the source rectangle (0, 32, 32, 32)
                                Rectangle sourceRectangle = new Rectangle(0, 32, 32, 32);

                                // Define the destination rectangle for drawing
                                Vector2 scale = new Vector2(4f, 4f); // Optional scaling
                                Rectangle destinationRectangle = new Rectangle(
                                    (int)position.X - 100,
                                    (int)position.Y,
                                    sourceRectangle.Width * (int)scale.X,
                                    sourceRectangle.Height * (int)scale.Y
                                );

                                // Draw the texture
                                spriteBatch.Draw(
                                    texture,
                                    destinationRectangle,
                                    sourceRectangle,
                                    Color.White
                                );
                            }
                            catch (Exception ex)
                            {
                                this._monitor.Log($"Failed to load texture at {texturePath}: {ex.Message}", LogLevel.Warn);
                            }
                        },
                        height: () => 128, // Define height for the option
                        tooltip: () => $"{horseName}",
                        fieldId: "generalhorsepreview"
                    );
                }
            }

            foreach (var farmEntry in this._config.HorseConfigs)
            {
                string farmName = farmEntry.Key;

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


                    // Add dynamic image preview
                    api.AddComplexOption(
                        mod: this._modManifest,
                        name: () => "Preview",
                        draw: (SpriteBatch spriteBatch, Vector2 position) =>
                        {
                            // Define the texture path based on current config
                            string texturePath = $"assets/Generated/{farmName}_{horseName}.png";

                            try
                            {
                                Texture2D texture = this._helper.ModContent.Load<Texture2D>(texturePath);

                                // Define the source rectangle (0, 32, 32, 32)
                                Rectangle sourceRectangle = new Rectangle(0, 32, 32, 32);

                                // Define the destination rectangle for drawing
                                Vector2 scale = new Vector2(4f, 4f); // Optional scaling
                                Rectangle destinationRectangle = new Rectangle(
                                    (int)position.X,
                                    (int)position.Y,
                                    sourceRectangle.Width * (int)scale.X,
                                    sourceRectangle.Height * (int)scale.Y
                                );

                                // Draw the texture
                                spriteBatch.Draw(
                                    texture,
                                    destinationRectangle,
                                    sourceRectangle,
                                    Color.White
                                );
                            }
                            catch (Exception ex)
                            {
                                this._monitor.Log($"Failed to load texture at {texturePath}: {ex.Message}", LogLevel.Warn);
                            }
                        },
                        height: () => 128, // Define height for the option
                        tooltip: () => "Dynamic preview of the selected horse skin.",
                        fieldId: "horsepreview"
                    );


                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Base Horse Skin",
                        getValue: () => horseConfig.BaseSkin,
                        setValue: value =>
                        {
                            horseConfig.BaseSkin = value;
                            this._helper.WriteConfig(_config);
                        },
                        allowedValues: ModConstants.ValidBaseSkins.ToArray(),
                        fieldId: "baseoption"
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Pattern 1",
                        getValue: () => horseConfig.Pattern1,
                        setValue: value =>
                        {
                            horseConfig.Pattern1 = value;
                            this._helper.WriteConfig(_config);
                        },
                        allowedValues: ModConstants.ValidPatterns.ToArray(),
                        fieldId: "pattern1option"
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Pattern 2",
                        getValue: () => horseConfig.Pattern2,
                        setValue: value =>
                        {
                            horseConfig.Pattern2 = value;
                            this._helper.WriteConfig(_config);
                        },
                        allowedValues: ModConstants.ValidPatterns.ToArray(),
                        fieldId: "pattern2option"
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Pattern 3",
                        getValue: () => horseConfig.Pattern3,
                        setValue: value =>
                        {
                            horseConfig.Pattern3 = value;
                            this._helper.WriteConfig(_config);
                        },
                        allowedValues: ModConstants.ValidPatterns.ToArray(),
                        fieldId: "pattern3option"
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Hair",
                        getValue: () => horseConfig.Hair,
                        setValue: value =>
                        {
                            horseConfig.Hair = value;
                            this._helper.WriteConfig(_config);
                        },
                        allowedValues: ModConstants.ValidHairOptions.ToArray(),
                        fieldId: "hairoption"
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Saddle Color",
                        getValue: () => horseConfig.SaddleColor,
                        setValue: value =>
                        {
                            horseConfig.SaddleColor = value;
                            this._helper.WriteConfig(_config);
                        },
                        allowedValues: ModConstants.ValidSaddleColors.ToArray(),
                        fieldId: "saddleoption"
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Accessory 1",
                        getValue: () => horseConfig.Accessory1,
                        setValue: value =>
                        {
                            horseConfig.Accessory1 = value;
                            this._helper.WriteConfig(_config);
                        },
                        allowedValues: ModConstants.ValidAccessories.ToArray(),
                        fieldId: "accessory1option"
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Accessory 2",
                        getValue: () => horseConfig.Accessory2,
                        setValue: value =>
                        {
                            horseConfig.Accessory2 = value;
                            this._helper.WriteConfig(_config);
                        },
                        allowedValues: ModConstants.ValidAccessories.ToArray(),
                        fieldId: "accessory2option"
                    );

                    api.AddTextOption(
                        mod: this._modManifest,
                        name: () => "Accessory 3",
                        getValue: () => horseConfig.Accessory3,
                        setValue: value =>
                        {
                            horseConfig.Accessory3 = value;
                            this._helper.WriteConfig(_config);
                        },
                        allowedValues: ModConstants.ValidAccessories.ToArray(),
                        fieldId: "accessory3option"
                    );
                }
            }
        }
    }
}
