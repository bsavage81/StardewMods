using StardewModdingAPI;

public interface IContentPatcherAPI
{
    /// <summary>
    /// Register a token for use in Content Patcher patches.
    /// </summary>
    /// <param name="mod">The mod registering the token.</param>
    /// <param name="name">The token name.</param>
    /// <param name="values">A function which returns the current token values.</param>
    void RegisterToken(IManifest mod, string name, Func<IEnumerable<string>> values);
}
