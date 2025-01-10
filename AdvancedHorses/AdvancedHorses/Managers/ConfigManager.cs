using AdvancedHorses.Config;
using AdvancedHorses.Helpers;
using StardewModdingAPI;
using StardewValley.Characters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AdvancedHorses.Managers
{
    public class ConfigManager(IMonitor monitor, IModHelper helper, ModConfig config, CompositeGenerator compositeGenerator, AssetLoader assetLoader, HorseManager horseManager)
    {
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly CompositeGenerator _compositeGenerator = compositeGenerator;
        private readonly ModConfig _config = config;
        private readonly AssetLoader _assetLoader = assetLoader;
        private readonly HorseManager _horseManager = horseManager;

        public void InitializeOrUpdateHorseConfigs()
        {
            if (_config == null || _horseManager == null || _assetLoader == null)
            {
                _monitor.Log("One or more dependencies are not initialized. Aborting InitializeOrUpdateHorseConfigs.", LogLevel.Error);
                return;
            }

            string farmName = _horseManager.GetCurrentFarmName();
            if (string.IsNullOrEmpty(farmName))
            {
                farmName = "DefaultFarm";
                _monitor.Log("Farm name is null or empty. Using fallback 'DefaultFarm'.", LogLevel.Warn);
            }

            if (!_config.HorseConfigs.ContainsKey(farmName))
            {
                _config.HorseConfigs[farmName] = new Dictionary<string, HorseConfig>();
            }

            var horses = _horseManager.GetHorses();
            if (horses == null || horses.Count == 0)
            {
                _monitor.Log("No horses found. Skipping configuration update.", LogLevel.Warn);
                return;
            }

            foreach (var horse in horses)
            {
                string horseName = horse.Name;

                if (!_config.HorseConfigs[farmName].ContainsKey(horseName))
                {
                    _config.HorseConfigs[farmName][horseName] = new HorseConfig
                    {
                        BaseSkin = _config.DefaultHorseConfig.BaseSkin,
                        Hair = _config.DefaultHorseConfig.Hair,
                        SaddleColor = _config.DefaultHorseConfig.SaddleColor,
                        MenuIcon = _config.DefaultHorseConfig.MenuIcon
                    };

                    _monitor.Log($"Added config for horse '{horseName}' on farm '{farmName}'.", LogLevel.Debug);
                }

                string baseSpritePath = _assetLoader.GetAssetPath("assets/Base", "Base_", _config.HorseConfigs[farmName][horseName].BaseSkin) ?? "DefaultPath";
                string patternOverlayPath = _assetLoader.GetAssetPath("assets/Patterns", "Pattern_", _config.HorseConfigs[farmName][horseName].Pattern) ?? "DefaultPath";
                string hairOverlayPath = _assetLoader.GetAssetPath("assets/Hair", "Hair_", _config.HorseConfigs[farmName][horseName].Hair) ?? "DefaultPath";
                string saddleOverlayPath = _assetLoader.GetAssetPath("assets/Saddles", "Saddle_", _config.HorseConfigs[farmName][horseName].SaddleColor) ?? "DefaultPath";
                string outputPath = Path.Combine(_helper.DirectoryPath, $"assets/Generated/{farmName}_{horseName}.png");

                _compositeGenerator.GenerateAndSaveCompositeIcon(horseName, farmName, baseSpritePath, patternOverlayPath, hairOverlayPath, saddleOverlayPath, outputPath);
            }

            _helper.WriteConfig(_config);
        }

        public Dictionary<string, HorseConfig> GetHorseConfigs(string farmName)
        {
            return _config.HorseConfigs.TryGetValue(farmName, out var configs) ? configs : new Dictionary<string, HorseConfig>();
        }
    }
}
