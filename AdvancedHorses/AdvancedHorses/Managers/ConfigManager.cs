using AdvancedHorses.Config;
using AdvancedHorses.Helpers;
using StardewModdingAPI;
using System.Collections.Generic;

namespace AdvancedHorses.Managers
{
    public class ConfigManager(IMonitor monitor, IModHelper helper, ModConfig config, TextureManager compositeGenerator, AssetHelper assetLoader)
    {
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly TextureManager _compositeGenerator = compositeGenerator;
        private readonly ModConfig _config = config;
        private readonly AssetHelper _assetLoader = assetLoader;

        public void InitializeOrUpdateHorseConfigs()
        {
            var (farmName, horses) = _assetLoader.GetFarmAndHorses();

            // Ensure the farm exists in the HorseConfigs dictionary
            if (!_config.HorseConfigs.ContainsKey(farmName))
            {
                _config.HorseConfigs[farmName] = new Dictionary<string, HorseConfig>();
            }

            // Log existing farm and horse configurations
            foreach (var entry in _config.HorseConfigs)
            {
                string configFarmName = entry.Key;
                var horseConfigs = entry.Value;

                _monitor.Log($"Farm: {configFarmName}", LogLevel.Debug);

                foreach (var horseConfig in horseConfigs)
                {
                    string configHorseName = horseConfig.Key;
                    var configValues = horseConfig.Value;

                    _monitor.Log($"  Horse: {configHorseName}, Config: {configValues}", LogLevel.Debug);
                }
            }

            // Iterate over the horses for the current farm
            foreach (var horse in horses)
            {
                string horseName = horse.Name;
                _monitor.Log($"2*********************************************'{horseName}' on farm '{farmName}'.", LogLevel.Debug);

                // Ensure the horse exists in the configuration for the current farm
                if (!_config.HorseConfigs[farmName].ContainsKey(horseName))
                {
                    // Attempt to get the default horse configuration
                    var defaultHorseConfig = _config.DefaultHorseConfig.ContainsKey("DefaultHorse")
                        ? _config.DefaultHorseConfig["DefaultHorse"]
                        : new HorseConfig
                        {
                            BaseSkin = "Vanilla",
                            Pattern1 = "None",
                            Pattern2 = "None",
                            Pattern3 = "None",
                            Hair = "Plain",
                            SaddleColor = "Brown",
                            Accessory1 = "None",
                            Accessory2 = "None",
                            Accessory3 = "None"
                        };

                    // Add the new horse configuration
                    _config.HorseConfigs[farmName][horseName] = new HorseConfig
                    {
                        BaseSkin = defaultHorseConfig.BaseSkin,
                        Pattern1 = defaultHorseConfig.Pattern1,
                        Pattern2 = defaultHorseConfig.Pattern2,
                        Pattern3 = defaultHorseConfig.Pattern3,
                        Hair = defaultHorseConfig.Hair,
                        SaddleColor = defaultHorseConfig.SaddleColor,
                        Accessory1 = defaultHorseConfig.Accessory1,
                        Accessory2 = defaultHorseConfig.Accessory2,
                        Accessory3 = defaultHorseConfig.Accessory3
                    };

                    _monitor.Log($"Added config for horse '{horseName}' on farm '{farmName}'.", LogLevel.Debug);
                }
            }

            // Save composite icons and write updated config
            _compositeGenerator.GenerateAndSaveCompositeIcon();
            _helper.WriteConfig(_config);
        }


        public Dictionary<string, HorseConfig> GetHorseConfigs(string farmName)
        {
            return _config.HorseConfigs.TryGetValue(farmName, out var configs) ? configs : new Dictionary<string, HorseConfig>();
        }
    }
}
