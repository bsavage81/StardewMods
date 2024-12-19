using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.Pathfinding;

namespace FrogFriendsLikeRain
{
    public class ModEntry : Mod
    {
        public static IMonitor ModMonitor = null!; // Suppress null warning with `null!`
        public override void Entry(IModHelper helper)
        {
            ModMonitor = this.Monitor;

            var harmony = new Harmony(this.ModManifest.UniqueID);

            try
            {
                // Patch updateWhenNotCurrentLocation to allow frogs to exit during rain
                harmony.Patch(
                    original: AccessTools.Method(typeof(FarmAnimal), nameof(FarmAnimal.updateWhenNotCurrentLocation)),
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(AllowFrogExitWhenRaining))
                );

                // Patch updatePerTenMinutes to modify happiness logic
                harmony.Patch(
                    original: AccessTools.Method(typeof(FarmAnimal), nameof(FarmAnimal.updatePerTenMinutes)),
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(AdjustFrogHappiness))
                );

                // Patch behaviors to ensure frogs act normally outdoors during rain
                harmony.Patch(
                    original: AccessTools.Method(typeof(FarmAnimal), nameof(FarmAnimal.behaviors)),
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(EnableFrogBehaviorsDuringRain))
                );

                this.Monitor.Log("Frog Friends Like Rain loaded successfully.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Failed to load Frog Friends Like Rain: {ex}", LogLevel.Error);
            }
        }

        // Ensure frogs can exit during rain
        private static void AllowFrogExitWhenRaining(FarmAnimal __instance, Building currentBuilding, GameTime time, GameLocation environment)
        {
            try
            {
                if (!__instance.type.Value.Contains("frog", StringComparison.OrdinalIgnoreCase) || currentBuilding == null || environment == null)
                    return;

                // Ensure the door is open and it's raining
                if (currentBuilding != null && Game1.random.NextBool(0.002) && currentBuilding.animalDoorOpen.Value && Game1.timeOfDay < 1630 && !environment.IsWinterHere() && !environment.farmers.Any())
                {
                    GameLocation parentLocation = currentBuilding.GetParentLocation();
                    Microsoft.Xna.Framework.Rectangle rectForAnimalDoor = currentBuilding.getRectForAnimalDoor();
                    rectForAnimalDoor.Inflate(-2, -2);
                    if (parentLocation.isCollidingPosition(rectForAnimalDoor, Game1.viewport, isFarmer: false, 0, glider: false, __instance, pathfinding: false) || parentLocation.isCollidingPosition(new Microsoft.Xna.Framework.Rectangle(rectForAnimalDoor.X, rectForAnimalDoor.Y + 64, rectForAnimalDoor.Width, rectForAnimalDoor.Height), Game1.viewport, isFarmer: false, 0, glider: false, __instance, pathfinding: false))
                    {
                        return;
                    }

                    parentLocation.animals.Remove(__instance.myID.Value);
                    currentBuilding.GetIndoors().animals.Remove(__instance.myID.Value);
                    parentLocation.animals.Add(__instance.myID.Value, __instance);
                    __instance.faceDirection(2);
                    __instance.SetMovingDown(b: true);
                    __instance.Position = new Vector2(rectForAnimalDoor.X, rectForAnimalDoor.Y - (__instance.Sprite.getHeight() * 4 - __instance.GetBoundingBox().Height) + 32);

                    __instance.noWarpTimer = 3000;
                    currentBuilding.currentOccupants.Value--;


                    if (Utility.isOnScreen(__instance.TilePoint, 192, parentLocation))
                    {
                        parentLocation.localSound("sandyStep");
                    }

                    environment.isTileOccupiedByFarmer(__instance.Tile)?.TemporaryPassableTiles.Add(__instance.GetBoundingBox());
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor?.Log($"Error in AllowFrogExitWhenRaining: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
            }
        }


        // Adjust frog happiness to gain during rain
        private static void AdjustFrogHappiness(FarmAnimal __instance, int timeOfDay, GameLocation environment)
        {
            if (!__instance.type.Value.Contains("frog", StringComparison.OrdinalIgnoreCase) && environment.IsRainingHere())
            {
                int happinessGain = __instance.GetAnimalData()?.HappinessDrain ?? 10; // Default gain
                __instance.happiness.Value = (byte)Math.Min(255, __instance.happiness.Value + happinessGain);
            }
        }

        // Ensure frogs behave normally outdoors during rain
        private static void EnableFrogBehaviorsDuringRain(FarmAnimal __instance, GameTime time, GameLocation location)
        {
            if (!__instance.type.Value.Contains("frog", StringComparison.OrdinalIgnoreCase) && location.IsRainingHere())
            {
                // Simulate simple wandering behavior for frogs
                Vector2 tileLocation = new Vector2(
                    (int)(__instance.Position.X / Game1.tileSize),
                    (int)(__instance.Position.Y / Game1.tileSize)
                );

                tileLocation.X += Game1.random.Next(-1, 2); // Random horizontal movement
                tileLocation.Y += Game1.random.Next(-1, 2); // Random vertical movement

                // Ensure the new position is valid
                if (!location.isTileLocationOpen(tileLocation) || location.IsTileOccupiedBy(tileLocation))
                    return;

                // Move frog to the new tile
                __instance.setTileLocation(tileLocation);
            }
        }
    }
}
