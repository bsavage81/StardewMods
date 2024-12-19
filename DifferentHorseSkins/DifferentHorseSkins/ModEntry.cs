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
            var horseSkins = this.GetValidFiles("assets/Horse", "png");
            ModConstants.ValidHorseSkins = horseSkins.Any() ? horseSkins : new List<string>(ModConstants.DefaultHorseSkins);

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
        }

        private void RegisterDefaultSettings(IGenericModConfigMenuApi api)
        {
            api.AddSectionTitle(mod: this.ModManifest, text: () => "Default Horse Settings");

            api.AddTextOption(
                mod: this.ModManifest,
                name: () => "Default Horse Skin",
                getValue: () => this.Config.DefaultHorseConfig.HorseSkin,
                setValue: value => this.Config.DefaultHorseConfig.HorseSkin = value,
                allowedValues: ModConstants.ValidHorseSkins.ToArray()
            );

            api.AddTextOption(
                mod: this.ModManifest,
                name: () => "Default Hair",
                getValue: () => this.Config.DefaultHorseConfig.Hair,
                setValue: value => this.Config.DefaultHorseConfig.Hair = value,
                allowedValues: ModConstants.ValidHairOptions.ToArray()
            );

            api.AddTextOption(
                mod: this.ModManifest,
                name: () => "Default Saddle Color",
                getValue: () => this.Config.DefaultHorseConfig.SaddleColor,
                setValue: value => this.Config.DefaultHorseConfig.SaddleColor = value,
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

                    api.AddTextOption(
                        mod: this.ModManifest,
                        name: () => "Horse Skin",
                        getValue: () => horseConfig.HorseSkin,
                        setValue: value => horseConfig.HorseSkin = value,
                        allowedValues: ModConstants.ValidHorseSkins.ToArray()
                    );

                    api.AddTextOption(
                        mod: this.ModManifest,
                        name: () => "Hair",
                        getValue: () => horseConfig.Hair,
                        setValue: value => horseConfig.Hair = value,
                        allowedValues: ModConstants.ValidHairOptions.ToArray()
                    );

                    api.AddTextOption(
                        mod: this.ModManifest,
                        name: () => "Saddle Color",
                        getValue: () => horseConfig.SaddleColor,
                        setValue: value => horseConfig.SaddleColor = value,
                        allowedValues: ModConstants.ValidSaddleColors.ToArray()
                    );
                }
            }
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu?.GetType().FullName == "GenericModConfigMenu.Framework.ModConfigMenu")
            {
                this.Monitor.Log("GMCM menu opened. Dynamically updating horse pages.", LogLevel.Debug);

                var api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                if (api == null)
                {
                    this.Monitor.Log("Generic Mod Config Menu API not found. Cannot update dynamically.", LogLevel.Warn);
                    return;
                }

                // Re-register the GMCM with the updated horse list
                this.RegisterModConfigMenu(api);
            }
        }
        private void InitializeOrUpdateHorseConfigs()
        {
            string farmName = Game1.player.farmName.Value ?? "Unknown";

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
                        HorseSkin = this.Config.DefaultHorseConfig.HorseSkin,
                        Hair = this.Config.DefaultHorseConfig.Hair,
                        SaddleColor = this.Config.DefaultHorseConfig.SaddleColor,
                        MenuIcon = this.Config.DefaultHorseConfig.MenuIcon
                    };

                    this.Monitor.Log($"Added config for horse '{horseName}' on farm '{farmName}'.", LogLevel.Debug);
                }

                // Generate composite image
                string baseSpritePath = Path.Combine(SHelper.DirectoryPath, $"assets/Horse/{this.Config.HorseConfigs[farmName][horseName].HorseSkin}.png");
                string hairOverlayPath = Path.Combine(SHelper.DirectoryPath, $"assets/Hair/Hair_{this.Config.HorseConfigs[farmName][horseName].Hair}.png");
                string saddleOverlayPath = Path.Combine(SHelper.DirectoryPath, $"assets/Saddles/Saddle_{this.Config.HorseConfigs[farmName][horseName].SaddleColor}.png");
                string outputPath = Path.Combine(SHelper.DirectoryPath, $"assets/Generated/{farmName}_{horseName}.png");

                this.GenerateAndSaveCompositeIcon(horseName, farmName, baseSpritePath, hairOverlayPath, saddleOverlayPath, outputPath);
            }

            this.Helper.WriteConfig(this.Config);
        }


        private void ApplyHorseConfigs()
        {
            string farmName = Game1.player.farmName.Value ?? "Unknown";

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

        private void GenerateAndSaveCompositeIcon(string horseName, string farmName, string baseSpritePath, string hairOverlayPath, string saddleOverlayPath, string outputPath)
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
                Texture2D hairOverlay = File.Exists(hairOverlayPath) ? SHelper.ModContent.Load<Texture2D>(hairOverlayPath) : null;
                Texture2D saddleOverlay = File.Exists(saddleOverlayPath) ? SHelper.ModContent.Load<Texture2D>(saddleOverlayPath) : null;

                // Generate composite texture
                Texture2D composite = new Texture2D(Game1.graphics.GraphicsDevice, baseSprite.Width, baseSprite.Height);
                Color[] baseData = new Color[baseSprite.Width * baseSprite.Height];
                baseSprite.GetData(baseData);

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

        private List<Horse> GetAllHorses()
        {
            List<Horse> horses = new List<Horse>();

            // Ensure the Farm location exists
            if (!Game1.locations.OfType<Farm>().Any())
            {
                this.Monitor.Log("Farm location not found. Cannot retrieve horses.", LogLevel.Warn);
                return horses;
            }

            // Retrieve horses from the Farm
            Farm farm = Game1.getFarm();
            foreach (NPC npc in farm.characters)
            {
                if (npc is Horse horse)
                {
                    horses.Add(horse);
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
        public string HorseSkin { get; set; } = "HolsteinerSilver";
        public string Hair { get; set; } = "Plain";
        public string SaddleColor { get; set; } = "Black";
        public bool MenuIcon { get; set; } = true;
    }

    public sealed class ModConstants
    {
        public static List<string> ValidHorseSkins = new List<string>();
        public static List<string> ValidHairOptions = new List<string>();
        public static List<string> ValidSaddleColors = new List<string>();

        // Valid horse skins
        public static readonly List<string> DefaultHorseSkins = new List<string>
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