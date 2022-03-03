using System.Text;
using Titan.Bundle.Types;

namespace Titan.Bundle;

public class FileBundleWriter : IBundleWriter
{
    private readonly ManifestHandler _manifestHandler;

    public FileBundleWriter(ManifestHandler manifestHandler)
    {
        _manifestHandler = manifestHandler;
    }
 
    public unsafe bool Write(string filename, string[] manifests)
    {
        using MemoryStream stringStream = new();
        using MemoryStream fileStream = new();

        Dictionary<string, StringRef> strings = new();
        List<AssetEntry> assetEntries = new();
        List<DependenciesEntry> dependenciesEntries = new();
        List<FileEntry> fileEntries= new();

        // Merge multiple manifest files into a single file
        foreach (var manifestFilename in manifests)
        {
            var manifest = _manifestHandler.ReadManifest(manifestFilename);
            var basePath = Path.GetDirectoryName(manifestFilename) ?? throw new InvalidOperationException("Failed to get the base path.");

            // Go through all assets
            foreach (var asset in manifest.Assets)
            {
                // The manifest entries contains the Id, dependency and file count
                assetEntries.Add(new AssetEntry
                {
                    Id = AddOrGetStringRef(asset.Id),
                    DependenciesCount = (byte)(asset.Dependencies?.Length ?? 0),
                    FilesCount = (byte)(asset.Files?.Length ?? 0),
                    Flags = GetFlags(asset)
                });
                
                // Write each dependency as as string ref
                foreach (var dependency in asset.Dependencies ?? Array.Empty<AssetDependencyDescriptor>())
                {
                    dependenciesEntries.Add(new DependenciesEntry
                    {
                        Id = AddOrGetStringRef(dependency.Id),
                        Name = AddOrGetStringRef(dependency.Name)
                    });
                }

                // Copy all  files into the filestream and add a file entry with the start offset, length and filename(for debugging only)
                foreach (var file in asset.Files ?? Array.Empty<string>())
                {
                    var bytes = File.ReadAllBytes(Path.Combine(basePath, file));
                    fileEntries.Add(new FileEntry
                    {
                        Length = bytes.Length,
                        Offset = (int)fileStream.Length,
                        Name = AddOrGetStringRef(file)
                    });
                    fileStream.Write(bytes);
                }
            }
        }

        var header = new Header
        {
            MagicNumber = Constants.FileMagicNumber,
            Version = Constants.CurrentVersion,
            DependenciesCount = dependenciesEntries.Count,
            AssetCount = assetEntries.Count,
            FileEntriesCount = fileEntries.Count,
            StringSize = (int)stringStream.Length,
            FileSize = (int)fileStream.Length
        };

        var totalFileSize = sizeof(Header)
                            + stringStream.Length
                            + fileStream.Length
                            + assetEntries.Count * sizeof(AssetEntry)
                            + fileEntries.Count * sizeof(FileEntry)
                            + dependenciesEntries.Count * sizeof(DependenciesEntry);

        using var outFile = File.Open(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        outFile.SetLength(totalFileSize);
        
        outFile.Write(new ReadOnlySpan<byte>(&header, sizeof(Header)));
        stringStream.Position = 0;
        stringStream.CopyTo(outFile);

        // TODO: Change this to either an array or a memory stream, this will be slow when there are a lot of entries
        foreach (var entry in assetEntries)
        {
            outFile.Write(new ReadOnlySpan<byte>(&entry, sizeof(AssetEntry)));
        }
        foreach (var entry in dependenciesEntries)
        {
            outFile.Write(new ReadOnlySpan<byte>(&entry, sizeof(DependenciesEntry)));
        }
        foreach (var entry in fileEntries)
        {
            outFile.Write(new ReadOnlySpan<byte>(&entry, sizeof(FileEntry)));
        }

        fileStream.Position = 0;
        fileStream.CopyTo(outFile);
        
        return true;

        StringRef AddOrGetStringRef(string value)
        {
            if (value == null)
            {
                return StringRef.Null;
            }

            if (!strings.TryGetValue(value, out var stringRef))
            {
                Span<byte> buffer = stackalloc byte[value.Length * 2];
                var bytes = Encoding.UTF8.GetBytes(value, buffer);
                stringRef = new StringRef
                {
                    Offset = (int)stringStream.Length,
                    Length = value.Length // NOTE(Jens): not sure if we should use value.Length or bytes
                };
                stringStream.Write(buffer[..bytes]);
                strings.Add(value, stringRef);
            }
            return stringRef;
        }

        static AssetFlags GetFlags(AssetDescriptor descriptor)
        {
            AssetFlags flags = 0;
            if (descriptor.Preload)
            {
                flags |= AssetFlags.Preload;
            }

            if (descriptor.Static)
            {
                flags |= AssetFlags.Static;
            }
            return flags;
        }
    }
}