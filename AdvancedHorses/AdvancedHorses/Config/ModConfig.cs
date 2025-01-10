
using System.Collections.Generic;

namespace AdvancedHorses.Config
{
    public sealed class ModConfig
    {
        public Dictionary<string, Dictionary<string, HorseConfig>> HorseConfigs { get; set; } = new Dictionary<string, Dictionary<string, HorseConfig>>();

        public HorseConfig DefaultHorseConfig { get; set; } = new HorseConfig();
    }
}
