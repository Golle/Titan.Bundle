using System.Diagnostics;
using System.Runtime.InteropServices;
using Titan.Bundle.Types;

namespace Titan.Bundle;

public class FileBundleReader : IBundleReader
{
    private readonly IManifestCreator _manifestCreator;

    public FileBundleReader(IManifestCreator manifestCreator)
    {
        _manifestCreator = manifestCreator;
    }
    public IBundle Read(string filename, bool readEntireFile)
    {
        return readEntireFile ? ReadFile(filename) : ReadPartial(filename);
    }

    private unsafe IBundle ReadPartial(string filename)
    {
        var fileHandle = File.OpenHandle(filename, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.RandomAccess);
        Header header;
        {
            var bytesRead = RandomAccess.Read(fileHandle, new Span<byte>(&header, sizeof(Header)), 0);
            if (bytesRead != sizeof(Header))
            {
                throw new InvalidOperationException("Mismatch in header size and bytes read");
            }
        }

        if (!IsValid(header))
        {
            throw new Exception($"This is not a Titan bundle file. Expected version {Constants.CurrentVersion} got {header.Version}, expected magic {Constants.FileMagicNumber} got {header.MagicNumber}");
        }

        var manifestSize =
            header.FileEntriesCount * sizeof(FileEntry) +
            header.AssetCount * sizeof(AssetEntry) +
            header.DependenciesCount * sizeof(DependenciesEntry) +
            header.StringSize;

        Debug.Assert(manifestSize == RandomAccess.GetLength(fileHandle) - header.FileSize - sizeof(Header), "Manifest size mismatch, did the file change?");

        var buffer = NativeMemory.Alloc((nuint)manifestSize);
        {
            var bytesRead = RandomAccess.Read(fileHandle, new Span<byte>(buffer, manifestSize), sizeof(Header));
            Debug.Assert(bytesRead == manifestSize);


            
        }

        NativeMemory.Free(buffer);
        return null;
    }
    private unsafe IBundle ReadFile(string filename)
    {
        using var fileHandle = File.OpenHandle(filename, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);

        var fileLength = RandomAccess.GetLength(fileHandle);
        var buffer = NativeMemory.Alloc((nuint)fileLength);
        var bytesRead = RandomAccess.Read(fileHandle, new Span<byte>(buffer, (int)fileLength), 0);
        if (bytesRead != fileLength)
        {
            throw new Exception("Failed to read the entire file.");
        }
        var pHeader = (Header*)buffer;
        if (!IsValid(*pHeader))
        {
            throw new Exception($"This is not a Titan bundle file. Expected version {Constants.CurrentVersion} got {pHeader->Version}, expected magic {Constants.FileMagicNumber} got {pHeader->MagicNumber}");
        }
        var pStrings = (byte*)(pHeader + 1);
        var pAssets = (AssetEntry*)(pStrings + pHeader->StringSize);
        var pDependencies = (DependenciesEntry*)(pAssets + pHeader->AssetCount);
        var pFileEntries = (FileEntry*)(pDependencies + pHeader->DependenciesCount);
        var pFiles = (byte*)(pFileEntries + pHeader->FileEntriesCount);

        var dependencyIndex = 0;
        var fileEntryIndex = 0;

        for (var assetIndex = 0; assetIndex < pHeader->AssetCount; ++assetIndex)
        {
            ref readonly var asset = ref pAssets[assetIndex];
            var id = asset.Id.CreateString(pStrings);
            for (var i = 0; i < asset.DependenciesCount; ++i)
            {
                ref readonly var dependency = ref pDependencies[dependencyIndex++];
                var depsId = dependency.Id.CreateString(pStrings);
                var name = dependency.Name.CreateString(pStrings);
            }

            for (var i = 0; i < asset.FilesCount; ++i)
            {
                ref readonly var fileEntry = ref pFileEntries[fileEntryIndex++];
                var name = fileEntry.Name.CreateString(pStrings);
                var fileBytes = new Span<byte>(pFiles + fileEntry.Offset, fileEntry.Length);
            }
        }
        
        NativeMemory.Free(buffer);

        return null;
    }

    private static bool IsValid(in Header header) => header.Version == Constants.CurrentVersion && header.MagicNumber == Constants.FileMagicNumber;
}

internal interface IManifestCreator
{

    
    
}