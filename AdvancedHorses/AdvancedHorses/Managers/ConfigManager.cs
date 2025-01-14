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
            string farmName = _horseManager.GetCurrentFarmName();

            if (!this._config.HorseConfigs.ContainsKey(farmName))
            {
                this._config.HorseConfigs[farmName] = new Dictionary<string, HorseConfig>();
            }

            foreach (var horse in _horseManager.GetAllHorses())
            {
                string horseName = horse.Name;

                if (!this._config.HorseConfigs[farmName].ContainsKey(horseName))
                {
                    this._config.HorseConfigs[farmName][horseName] = new HorseConfig
                    {
                        BaseSkin = this._config.DefaultHorseConfig.BaseSkin,
                        Pattern = this._config.DefaultHorseConfig.Pattern,
                        Hair = this._config.DefaultHorseConfig.Hair,
                        SaddleColor = this._config.DefaultHorseConfig.SaddleColor,
                        MenuIcon = this._config.DefaultHorseConfig.MenuIcon
                    };

                    this._monitor.Log($"Added config for horse '{horseName}' on farm '{farmName}'.", LogLevel.Debug);
                }

                // Generate composite image
                string baseSpritePath = _assetLoader.GetAssetPath("assets", "Base", "Base_", this._config.HorseConfigs[farmName][horseName].BaseSkin);
                string patternOverlayPath = _assetLoader.GetAssetPath("assets", "Patterns", "Pattern_", this._config.HorseConfigs[farmName][horseName].Pattern);
                string hairOverlayPath = _assetLoader.GetAssetPath("assets", "Hair", "Hair_", this._config.HorseConfigs[farmName][horseName].Hair);
                string saddleOverlayPath = _assetLoader.GetAssetPath("assets", "Saddles", "Saddle_", this._config.HorseConfigs[farmName][horseName].SaddleColor);
                string outputPath = Path.Combine(_helper.DirectoryPath, $"assets/Generated/{farmName}_{horseName}.png");
                _compositeGenerator.GenerateAndSaveCompositeIcon(horseName, farmName, baseSpritePath, patternOverlayPath, hairOverlayPath, saddleOverlayPath, outputPath);
            }

            this._helper.WriteConfig(this._config);
        }


        public Dictionary<string, HorseConfig> GetHorseConfigs(string farmName)
        {
            return _config.HorseConfigs.TryGetValue(farmName, out var configs) ? configs : new Dictionary<string, HorseConfig>();
        }
    }
}
