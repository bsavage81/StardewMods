using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AdvancedHorses.Helpers
{
    public class AssetLoader(IMonitor monitor, IModHelper helper)
    {
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;

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

            _monitor.Log("Dynamic allowed values loaded successfully.", LogLevel.Debug);
        }

        public string GetAssetPath(string folder, string prefix, string name)
        {
            // Check for prefixed file
            string prefixedPath = Path.Combine(_helper.DirectoryPath, folder, $"{prefix}{name}.png");
            if (File.Exists(prefixedPath))
                return prefixedPath;

            // Check for non-prefixed file
            string nonPrefixedPath = Path.Combine(_helper.DirectoryPath, folder, $"{name}.png");
            if (File.Exists(nonPrefixedPath))
                return nonPrefixedPath;

            // Log a warning if the file is not found
            _monitor.Log($"Asset not found in folder '{folder}' for name '{name}' with or without prefix '{prefix}'.", LogLevel.Warn);
            return null;
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
    }
}
