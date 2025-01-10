using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.IO;
using System;
using System.Drawing;
using System.Linq;
using AdvancedHorses.Managers;
using AdvancedHorses.Config;

public class CompositeGenerator(
    IManifest modManifest,
    IModHelper helper,
    IMonitor monitor,
    ModConfig config,
    ConfigManager ConfigManager,
    MultiplayerHandler multiplayerHandler,
    HorseManager horseManager)
{
    private readonly IManifest _modManifest = modManifest;
    private readonly IMonitor _monitor = monitor;
    private readonly IModHelper _helper = helper;
    private readonly ModConfig _config = config;
    private readonly HorseManager _horseManager = horseManager;
    private readonly ConfigManager _ConfigManager = ConfigManager;
    private readonly MultiplayerHandler _multiplayerHandler = multiplayerHandler;

    public void GenerateAndSaveCompositeIcon(
        string farmName,
        string horseName,
        string baseSpritePath,
        string patternOverlayPath,
        string hairOverlayPath,
        string saddleOverlayPath,
        string outputPath)
    {
        try
        {
            // Ensure the output directory exists
            string directoryPath = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                this._monitor.Log($"Created directory: {directoryPath}", LogLevel.Debug);
            }

            // Load base sprite, hair overlay, and saddle overlay
            Texture2D baseSprite = _helper.ModContent.Load<Texture2D>(baseSpritePath);
            Texture2D patternOverlay = File.Exists(patternOverlayPath) ? _helper.ModContent.Load<Texture2D>(patternOverlayPath) : null;
            Texture2D hairOverlay = File.Exists(hairOverlayPath) ? _helper.ModContent.Load<Texture2D>(hairOverlayPath) : null;
            Texture2D saddleOverlay = File.Exists(saddleOverlayPath) ? _helper.ModContent.Load<Texture2D>(saddleOverlayPath) : null;


            // Generate composite texture
            Texture2D composite = new Texture2D(Game1.graphics.GraphicsDevice, baseSprite.Width, baseSprite.Height);
            Color[] baseData = new Color[baseSprite.Width * baseSprite.Height];
            baseSprite.GetData(baseData);

            // Apply pattern overlay
            if (patternOverlay != null)
            {
                Color[] patternData = new Color[patternOverlay.Width * patternOverlay.Height];
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
                Color[] hairData = new Color[hairOverlay.Width * hairOverlay.Height];
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
                Color[] saddleData = new Color[saddleOverlay.Width * saddleOverlay.Height];
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
                composite.SaveAsPng(stream, composite.Width, composite.Height);
            }

            this._monitor.Log($"Menu icon generated and saved to: {outputPath}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Failed to generate or save menu icon for horse '{horseName}'. Error: {ex.Message}", LogLevel.Error);
        }
    }

}
