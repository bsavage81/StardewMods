using AdvancedHorses.Config;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AdvancedHorses.Helpers
{
    public class AssetHelper(IManifest modManifest, IMonitor monitor, IModHelper helper,
        ModConfig config)
    {
        private readonly IManifest _modManifest = modManifest;
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;

        public ModConfig _config = config;

        public Texture2D LoadTexture(string path)
        {
            try
            {
                return _helper.ModContent.Load<Texture2D>(path);
            }
            catch
            {
                _monitor.Log($"Failed to load texture at path: {path}", LogLevel.Warn);
                return null;
            }
        }

        public void LoadDynamicAllowedValues()
        {
            // Scan for horse skins
            var horseSkins = GetValidFiles("assets/Base", "png")
                .Select(name => name.Replace("Base_", ""))
                .ToList();
            ModConstants.ValidBaseSkins = horseSkins.Any() ? horseSkins : new List<string>(ModConstants.DefaultBaseSkins);

            var patterns = GetValidFiles("assets/Patterns", "png")
                .Select(name => name.Replace("Pattern_", ""))
                .ToList();
            ModConstants.ValidPatterns = patterns.Any() ? patterns : new List<string>(ModConstants.DefaultPatternOptions);

            // Scan for hair overlays
            var hairOptions = GetValidFiles("assets/Hair", "png")
                .Select(name => name.Replace("Hair_", ""))
                .ToList();
            ModConstants.ValidHairOptions = hairOptions.Any() ? hairOptions : new List<string>(ModConstants.DefaultHairOptions);

            // Scan for saddle colors
            var saddleColors = GetValidFiles("assets/Saddles", "png")
                .Select(name => name.Replace("Saddle_", ""))
                .ToList();
            ModConstants.ValidSaddleColors = saddleColors.Any() ? saddleColors : new List<string>(ModConstants.DefaultSaddleColors);

            var accessories = GetValidFiles("assets/Accessories", "png")
                .Select(name => name.Replace("Accessories_", ""))
                .ToList();
            ModConstants.ValidAccessories = accessories.Any() ? accessories : new List<string>(ModConstants.DefaultAccessoryOptions);

            _monitor.Log("Dynamic allowed values loaded successfully.", LogLevel.Debug);
        }

        public List<string> GetValidFiles(string folderPath, string extension)
        {
            string fullPath = Path.Combine(_helper.DirectoryPath, folderPath);
            if (!Directory.Exists(fullPath))
            {
                _monitor.Log($"Directory not found: {fullPath}", LogLevel.Warn);
                return new List<string>();
            }

            return Directory
                .EnumerateFiles(fullPath, $"*.{extension}")
                .Select(file => Path.GetFileNameWithoutExtension(file))
                .ToList();
        }
        public string GetAssetPath(string asset_folder, string folder, string prefix, string name)
        {
            string prefixedPath = Path.Combine(_helper.DirectoryPath, asset_folder, folder, prefix + name + ".png");
            if (File.Exists(prefixedPath))
            {
                return prefixedPath;
            }
            string nonPrefixedPath = Path.Combine(_helper.DirectoryPath, asset_folder, folder, name + ".png");
            if (File.Exists(nonPrefixedPath))
            {
                return nonPrefixedPath;
            }
            // Log a warning if the file is not found
            _monitor.Log($"Asset not found in folder '{folder}' for name '{name}' with or without prefix '{prefix}'.", LogLevel.Warn);
            return null;
        }

        public (string FarmName, List<Horse> Horses) GetFarmAndHorses()
        {
            List<Horse> horses = new List<Horse>();

            // Attempt to retrieve horses from the farm location
            Farm farm = Game1.locations.OfType<Farm>().FirstOrDefault();
            string farmName = farm != null && !string.IsNullOrEmpty(farm.Name) ? farm.Name : GetCurrentDisplayedFarmName() ?? "DefaultFarm";

            if (farm != null && !string.IsNullOrEmpty(farm.Name))
            {
                foreach (NPC npc in farm.characters)
                {
                    if (npc is Horse horse)
                    {
                        horses.Add(horse);
                    }
                }
            }

            // Fallback to configuration if no in-world horses are found
            if (!horses.Any() && _config.HorseConfigs != null && _config.HorseConfigs.TryGetValue(farmName, out var horseConfigs))
            {
                horses = horseConfigs.Keys.Select(name => new Horse(Guid.NewGuid(), 0, 0)
                {
                    Name = name,
                    Sprite = new AnimatedSprite("Animals/horse", 0, 32, 32) // Placeholder sprite
                }).ToList();
            }

            if (!horses.Any())
            {
                _monitor.Log($"No horses found for farm '{farmName}'.", LogLevel.Warn);
            }

            return (farmName, horses);
        }


        private string GetCurrentDisplayedFarmName()
        {
            var gmcmApi = _helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcmApi != null && gmcmApi.TryGetCurrentMenu(out var mod, out var page))
            {
                if (mod == _modManifest && !string.IsNullOrEmpty(page))
                {
                    _monitor.Log($"Currently displayed page: {page}", LogLevel.Debug);
                    return page;
                }
            }

            _monitor.Log("No valid farm configuration page is currently displayed.", LogLevel.Warn);
            return null;
        }
    }
}
