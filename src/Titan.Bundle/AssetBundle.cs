namespace Titan.Bundle;

internal class AssetBundle : IBundle
{
    public bool IsCached { get; }
    private readonly IDisposable _resource;
    private readonly IDictionary<string, Asset> _assets;

    internal AssetBundle(IDisposable resource, IDictionary<string, Asset> assets, bool isCached)
    {
        IsCached = isCached;
        _resource = resource;
        _assets = assets;
    }
 
    public void Dispose()
    {
        _resource.Dispose();
    }

    public IFileRef[] GetFiles(string identifier)
    {
        if (_assets.TryGetValue(identifier, out var asset))
        {
            return asset.Files;
        }

        throw new InvalidOperationException($"No resource with identifier {identifier} in the collection");
    }
}