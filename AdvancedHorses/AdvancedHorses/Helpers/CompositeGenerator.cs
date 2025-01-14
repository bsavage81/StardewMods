
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.IO;
using System;

namespace AdvancedHorses.Helpers
{
    public class CompositeGenerator(
        IMonitor monitor, IModHelper helper)

    {
        private readonly IMonitor _monitor = monitor;
        private readonly IModHelper _helper = helper;

        public void GenerateAndSaveCompositeIcon(
            string horseName, string farmName, string baseSpritePath, string patternOverlayPath, string hairOverlayPath, string saddleOverlayPath, string outputPath)
        {
            _monitor.Log($"Generating icon for horse '{horseName}' on farm '{farmName}'...", LogLevel.Debug);
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
                Texture2D patternOverlay = File.Exists(patternOverlayPath) ? _helper.ModContent.Load<Texture2D>(patternOverlayPath) : null;
                Texture2D hairOverlay = File.Exists(hairOverlayPath) ? _helper.ModContent.Load<Texture2D>(hairOverlayPath) : null;
                Texture2D saddleOverlay = File.Exists(saddleOverlayPath) ? _helper.ModContent.Load<Texture2D>(saddleOverlayPath) : null;

                int width = baseSprite.Width;
                int height = baseSprite.Height;

                if ((patternOverlay != null && (patternOverlay.Width != width || patternOverlay.Height != height)) ||
                    (hairOverlay != null && (hairOverlay.Width != width || hairOverlay.Height != height)) ||
                    (saddleOverlay != null && (saddleOverlay.Width != width || saddleOverlay.Height != height)))
                {
                    throw new InvalidOperationException("One or more overlay textures have different dimensions from the base sprite.");
                }

                // Generate composite texture
                Texture2D composite = new Texture2D(Game1.graphics.GraphicsDevice, width, height);
                Color[] baseData = new Color[width * height];
                baseSprite.GetData(baseData);

                // Apply pattern overlay
                if (patternOverlay != null)
                {
                    Color[] patternData = new Color[width * height];
                    patternOverlay.GetData(patternData);
                    for (int i = 0; i < baseData.Length; i++)
                    {
                        if (patternData[i].A > 0) // Apply non-transparent pixels
                            baseData[i] = patternData[i];
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
        }
    }
}