using Titan.Bundle.Common;

namespace Titan.Bundle;

public record AssetDependencyDescriptor(string Id, string Name);
public record AssetMetadata(string Key, string Value);
public record AssetDescriptor(string Id, string Type, string[] Files, AssetDependencyDescriptor[] Dependencies, AssetMetadata[] Metadata, bool Static, bool Preload);
public record Manifest(AssetDescriptor[] Assets);

public class ManifestHandler
{
    public Manifest ReadManifest(string filename)
    {
        using var file = File.Open(filename, FileMode.Open, FileAccess.Read);
        Span<byte> buffer = stackalloc byte[(int)file.Length];
        var bytesRead = file.Read(buffer);
        if (bytesRead != buffer.Length)
        {
            throw new InvalidOperationException("Failed to read all bytes from the file.");
        }

        var manifest = Json.Deserialize<Manifest>(buffer);

        return manifest;
    }
}