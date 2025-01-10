using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using Newtonsoft.Json;
using StardewValley.Locations;
using StardewValley.Extensions;
using StardewValley.GameData.HomeRenovations;
using xTile.Dimensions;

namespace DifferentHorseSkins
{
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private static IMonitor SMonitor;
        private static IModHelper SHelper;

        public override void Entry(IModHelper helper)
        {
            SMonitor = this.Monitor;
            SHelper = this.Helper;

            this.Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();

            // Load dynamic values
            this.LoadDynamicAllowedValues();

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
            helper.Events.Multiplayer.ModMessageReceived += this.OnMessageReceived;


            this.Helper.ConsoleCommands.Add("refresh_horses", "Refresh all horse configurations and appearances.", (command, args) =>
            {
                InitializeOrUpdateHorseConfigs(); // Ensure configs are up-to-date
                ApplyHorseConfigs(); // Reapply all configurations
                this.Monitor.Log("Horse configurations refreshed.", LogLevel.Info);
            });

        }

        private void LoadDynamicAllowedValues()
        {
            // Scan for horse skins
            var horseSkins = this.GetValidFiles("assets/Base", "png")
                .Select(name => name.Replace("Base_", ""))
                .ToList();
            ModConstants.ValidBaseSkins = horseSkins.Any() ? horseSkins : new List<string>(ModConstants.DefaultBaseSkins);

            var patterns = this.GetValidFiles("assets/Patterns", "png")
                .Select(name => name.Replace("Pattern_", ""))
                .ToList();
            ModConstants.ValidPatterns = patterns.Any() ? patterns : new List<string>(ModConstants.DefaultPatternOptions);

            // Scan for hair overlays
            var hairOptions = this.GetValidFiles("assets/Hair", "png")
                .Select(name => name.Replace("Hair_", ""))
                .ToList();
            ModConstants.ValidHairOptions = hairOptions.Any() ? hairOptions : new List<string>(ModConstants.DefaultHairOptions);

            // Scan for saddle colors
            var saddleColors = this.GetValidFiles("assets/Saddles", "png")
                .Select(name => name.Replace("Saddle_", ""))
                .ToList();
            ModConstants.ValidSaddleColors = saddleColors.Any() ? saddleColors : new List<string>(ModConstants.DefaultSaddleColors);

            this.Monitor.Log("Dynamic allowed values loaded successfully.", LogLevel.Debug);
        }

        private List<string> GetValidFiles(string folderPath, string extension)
        {
            string fullPath = Path.Combine(SHelper.DirectoryPath, folderPath);
            if (!Directory.Exists(fullPath))
            {
                this.Monitor.Log($"Directory not found: {fullPath}", LogLevel.Warn);
                return new List<string>();
            }

            return Directory
                .EnumerateFiles(fullPath, $"*.{extension}")
                .Select(file => Path.GetFileNameWithoutExtension(file))
                .ToList();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            this.SetupModConfigMenu();
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.InitializeOrUpdateHorseConfigs();
            this.ApplyHorseConfigs();

            if (Context.IsMainPlayer)
            {
                this.BroadcastHorseConfig(); // Broadcast updated config to all players
                this.Monitor.Log("Broadcasted updated horse configurations to connected players.", LogLevel.Info);
            }
            else
            {
                this.NotifyHostOfConfigChange(); // Notify host about the config change
                this.Monitor.Log("Sent updated horse configurations to the host.", LogLevel.Info);
            }
        }

        private void OnPeerConnected(object sender, PeerConnectedEventArgs e)
        {
            this.InitializeOrUpdateHorseConfigs();
            this.ApplyHorseConfigs();

            if (Context.IsMainPlayer)
            {
                this.BroadcastHorseConfig(); // Broadcast updated config to all players
                this.Monitor.Log("Broadcasted updated horse configurations to connected players.", LogLevel.Info);
            }
            else
            {
                this.NotifyHostOfConfigChange(); // Notify host about the config change
                this.Monitor.Log("Sent updated horse configurations to the host.", LogLevel.Info);
            }
        }

        private void SetupModConfigMenu()
        {
            var api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api == null)
            {
                this.Monitor.Log("Generic Mod Config Menu not found. Skipping configuration menu setup.", LogLevel.Warn);
                return;
            }

            // Add event listener for when GMCM is opened
            this.Helper.Events.Display.MenuChanged += (sender, e) =>
            {
                if (e.NewMenu?.GetType().FullName == "GenericModConfigMenu.Framework.ModConfigMenu")
                {
                    this.Monitor.Log("GMCM opened. Re-registering menu to include updated horse list.", LogLevel.Debug);

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
                mod: this.ModManifest,
                reset: () =>
                {
                    this.Config = new ModConfig();
                    this.Helper.WriteConfig(this.Config);
                    this.InitializeOrUpdateHorseConfigs();
                    this.ApplyHorseConfigs(); // Apply changes after saving
                },
                save: () =>
                {
                    this.Helper.WriteConfig(this.Config);
                    if (Context.IsMainPlayer)
                    {
                        this.BroadcastHorseConfig(); // Broadcast updated config to all players
                        this.Monitor.Log("Broadcasted updated horse configurations to connected players.", LogLevel.Info);
                    }
                    else
                    {
                        this.NotifyHostOfConfigChange(); // Notify host about the config change
                        this.Monitor.Log("Sent updated horse configurations to the host.", LogLevel.Info);
                    }

                    this.InitializeOrUpdateHorseConfigs();
                    this.ApplyHorseConfigs(); // Apply changes after saving
                }
            );

            // Register default settings
            this.RegisterDefaultSettings(api);

            // Register horse-specific pages
            this.RegisterHorsePages(api);

            // Add dynamic update listener
            api.OnFieldChanged(this.ModManifest, (fieldId, newValue) =>
            {
                this.Helper.WriteConfig(this.Config);
                this.InitializeOrUpdateHorseConfigs();
                this.ApplyHorseConfigs(); // Apply changes after saving
            });
        }

        private void RegisterDefaultSettings(IGenericModConfigMenuApi api)
        {
            api.AddSectionTitle(mod: this.ModManifest, text: () => "Default Horse Settings");

            // Load the texture from the specified path
            Texture2D defaulttexture = this.Helper.ModContent.Load<Texture2D>("assets/Generated/default.png");

            // Add the image to the GMCM menu
            api.AddImage(
                mod: this.ModManifest,
                texture: () => defaulttexture, // Provide the loaded texture
                texturePixelArea: new Microsoft.Xna.Framework.Rectangle(0, 32, 32, 32), // Specify the 32x32 area starting at (0, 32)
                scale: 4 // Scale the image for display
            );

            api.AddTextOption(
                mod: this.ModManifest,
                name: () => "Default Base Horse Skin",
                getValue: () => this.Config.DefaultHorseConfig.BaseSkin,
                setValue: value =>
                {
                    this.Config.DefaultHorseConfig.BaseSkin = value;
                    this.ApplyHorseConfigs();
                },
                allowedValues: ModConstants.ValidBaseSkins.ToArray()
            );

            api.AddTextOption(
                mod: this.ModManifest,
                name: () => "Default Pattern",
                getValue: () => this.Config.DefaultHorseConfig.Pattern,
                setValue: value =>
                {
                    this.Config.DefaultHorseConfig.Pattern = value;
                    this.ApplyHorseConfigs();
                },
                allowedValues: ModConstants.ValidPatterns.ToArray()
            );

            api.AddTextOption(
                mod: this.ModManifest,
                name: () => "Default Hair",
                getValue: () => this.Config.DefaultHorseConfig.Hair,
                setValue: value =>
                {
                    this.Config.DefaultHorseConfig.Hair = value;
                    this.ApplyHorseConfigs();
                },
                allowedValues: ModConstants.ValidHairOptions.ToArray()
            );

            api.AddTextOption(
                mod: this.ModManifest,
                name: () => "Default Saddle Color",
                getValue: () => this.Config.DefaultHorseConfig.SaddleColor,
                setValue: value =>
                {
                    this.Config.DefaultHorseConfig.SaddleColor = value;
                    this.ApplyHorseConfigs();
                },
                allowedValues: ModConstants.ValidSaddleColors.ToArray()
            );

            // Add links to farm pages
            foreach (var farmName in this.Config.HorseConfigs.Keys.Where(k => k != "DefaultHorseConfig"))
            {
                api.AddPageLink(
                    mod: this.ModManifest,
                    pageId: farmName,
                    text: () => farmName,
                    tooltip: () => $"Configure horses on {farmName}"
                );
            }
        }

        private void RegisterHorsePages(IGenericModConfigMenuApi api)
        {
            foreach (var farmEntry in this.Config.HorseConfigs)
            {
                string farmName = farmEntry.Key;
                if (farmName == "DefaultHorseConfig") continue;

                api.AddPage(mod: this.ModManifest, pageId: farmName, pageTitle: () => $"{farmName} Horses");

                foreach (var horseEntry in farmEntry.Value)
                {
                    string horseName = horseEntry.Key;
                    var horseConfig = horseEntry.Value;

                    api.AddSectionTitle(mod: this.ModManifest, text: () => $"Settings for {horseName}");

                    // Load the texture from the specified path
                    Texture2D texture = this.Helper.ModContent.Load<Texture2D>($"assets/Generated/{farmName}_{horseName}.png");

                    // Add the image to the GMCM menu
                    api.AddImage(
                        mod: this.ModManifest,
                        texture: () => texture, // Provide the loaded texture
                        texturePixelArea: new Microsoft.Xna.Framework.Rectangle(0, 32, 32, 32), // Specify the 32x32 area starting at (0, 32)
                        scale: 2 // Scale the image for display
                    );

                    api.AddTextOption(
                        mod: this.ModManifest,
                        name: () => "Base Horse Skin",
                        getValue: () => horseConfig.BaseSkin,
                        setValue: value =>
                        {
                            horseConfig.BaseSkin = value;
                            this.ApplyHorseConfigs();
                        },
                        allowedValues: ModConstants.ValidBaseSkins.ToArray()
                    );

                    api.AddTextOption(
                        mod: this.ModManifest,
                        name: () => "Pattern",
                        getValue: () => horseConfig.Pattern,
                        setValue: value =>
                        {
                            horseConfig.Pattern = value;
                            this.ApplyHorseConfigs();
                        },
                        allowedValues: ModConstants.ValidPatterns.ToArray()
                    );

                    api.AddTextOption(
                        mod: this.ModManifest,
                        name: () => "Hair",
                        getValue: () => horseConfig.Hair,
                        setValue: value =>
                        {
                            horseConfig.Hair = value;
                            this.ApplyHorseConfigs();
                        },
                        allowedValues: ModConstants.ValidHairOptions.ToArray()
                    );

                    api.AddTextOption(
                        mod: this.ModManifest,
                        name: () => "Saddle Color",
                        getValue: () => horseConfig.SaddleColor,
                        setValue: value =>
                        {
                            horseConfig.SaddleColor = value;
                            this.ApplyHorseConfigs();
                        },
                        allowedValues: ModConstants.ValidSaddleColors.ToArray()
                    );
                }
            }
        }

        private string GetCurrentFarmName()
        {
            if (Game1.player?.farmName?.Value != null)
                return Game1.player.farmName.Value;

            return DeriveFarmAndHorseNamesFromAssets().FarmName;
        }

        private string GetCurrentHorseName()
        {
            return DeriveFarmAndHorseNamesFromAssets().HorseName;
        }


        private void InitializeOrUpdateHorseConfigs()
        {
            string farmName = GetCurrentFarmName();

            if (!this.Config.HorseConfigs.ContainsKey(farmName))
            {
                this.Config.HorseConfigs[farmName] = new Dictionary<string, HorseConfig>();
            }

            foreach (var horse in this.GetAllHorses())
            {
                string horseName = horse.Name;

                if (!this.Config.HorseConfigs[farmName].ContainsKey(horseName))
                {
                    this.Config.HorseConfigs[farmName][horseName] = new HorseConfig
                    {
                        BaseSkin = this.Config.DefaultHorseConfig.BaseSkin,
                        Hair = this.Config.DefaultHorseConfig.Hair,
                        SaddleColor = this.Config.DefaultHorseConfig.SaddleColor,
                        MenuIcon = this.Config.DefaultHorseConfig.MenuIcon
                    };

                    this.Monitor.Log($"Added config for horse '{horseName}' on farm '{farmName}'.", LogLevel.Debug);
                }

                // Generate composite image
                string baseSpritePath = GetAssetPath("assets/Base", "Base_", this.Config.HorseConfigs[farmName][horseName].BaseSkin);
                string patternOverlayPath = GetAssetPath("assets/Patterns", "Pattern_", this.Config.HorseConfigs[farmName][horseName].Pattern);
                string hairOverlayPath = GetAssetPath("assets/Hair", "Hair_", this.Config.HorseConfigs[farmName][horseName].Hair);
                string saddleOverlayPath = GetAssetPath("assets/Saddles", "Saddle_", this.Config.HorseConfigs[farmName][horseName].SaddleColor);
                string outputPath = Path.Combine(SHelper.DirectoryPath, $"assets/Generated/{farmName}_{horseName}.png");
                this.GenerateAndSaveCompositeIcon(horseName, farmName, baseSpritePath, patternOverlayPath, hairOverlayPath, saddleOverlayPath, outputPath);
            }

            this.Helper.WriteConfig(this.Config);
        }

        private string GetAssetPath(string folder, string prefix, string name)
        {
            // Check for prefixed file
            string prefixedPath = Path.Combine(SHelper.DirectoryPath, folder, $"{prefix}{name}.png");
            if (File.Exists(prefixedPath))
                return prefixedPath;

            // Check for non-prefixed file
            string nonPrefixedPath = Path.Combine(SHelper.DirectoryPath, folder, $"{name}.png");
            if (File.Exists(nonPrefixedPath))
                return nonPrefixedPath;

            // Log a warning if the file is not found
            this.Monitor.Log($"Asset not found in folder '{folder}' for name '{name}' with or without prefix '{prefix}'.", LogLevel.Warn);
            return null;
        }

        private void ApplyHorseConfigs()
        {
            string farmName = GetCurrentFarmName();

            if (!this.Config.HorseConfigs.TryGetValue(farmName, out var farmHorses))
            {
                this.Monitor.Log($"No configuration found for farm '{farmName}'. Skipping.", LogLevel.Warn);
                return;
            }

            foreach (var horse in this.GetAllHorses())
            {
                string horseName = horse.Name;

                if (farmHorses.TryGetValue(horseName, out var horseConfig))
                {
                    string relativePath = $"assets/Generated/{farmName}_{horseName}.png";
                    string fullPath = Path.Combine(SHelper.DirectoryPath, relativePath);

                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            // Load and apply texture
                            string assetName = this.Helper.ModContent.GetInternalAssetName(relativePath).BaseName;

                            // Debug log paths
                            this.Monitor.Log($"Full path: {fullPath}", LogLevel.Debug);
                            this.Monitor.Log($"Asset name: {assetName}", LogLevel.Debug);

                            horse.Sprite.overrideTextureName = assetName;
                            horse.Sprite.LoadTexture(assetName, syncTextureName: true);

                            
                            // Invalidate asset cache to ensure textures are reloaded
                            this.Helper.GameContent.InvalidateCache(asset => asset.Name.BaseName.EqualsIgnoreCase("Animals/horse"));
                            this.Helper.GameContent.InvalidateCache(asset => asset.Name.BaseName.EqualsIgnoreCase(assetName));
                            this.Monitor.Log($"Invalidating cache for horse: {horseName}", LogLevel.Debug);

                            this.Monitor.Log($"Refreshed texture for horse '{horseName}' using '{relativePath}'.", LogLevel.Info);
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"Failed to load or apply texture for horse '{horseName}'. Error: {ex.Message}", LogLevel.Error);
                        }
                    }
                    else
                    {
                        this.Monitor.Log($"Generated texture not found for horse '{horseName}' at path '{fullPath}'. Skipping.", LogLevel.Warn);
                    }
                }
                else
                {
                    this.Monitor.Log($"No configuration found for horse '{horseName}' on farm '{farmName}'. Skipping.", LogLevel.Warn);
                }
            }
        }


        public class HorseConfigSyncMessage
        {
            public string FarmName { get; set; }
            public Dictionary<string, HorseConfig> HorseConfigs { get; set; }
        }

        private void BroadcastHorseConfig()
        {
            if (!Context.IsMainPlayer) return;

            string farmName = Game1.player.farmName.Value ?? "Unknown";
            if (this.Config.HorseConfigs.TryGetValue(farmName, out var horseConfigs))
            {
                var message = new HorseConfigSyncMessage
                {
                    FarmName = farmName,
                    HorseConfigs = horseConfigs
                };

                this.Helper.Multiplayer.SendMessage(
                    message: message,
                    messageType: "HorseConfigSync",
                    modIDs: new[] { this.ModManifest.UniqueID }
                );

                this.Monitor.Log($"Broadcasted horse configurations for farm '{farmName}' to connected players.", LogLevel.Info);
            }

            this.InitializeOrUpdateHorseConfigs();
            this.ApplyHorseConfigs();

        }

        private void OnMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != this.ModManifest.UniqueID || e.Type != "HorseConfigSync") return;

            var message = e.ReadAs<HorseConfigSyncMessage>();
            this.Monitor.Log($"Received horse configuration for farm '{message.FarmName}' from host.", LogLevel.Info);

            this.Config.HorseConfigs[message.FarmName] = message.HorseConfigs;
            this.InitializeOrUpdateHorseConfigs();
            this.ApplyHorseConfigs();
        }

        private void NotifyHostOfConfigChange()
        {
            if (Context.IsMainPlayer) return;

            string farmName = Game1.player.farmName.Value ?? "Unknown";
            if (this.Config.HorseConfigs.TryGetValue(farmName, out var horseConfigs))
            {
                var message = new HorseConfigSyncMessage
                {
                    FarmName = farmName,
                    HorseConfigs = horseConfigs
                };

                this.Helper.Multiplayer.SendMessage(
                    message: message,
                    messageType: "HorseConfigUpdate",
                    modIDs: new[] { this.ModManifest.UniqueID }
                );

                this.Monitor.Log($"Sent updated horse configurations for farm '{farmName}' to the host.", LogLevel.Info);
            }
        }

        private void OnPlayerConfigUpdateReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (!Context.IsMainPlayer || e.FromModID != this.ModManifest.UniqueID || e.Type != "HorseConfigUpdate") return;

            var message = e.ReadAs<HorseConfigSyncMessage>();
            this.Monitor.Log($"Received updated horse configuration for farm '{message.FarmName}' from player.", LogLevel.Info);

            this.Config.HorseConfigs[message.FarmName] = message.HorseConfigs;
            this.BroadcastHorseConfig(); // Re-broadcast to all players
        }

        private void GenerateAndSaveCompositeIcon(string horseName, string farmName, string baseSpritePath, string patternOverlayPath, string hairOverlayPath, string saddleOverlayPath, string outputPath)
        {
            try
            {
                // Ensure the output directory exists
                string directoryPath = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    this.Monitor.Log($"Created directory: {directoryPath}", LogLevel.Debug);
                }

                // Load base sprite, hair overlay, and saddle overlay
                Texture2D baseSprite = SHelper.ModContent.Load<Texture2D>(baseSpritePath);
                Texture2D patternOverlay = File.Exists(patternOverlayPath) ? SHelper.ModContent.Load<Texture2D>(patternOverlayPath) : null;
                Texture2D hairOverlay = File.Exists(hairOverlayPath) ? SHelper.ModContent.Load<Texture2D>(hairOverlayPath) : null;
                Texture2D saddleOverlay = File.Exists(saddleOverlayPath) ? SHelper.ModContent.Load<Texture2D>(saddleOverlayPath) : null;
                

                // Generate composite texture
                Texture2D composite = new Texture2D(Game1.graphics.GraphicsDevice, baseSprite.Width, baseSprite.Height);
                Color[] baseData = new Color[baseSprite.Width * baseSprite.Height];
                baseSprite.GetData(baseData);

                // Apply pattern overlay
                if (patternOverlay != null)
                {
                    Color[] patternData = new Color[patternOverlay.Width * patternOverlay.Height];
                    patternOverlay.GetData(patternData);
                    for (int i = 0; i < baseData.Length; i++)
                    {
                        if (patternData[i].A > 0) // Apply non-transparent pixels
                            baseData[i] = patternData[i];
                    }
                }

                // Apply hair overlay
                if (hairOverlay != null)
                {
                    Color[] hairData = new Color[hairOverlay.Width * hairOverlay.Height];
                    hairOverlay.GetData(hairData);
                    for (int i = 0; i < baseData.Length; i++)
                    {
                        if (hairData[i].A > 0) // Apply non-transparent pixels
                            baseData[i] = hairData[i];
                    }
                }

                // Apply saddle overlay
                if (saddleOverlay != null)
                {
                    Color[] saddleData = new Color[saddleOverlay.Width * saddleOverlay.Height];
                    saddleOverlay.GetData(saddleData);
                    for (int i = 0; i < baseData.Length; i++)
                    {
                        if (saddleData[i].A > 0) // Apply non-transparent pixels
                            baseData[i] = saddleData[i];
                    }
                }

                composite.SetData(baseData);

                // Save the composite texture
                using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    composite.SaveAsPng(stream, composite.Width, composite.Height);
                }

                this.Monitor.Log($"Menu icon generated and saved to: {outputPath}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Failed to generate or save menu icon for horse '{horseName}'. Error: {ex.Message}", LogLevel.Error);
            }
        }

        private (string FarmName, string HorseName) DeriveFarmAndHorseNamesFromAssets()
        {
            string generatedPath = Path.Combine(this.Helper.DirectoryPath, "assets/Generated");

            if (!Directory.Exists(generatedPath))
            {
                this.Monitor.Log($"Generated assets directory not found: {generatedPath}", LogLevel.Warn);
                return ("DefaultFarm", "DefaultHorse");
            }

            var files = Directory.GetFiles(generatedPath, "*.png");
            if (files.Length == 0)
                return ("DefaultFarm", "DefaultHorse");

            // Extract farm and horse names from the first valid file name
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string[] parts = fileName.Split('_');
                if (parts.Length == 2) // Assumes format is "{FarmName}_{HorseName}.png"
                {
                    this.Monitor.Log($"Asset Farm Name {parts[0]}, Asset Horse Name {parts[1]}", LogLevel.Info);
                    return (parts[0], parts[1]);
                }
            }

            return ("DefaultFarm", "DefaultHorse");
        }

        private List<Horse> GetAllHorses()
        {
            List<Horse> horses = new List<Horse>();

            // Attempt to retrieve horses from the farm location
            Farm farm = Game1.locations.OfType<Farm>().FirstOrDefault();
            if (farm != null)
            {
                foreach (NPC npc in farm.characters)
                {
                    if (npc is Horse horse)
                    {
                        horses.Add(horse);
                    }
                }
            }

            // Fallback: Use filenames if no horses are found
            if (!horses.Any())
            {
                this.Monitor.Log("No horses found in the Farm location. Falling back to generated asset filenames.", LogLevel.Warn);
                var (farmName, horseName) = DeriveFarmAndHorseNamesFromAssets();

                if (!string.IsNullOrEmpty(horseName))
                {
                    Guid horseId = Guid.NewGuid(); // Generate a unique ID for the mock horse
                    int xTile = 0; // Placeholder tile position
                    int yTile = 0; // Placeholder tile position

                    // Create a mock horse object
                    Horse mockHorse = new Horse(horseId, xTile, yTile)
                    {
                        Name = horseName,
                        Sprite = new AnimatedSprite("Animals/horse", 0, 32, 32) // Placeholder sprite
                    };

                    horses.Add(mockHorse);
                }
                else
                {
                    this.Monitor.Log("No valid horse entries found in filenames. Returning an empty list.", LogLevel.Warn);
                }
            }

            return horses;
        }

    }

    public sealed class ModConfig
    {
        public Dictionary<string, Dictionary<string, HorseConfig>> HorseConfigs { get; set; } = new Dictionary<string, Dictionary<string, HorseConfig>>();

        public HorseConfig DefaultHorseConfig { get; set; } = new HorseConfig();
    }

    public class HorseConfig
    {
        public string BaseSkin { get; set; } = "HolsteinerSilver";
        public string Hair { get; set; } = "Plain";
        public string SaddleColor { get; set; } = "Black";
        public string Pattern { get; set; } = "None"; // New property
        public bool MenuIcon { get; set; } = true;
    }


    public sealed class ModConstants
    {
        public static List<string> ValidBaseSkins = new List<string>();
        public static List<string> ValidHairOptions = new List<string>();
        public static List<string> ValidSaddleColors = new List<string>();
        public static List<string> ValidPatterns = new List<string>();


        // Valid horse skins
        public static readonly List<string> DefaultBaseSkins = new List<string>
        {
            "Andalusian", "AppaloosaBlonde", "AppaloosaFawn", "AppaloosaBrown", "AppaloosaRed",
            "AppaloosaMidnight", "AppaloosaBlack", "AppaloosaGrey", "AppaloosaSilver", "Azteca",
            "BlackForest", "BlackShire", "Blue", "Buckskin", "Chestnut", "ClevelandBay",
            "Clydesdale", "Cremello", "Epona", "Fjord", "Fresian", "Green", "Holsteiner",
            "HolsteinerSilver", "Kathiawari", "Lipizzan", "Marwari", "Orange", "PaleDun",
            "Palomino", "Percheron", "Pink", "PintoBlonde", "PintoFawn", "PintoBrown", "PintoRed",
            "PintoMidnight", "PintoBlack", "PintoGrey", "PintoSilver", "Purple", "Red", "RedShire",
            "RoanBay", "RoanBlue", "RoanStrawberry", "Shadowmere", "SilverRockyMt", "SolidCream",
            "SolidBlonde", "SolidFawn", "SolidBrown", "SolidRed", "SolidMidnight", "SolidBlack",
            "SolidGrey", "SolidSilver", "SolidWhite", "Sorrel", "SpeckledBlonde", "SpeckledFawn",
            "SpeckledBrown", "SpeckledRed", "SpeckledMidnight", "SpeckledBlack", "SpeckledGrey",
            "SpeckledSilver", "Teal", "Thoroughbred", "Turquoise", "Vanilla", "WhiteShire", "Yellow",
            "VoidAppaloosa", "VoidBay", "VoidPinto", "VoidShire", "VoidSolid", "VoidSpeckled"
        };

        // Valid Pattern options
        public static readonly List<string> DefaultPatternOptions = new List<string>
        {
            "None"
        };

        // Valid hair options
        public static readonly List<string> DefaultHairOptions = new List<string>
        {
            "Plain", "Prismatic", "Black", "Brown", "Blonde", "Red", "Blue", "Green", "Purple"
        };

        // Valid saddle colors
        public static readonly List<string> DefaultSaddleColors = new List<string>
        {
            "Cream", "Blonde", "Fawn", "Brown", "Red", "Midnight", "Black", "Grey", "Silver",
            "White", "Vanilla", "BrightRed", "Orange", "Yellow", "Green", "Teal", "Turquoise",
            "Blue", "LightBlue", "Purple", "LightPurple", "Pink", "LightPink"
        };

        private ModConstants()
        {
            // Prevent instantiation
        }
    }
}