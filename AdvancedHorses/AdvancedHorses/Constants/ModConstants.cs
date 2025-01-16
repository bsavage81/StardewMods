using System.Collections.Generic;

public sealed class ModConstants
{
    public static List<string> ValidBaseSkins = new List<string>();
    public static List<string> ValidHairOptions = new List<string>();
    public static List<string> ValidSaddleColors = new List<string>();
    public static List<string> ValidPatterns = new List<string>();
    public static List<string> ValidAccessories = new List<string>();


    // Valid horse skins
    public static readonly List<string> DefaultBaseSkins = new List<string>
        {
            "Andalusian", "AppaloosaBlonde", "AppaloosaFawn", "AppaloosaBrown", "AppaloosaRed",
            "AppaloosaMidnight", "AppaloosaBlack", "AppaloosaGrey", "AppaloosaSilver", "Azteca",
            "BlackForest", "BlackShire", "Blue", "Buckskin", "Chestnut", "ClevelandBay",
            "Clydesdale", "Cremello", "Epona", "Fjord", "Fresian", "Green", "Holsteiner",
            "HolsteinerSilver", "Kathiawari", "Lipizzan", "Marwari", "Orange", "PaleDun",
            "Palomino", "Percheron", "Pink", "PintoBlonde", "PintoFawn", "PintoBrown", "PintoRed",
            "PintoMidnight", "PintoBlack", "PintoGrey", "PintoSilver", "Purple", "Red", "RedShire",
            "RoanBay", "RoanBlue", "RoanStrawberry", "Shadowmere", "SilverRockyMt", "SolidCream",
            "SolidBlonde", "SolidFawn", "SolidBrown", "SolidRed", "SolidMidnight", "SolidBlack",
            "SolidGrey", "SolidSilver", "SolidWhite", "Sorrel", "SpeckledBlonde", "SpeckledFawn",
            "SpeckledBrown", "SpeckledRed", "SpeckledMidnight", "SpeckledBlack", "SpeckledGrey",
            "SpeckledSilver", "Teal", "Thoroughbred", "Turquoise", "Vanilla", "WhiteShire", "Yellow",
            "VoidAppaloosa", "VoidBay", "VoidPinto", "VoidShire", "VoidSolid", "VoidSpeckled"
        };

    // Valid Pattern options
    public static readonly List<string> DefaultPatternOptions = new List<string>
        {
            "None"
        };

    // Valid hair options
    public static readonly List<string> DefaultHairOptions = new List<string>
        {
            "Plain", "Prismatic", "Black", "Brown", "Blonde", "Red", "Blue", "Green", "Purple"
        };

    // Valid saddle colors
    public static readonly List<string> DefaultSaddleColors = new List<string>
        {
            "Cream", "Blonde", "Fawn", "Brown", "Red", "Midnight", "Black", "Grey", "Silver",
            "White", "Vanilla", "BrightRed", "Orange", "Yellow", "Green", "Teal", "Turquoise",
            "Blue", "LightBlue", "Purple", "LightPurple", "Pink", "LightPink"
        };

    // Valid Accessory options
    public static readonly List<string> DefaultAccessoryOptions = new List<string>
        {
            "None"
        };

    private ModConstants()
    {
        // Prevent instantiation
    }
}
