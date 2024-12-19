using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using System;
using System.Collections.Generic;

namespace FarmBuildingInteriors
{
    public class ModEntry : Mod
    {
        private Dictionary<string, string?> BuildingTokens = new();
        private const string BuildingTokenPrefix = "FBI";
        public IContentPatcherAPI? ContentPatcherAPI = null;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ContentPatcherAPI = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            if (ContentPatcherAPI == null)
            {
                Monitor.Log("Content Patcher API not found. Tokens will not be registered.", LogLevel.Warn);
                return;
            }

            RegisterPlaceholderTokens();
            Monitor.Log("Content Patcher API successfully loaded and placeholder tokens registered.", LogLevel.Info);
        }

        private void RegisterPlaceholderTokens()
        {
            char placeholderChar = 'A';

            for (int i = 0; i < 26; i++) // Create 26 placeholders (A to Z)
            {
                string tokenKey = $"{BuildingTokenPrefix}Building{placeholderChar}";
                if (!BuildingTokens.ContainsKey(tokenKey))
                {
                    ContentPatcherAPI?.RegisterToken(ModManifest, tokenKey, () =>
                    {
                        return BuildingTokens[tokenKey] is not null ? new[] { BuildingTokens[tokenKey]! } : Array.Empty<string>();
                    });
                    BuildingTokens[tokenKey] = null;
                    Monitor.Log($"Registered placeholder token: {tokenKey}", LogLevel.Info);
                }

                placeholderChar++;
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            UpdateTokens();
        }

        private void UpdateTokens()
        {
            foreach (var tokenKey in BuildingTokens.Keys)
            {
                BuildingTokens[tokenKey] = null; // Reset the token value
            }

            var farm = Game1.getFarm();
            if (farm == null)
            {
                Monitor.Log("Farm data is not available. Tokens cannot be updated.", LogLevel.Error);
                return;
            }

            int tokenIndex = 0;

            foreach (var building in farm.buildings)
            {
                if (building.GetIndoors() is GameLocation indoorsLocation)
                {
                    char placeholderChar = (char)('A' + tokenIndex % 26);
                    string tokenKey = $"{BuildingTokenPrefix}Building{placeholderChar}";
                    string locationName = indoorsLocation.NameOrUniqueName ?? "UnknownLocation";

                    if (BuildingTokens.ContainsKey(tokenKey))
                    {
                        BuildingTokens[tokenKey] = locationName; // Update the value
                        Monitor.Log($"Updated token: {tokenKey} with value: {locationName}", LogLevel.Info);
                    }

                    tokenIndex++;
                }
                else
                {
                    Monitor.Log($"Building with type {building.buildingType.Value} has no valid indoor location.", LogLevel.Warn);
                }
            }
        }
    }
}
