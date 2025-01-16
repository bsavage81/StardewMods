
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.IO;
using System;
using AdvancedHorses.Config;
using StardewValley.Extensions;
using StardewValley.Locations;
using AdvancedHorses.Helpers;

namespace AdvancedHorses.Managers
{
    public class TextureManager(IMonitor monitor, IModHelper helper, ModConfig config, AssetHelper assetLoader)
    {
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;
        private readonly ModConfig _config = config;
        private readonly AssetHelper _assetLoader = assetLoader;

        public void GenerateAndSaveCompositeIcon()
        {
            var (farmName, horses) = _assetLoader.GetFarmAndHorses();

            if (!_config.HorseConfigs.TryGetValue(farmName, out var farmHorses))
            {
                _monitor.Log($"No configuration found for farm '{farmName}'. Skipping.", LogLevel.Warn);
                return;
            }

            foreach (var horse in horses)
            {
                string horseName = horse.Name;

                _monitor.Log($"Generating icon for horse '{horseName}' on farm '{farmName}'...", LogLevel.Debug);

                // Generate composite image
                string baseSpritePath = _assetLoader.GetAssetPath("assets", "Base", "Base_", _config.HorseConfigs[farmName][horseName].BaseSkin);
                string pattern1OverlayPath = _assetLoader.GetAssetPath("assets", "Patterns", "Pattern_", _config.HorseConfigs[farmName][horseName].Pattern1);
                string pattern2OverlayPath = _assetLoader.GetAssetPath("assets", "Patterns", "Pattern_", _config.HorseConfigs[farmName][horseName].Pattern2);
                string pattern3OverlayPath = _assetLoader.GetAssetPath("assets", "Patterns", "Pattern_", _config.HorseConfigs[farmName][horseName].Pattern3);
                string hairOverlayPath = _assetLoader.GetAssetPath("assets", "Hair", "Hair_", _config.HorseConfigs[farmName][horseName].Hair);
                string saddleOverlayPath = _assetLoader.GetAssetPath("assets", "Saddles", "Saddle_", _config.HorseConfigs[farmName][horseName].SaddleColor);
                string accessory1OverlayPath = _assetLoader.GetAssetPath("assets", "Accessories", "Accessories_", _config.HorseConfigs[farmName][horseName].Accessory1);
                string accessory2OverlayPath = _assetLoader.GetAssetPath("assets", "Accessories", "Accessories_", _config.HorseConfigs[farmName][horseName].Accessory2);
                string accessory3OverlayPath = _assetLoader.GetAssetPath("assets", "Accessories", "Accessories_", _config.HorseConfigs[farmName][horseName].Accessory3);
                string outputPath = Path.Combine(_helper.DirectoryPath, $"assets/Generated/{farmName}_{horseName}.png");

                try
                {
                    // Ensure the output directory exists
                    string directoryPath = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                        _monitor.Log($"Created directory: {directoryPath}", LogLevel.Debug);
                    }

                    // Load base sprite and overlays
                    Texture2D baseSprite = _helper.ModContent.Load<Texture2D>(baseSpritePath);
                    Texture2D pattern1Overlay = File.Exists(pattern1OverlayPath) ? _helper.ModContent.Load<Texture2D>(pattern1OverlayPath) : null;
                    Texture2D pattern2Overlay = File.Exists(pattern2OverlayPath) ? _helper.ModContent.Load<Texture2D>(pattern2OverlayPath) : null;
                    Texture2D pattern3Overlay = File.Exists(pattern3OverlayPath) ? _helper.ModContent.Load<Texture2D>(pattern3OverlayPath) : null;
                    Texture2D hairOverlay = File.Exists(hairOverlayPath) ? _helper.ModContent.Load<Texture2D>(hairOverlayPath) : null;
                    Texture2D saddleOverlay = File.Exists(saddleOverlayPath) ? _helper.ModContent.Load<Texture2D>(saddleOverlayPath) : null;
                    Texture2D accessory1Overlay = File.Exists(accessory1OverlayPath) ? _helper.ModContent.Load<Texture2D>(accessory1OverlayPath) : null;
                    Texture2D accessory2Overlay = File.Exists(accessory2OverlayPath) ? _helper.ModContent.Load<Texture2D>(accessory2OverlayPath) : null;
                    Texture2D accessory3Overlay = File.Exists(accessory3OverlayPath) ? _helper.ModContent.Load<Texture2D>(accessory3OverlayPath) : null;

                    int width = baseSprite.Width;
                    int height = baseSprite.Height;

                    if (pattern1Overlay != null && (pattern1Overlay.Width != width || pattern1Overlay.Height != height) ||
                        pattern2Overlay != null && (pattern2Overlay.Width != width || pattern2Overlay.Height != height) ||
                        pattern3Overlay != null && (pattern3Overlay.Width != width || pattern3Overlay.Height != height) ||
                        hairOverlay != null && (hairOverlay.Width != width || hairOverlay.Height != height) ||
                        saddleOverlay != null && (saddleOverlay.Width != width || saddleOverlay.Height != height) ||
                        accessory1Overlay != null && (accessory1Overlay.Width != width || accessory1Overlay.Height != height) ||
                        accessory2Overlay != null && (accessory2Overlay.Width != width || accessory2Overlay.Height != height) ||
                        accessory3Overlay != null && (accessory3Overlay.Width != width || accessory3Overlay.Height != height))
                    {
                        throw new InvalidOperationException("One or more overlay textures have different dimensions from the base sprite.");
                    }

                    // Generate composite texture
                    Texture2D composite = new Texture2D(Game1.graphics.GraphicsDevice, width, height);
                    Color[] baseData = new Color[width * height];
                    baseSprite.GetData(baseData);

                    // Apply pattern overlay
                    if (pattern1Overlay != null)
                    {
                        Color[] pattern1Data = new Color[width * height];
                        pattern1Overlay.GetData(pattern1Data);
                        for (int i = 0; i < baseData.Length; i++)
                        {
                            if (pattern1Data[i].A > 0) // Apply non-transparent pixels
                                baseData[i] = pattern1Data[i];
                        }
                    }

                    if (pattern2Overlay != null)
                    {
                        Color[] pattern2Data = new Color[width * height];
                        pattern2Overlay.GetData(pattern2Data);
                        for (int i = 0; i < baseData.Length; i++)
                        {
                            if (pattern2Data[i].A > 0) // Apply non-transparent pixels
                                baseData[i] = pattern2Data[i];
                        }
                    }

                    if (pattern3Overlay != null)
                    {
                        Color[] pattern3Data = new Color[width * height];
                        pattern3Overlay.GetData(pattern3Data);
                        for (int i = 0; i < baseData.Length; i++)
                        {
                            if (pattern3Data[i].A > 0) // Apply non-transparent pixels
                                baseData[i] = pattern3Data[i];
                        }
                    }

                    // Apply hair overlay
                    if (hairOverlay != null)
                    {
                        Color[] hairData = new Color[width * height];
                        hairOverlay.GetData(hairData);
                        for (int i = 0; i < baseData.Length; i++)
                        {
                            if (hairData[i].A > 0) // Apply non-transparent pixels
                                baseData[i] = hairData[i];
                        }
                    }

                    // Apply saddle overlay
                    if (saddleOverlay != null)
                    {
                        Color[] saddleData = new Color[width * height];
                        saddleOverlay.GetData(saddleData);
                        for (int i = 0; i < baseData.Length; i++)
                        {
                            if (saddleData[i].A > 0) // Apply non-transparent pixels
                                baseData[i] = saddleData[i];
                        }
                    }

                    // Apply pattern overlay
                    if (accessory1Overlay != null)
                    {
                        Color[] accessory1Data = new Color[width * height];
                        accessory1Overlay.GetData(accessory1Data);
                        for (int i = 0; i < baseData.Length; i++)
                        {
                            if (accessory1Data[i].A > 0) // Apply non-transparent pixels
                                baseData[i] = accessory1Data[i];
                        }
                    }

                    if (accessory2Overlay != null)
                    {
                        Color[] accessory2Data = new Color[width * height];
                        accessory2Overlay.GetData(accessory2Data);
                        for (int i = 0; i < baseData.Length; i++)
                        {
                            if (accessory2Data[i].A > 0) // Apply non-transparent pixels
                                baseData[i] = accessory2Data[i];
                        }
                    }

                    if (accessory3Overlay != null)
                    {
                        Color[] accessory3Data = new Color[width * height];
                        accessory3Overlay.GetData(accessory3Data);
                        for (int i = 0; i < baseData.Length; i++)
                        {
                            if (accessory3Data[i].A > 0) // Apply non-transparent pixels
                                baseData[i] = accessory3Data[i];
                        }
                    }

                    composite.SetData(baseData);

                    // Save the composite texture
                    using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        composite.SaveAsPng(stream, width, height);
                    }

                    _monitor.Log($"Menu icon generated and saved to: {outputPath}", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Failed to generate or save menu icon for horse '{horseName}'. Error: {ex.Message}", LogLevel.Error);
                }

                if (farmHorses.TryGetValue(horseName, out var horseConfig))
                {
                    string relativePath = $"assets/Generated/{farmName}_{horseName}.png";
                    string fullPath = Path.Combine(_helper.DirectoryPath, relativePath);

                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            // Load and apply texture
                            string assetName = _helper.ModContent.GetInternalAssetName(relativePath).BaseName;

                            // Debug log paths
                            _monitor.Log($"Full path: {fullPath}", LogLevel.Debug);
                            _monitor.Log($"Asset name: {assetName}", LogLevel.Debug);

                            horse.Sprite.overrideTextureName = assetName;
                            horse.Sprite.LoadTexture(assetName, syncTextureName: true);


                            // Invalidate asset cache to ensure textures are reloaded
                            _helper.GameContent.InvalidateCache(asset => asset.Name.BaseName.EqualsIgnoreCase("Animals/horse"));
                            _helper.GameContent.InvalidateCache(asset => asset.Name.BaseName.EqualsIgnoreCase(assetName));
                            _monitor.Log($"Invalidating cache for horse: {horseName}", LogLevel.Debug);

                            _monitor.Log($"Refreshed texture for horse '{horseName}' using '{relativePath}'.", LogLevel.Info);
                        }
                        catch (Exception ex)
                        {
                            _monitor.Log($"Failed to load or apply texture for horse '{horseName}'. Error: {ex.Message}", LogLevel.Error);
                        }
                    }
                    else
                    {
                        _monitor.Log($"Generated texture not found for horse '{horseName}' at path '{fullPath}'. Skipping.", LogLevel.Warn);
                    }
                }
                else
                {
                    _monitor.Log($"No configuration found for horse '{horseName}' on farm '{farmName}'. Skipping.", LogLevel.Warn);
                }
            }
        }
    }
}