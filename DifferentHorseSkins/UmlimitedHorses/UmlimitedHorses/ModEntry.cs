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

namespace UnlimitedStables
{
    public class ModConfig
    {
        public bool UnlimitedStables { get; set; } = true;
    }

    public class ModEntry : Mod
    {
        private ModConfig Config;

        public override void Entry(IModHelper helper)
        {
            // Load configuration
            Config = helper.ReadConfig<ModConfig>();

            // Subscribe to events
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            this.Monitor.Log("Unlimited Stables mod loaded successfully.", LogLevel.Info);
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!Config.UnlimitedStables || !(e.NewMenu is CarpenterMenu carpenterMenu)) return;

            // Modify Carpenter Menu to allow stable purchase regardless of ownership
            this.Monitor.Log("Modifying Carpenter Menu for unlimited stables.", LogLevel.Debug);

            // Remove the default stable blueprint and re-add it
            carpenterMenu.blueprints.RemoveAll(b => b.name == "Stable");
            var stableBlueprint = new BluePrint("Stable")
            {
                name = "CustomStable"
            };
            carpenterMenu.blueprints.Add(stableBlueprint);

            // Add stable ownership count UI
            AddStableCountToMenu(carpenterMenu);
        }

        private void AddStableCountToMenu(CarpenterMenu carpenterMenu)
        {
            var playerStables = GetPlayerOwnedStables();
            var stableCountMessage = $"You currently own {playerStables.Count} stable(s).";
            Game1.addHUDMessage(new HUDMessage(stableCountMessage, 3));
        }

        private List<Building> GetPlayerOwnedStables()
        {
            var stables = new List<Building>();
            foreach (var building in Game1.getFarm().buildings)
            {
                if (building.buildingType.Value == "Stable" && building.indoors.Value is Stable stable)
                {
                    stables.Add(building);
                }
            }
            return stables;
        }

        private void AssignHorseToStable(Building stable)
        {
            if (!(stable.indoors.Value is Stable stableIndoors)) return;

            var newHorse = new Horse($"Horse{Game1.getFarm().buildings.Count}", stable.tileX.Value, stable.tileY.Value)
            {
                displayName = $"Horse {Game1.getFarm().buildings.Count}"
            };

            Game1.getFarm().animals.Add(newHorse.myID.Value, newHorse);
            stableIndoors.horse = newHorse;
        }
    }

    public class StableNamingMenu : NamingMenu
    {
        public StableNamingMenu(OnDoneNaming doneNaming)
            : base(doneNaming, "Name your new stable:", "Stable")
        {
        }
    }
}
