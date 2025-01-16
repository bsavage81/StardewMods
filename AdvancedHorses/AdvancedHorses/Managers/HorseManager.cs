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
        IMonitor monitor, IModHelper helper, ModConfig config, AssetLoader assetLoader)
    {
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly ModConfig _config = config;
        private readonly AssetLoader _assetLoader = assetLoader;

        public void ProcessHorses()
        {}
    }
}
