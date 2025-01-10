using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Locations;
using System.IO;
using AdvancedHorses.Helpers;
using StardewValley.Extensions;
using AdvancedHorses.Config;

namespace AdvancedHorses.Managers
{
    public class HorseManager(
    IManifest modManifest,
    IModHelper helper,
    IMonitor monitor,
    ModConfig config,
    ConfigManager ConfigManager,
    MultiplayerHandler multiplayerHandler,
    HorseManager horseManager)
    {
        private readonly IManifest _modManifest = modManifest;
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly ModConfig _config = config;
        private readonly HorseManager _horseManager = horseManager;
        private readonly ConfigManager _ConfigManager = ConfigManager;
        private readonly MultiplayerHandler _multiplayerHandler = multiplayerHandler;

        public void ProcessHorses()
        {
            string farmName = GetCurrentFarmName();

            if (!this._config.HorseConfigs.TryGetValue(farmName, out var farmHorses))
            {
                this._monitor.Log($"No configuration found for farm '{farmName}'. Skipping.", LogLevel.Warn);
                return;
            }
            foreach (var horse in GetHorses())
            {
                string horseName = horse.Name;

                if (farmHorses.TryGetValue(horseName, out var horseConfig))
                {
                    string relativePath = $"assets/Generated/{farmName}_{horseName}.png";
                    string fullPath = Path.Combine(_helper.DirectoryPath, relativePath);

                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            // Load and apply texture
                            string assetName = this._helper.ModContent.GetInternalAssetName(relativePath).BaseName;

                            // Debug log paths
                            this._monitor.Log($"Full path: {fullPath}", LogLevel.Debug);
                            this._monitor.Log($"Asset name: {assetName}", LogLevel.Debug);

                            horse.Sprite.overrideTextureName = assetName;
                            horse.Sprite.LoadTexture(assetName, syncTextureName: true);


                            // Invalidate asset cache to ensure textures are reloaded
                            this._helper.GameContent.InvalidateCache(asset => asset.Name.BaseName.EqualsIgnoreCase("Animals/horse"));
                            this._helper.GameContent.InvalidateCache(asset => asset.Name.BaseName.EqualsIgnoreCase(assetName));
                            this._monitor.Log($"Invalidating cache for horse: {horseName}", LogLevel.Debug);

                            this._monitor.Log($"Refreshed texture for horse '{horseName}' using '{relativePath}'.", LogLevel.Info);
                        }
                        catch (Exception ex)
                        {
                            this._monitor.Log($"Failed to load or apply texture for horse '{horseName}'. Error: {ex.Message}", LogLevel.Error);
                        }
                    }
                    else
                    {
                        this._monitor.Log($"Generated texture not found for horse '{horseName}' at path '{fullPath}'. Skipping.", LogLevel.Warn);
                    }
                }
                else
                {
                    this._monitor.Log($"No configuration found for horse '{horseName}' on farm '{farmName}'. Skipping.", LogLevel.Warn);
                }
            }
        }

        public string GetCurrentFarmName()
        {
            if (Game1.player?.farmName?.Value != null)
                return Game1.player.farmName.Value;

            _monitor.Log("Farm name is null. Returning 'DefaultFarm' as fallback.", LogLevel.Warn);
            return "DefaultFarm";
        }

        public List<Horse> GetHorses()
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

            return horses;
        }
    }
}
