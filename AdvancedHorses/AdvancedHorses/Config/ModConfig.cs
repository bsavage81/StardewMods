
using System.Collections.Generic;

namespace AdvancedHorses.Config
{
    public sealed class ModConfig
    {
        public Dictionary<string, Dictionary<string, HorseConfig>> HorseConfigs { get; set; } = new Dictionary<string, Dictionary<string, HorseConfig>>();

        public Dictionary<string, HorseConfig> DefaultHorseConfig => HorseConfigs.ContainsKey("DefaultFarm") ? HorseConfigs["DefaultFarm"] : new Dictionary<string, HorseConfig>();
    }
}
