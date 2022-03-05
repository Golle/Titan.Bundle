using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Titan.Bundle.Types;

namespace Titan.Bundle;

public class FileBundleReader : IBundleReader
{
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
        var pStrings = (byte*)buffer;
        var pAssets = (AssetEntry*)(pStrings + header.StringSize);
        var pDependencies = (DependenciesEntry*)(pAssets + header.AssetCount);
        var pFileEntries = (FileEntry*)(pDependencies + header.DependenciesCount);

        var fileStartOffset = sizeof(Header) + manifestSize;

        var dependencyIndex = 0;
        var fileEntryIndex = 0;

        Dictionary<string, Asset> _assets = new();
        for (var assetIndex = 0; assetIndex < header.AssetCount; ++assetIndex)
        {
            ref readonly var asset = ref pAssets[assetIndex];
            var id = asset.Id.CreateString(pStrings);

            var dependencies = new Dependency[asset.DependenciesCount];
            for (var i = 0; i < asset.DependenciesCount; ++i)
            {
                ref readonly var dependency = ref pDependencies[dependencyIndex++];
                dependencies[i] = new Dependency(dependency.Id.CreateString(pStrings), dependency.Name.CreateString(pStrings));
            }

            var files = new IFileRef[asset.FilesCount];
            for (var i = 0; i < asset.FilesCount; ++i)
            {
                ref readonly var fileEntry = ref pFileEntries[fileEntryIndex++];
                var name = fileEntry.Name.CreateString(pStrings);
                files[i] = new FileRef(name, fileHandle, fileStartOffset + fileEntry.Offset, fileEntry.Length);
            }
            _assets.Add(id, new Asset(id, dependencies, files));
        }


        NativeMemory.Free(buffer);

        return new AssetBundle(fileHandle, _assets, false);
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

        Dictionary<string, Asset> _assets = new();
        for (var assetIndex = 0; assetIndex < pHeader->AssetCount; ++assetIndex)
        {

            ref readonly var asset = ref pAssets[assetIndex];
            var id = asset.Id.CreateString(pStrings);

            var dependencies = new Dependency[asset.DependenciesCount];
            for (var i = 0; i < asset.DependenciesCount; ++i)
            {
                ref readonly var dependency = ref pDependencies[dependencyIndex++];
                dependencies[i] = new Dependency(dependency.Id.CreateString(pStrings), dependency.Name.CreateString(pStrings));
            }

            var files = new IFileRef[asset.FilesCount];
            for (var i = 0; i < asset.FilesCount; ++i)
            {
                ref readonly var fileEntry = ref pFileEntries[fileEntryIndex++];
                var name = fileEntry.Name.CreateString(pStrings);
                files[i] = new InMemoryFileRef(name, pFiles + fileEntry.Offset, fileEntry.Length);
            }
            _assets.Add(id, new Asset(id, dependencies, files));
        }

        return new AssetBundle(new DisposableHandle(buffer), _assets, true);
    }

    private static bool IsValid(in Header header) => header.Version == Constants.CurrentVersion && header.MagicNumber == Constants.FileMagicNumber;

    private unsafe class DisposableHandle  : IDisposable
    {
        private void* _buffer;
        public DisposableHandle(void* buffer) => _buffer = buffer;

        public void Dispose()
        {
            if (_buffer != null)
            {
                NativeMemory.Free(_buffer);
                _buffer = null;
            }
        }
    }
}

internal struct Asset
{
    public string Name { get; }
    public IFileRef[] Files { get; }
    public Dependency[] Dependencies { get; }

    public Asset(string name, Dependency[] dependencies, IFileRef[] files)
    {
        Name = name;
        Files = files;
        Dependencies = dependencies;
    }
} 

internal readonly struct Dependency
{
    public readonly string Name;
    public readonly string Identifier;
    public Dependency(string identifier, string name)
    {
        Identifier = identifier;
        Name = name;
    }
}

public interface IFileRef
{
    bool IsCached { get; }
    string Name { get; }
    int Length { get; }
    int ReadData(Span<byte> buffer);
    ReadOnlySpan<byte> ReadData();
}

internal unsafe class InMemoryFileRef : IFileRef
{
    public bool IsCached => true;
    public string Name { get; }
    public int Length { get; }
    private readonly byte* _data;

    public InMemoryFileRef(string name, byte* data, int length)
    {
        Name = name;
        Length = length;
        _data = data;
    }
    /// <summary>
    /// Copies the file data into a preallocted buffer
    /// </summary>
    /// <param name="buffer">A pre-allocated buffer </param>
    /// <returns>Bytes copied</returns>
    public int ReadData(Span<byte> buffer)
    {
        Debug.Assert(buffer.Length >= Length);
        fixed (byte* pBuffer = buffer)
        {
            Buffer.MemoryCopy(_data, pBuffer, buffer.Length, Length);
        }
        return Length;
    }

    /// <summary>
    /// Returns a ReadOnlySpan over the bytes in the file
    /// </summary>
    /// <returns>A span</returns>
    public ReadOnlySpan<byte> ReadData() => new(_data, Length);
}

public class FileRef : IFileRef
{
    public bool IsCached => false;
    public string Name { get; }
    private readonly WeakReference<SafeFileHandle> _handle;
    public int Offset { get; }
    public int Length { get; }
    public FileRef(string name, SafeFileHandle handle, int offset, int length)
    {
        _handle = new WeakReference<SafeFileHandle>(handle);
        Name = name;
        Offset = offset;
        Length = length;
    }
    /// <summary>
    /// Read the data into a preallocated buffer
    /// </summary>
    /// <returns>The number of bytes read</returns>
    public int ReadData(Span<byte> buffer)
    {
        Debug.Assert(buffer.Length >= Length, "The size of the buffer is not big enough");
        if (_handle.TryGetTarget(out var handle))
        {
            return RandomAccess.Read(handle, buffer[..Length], Offset);
        }
        return -1;
    }

    /// <summary>
    /// Read the data from disk and allocate a byte array
    /// </summary>
    /// <returns>The buffer created</returns>
    public ReadOnlySpan<byte> ReadData()
    {
        var buffer = new byte[Length];
        if (_handle.TryGetTarget(out var handle))
        {
            RandomAccess.Read(handle, buffer, Offset);
        }
        return Span<byte>.Empty;
    }
}

public readonly unsafe struct FileRefOLD
{
    private readonly SafeFileHandle FileHandle;
    private readonly byte* Bytes;
    private readonly int Offset;
    public int Length { get; }
    public bool IsPreLoaded { get; }
    internal FileRefOLD(SafeFileHandle handle, int offset, int length)
    {
        FileHandle = handle;
        Length = length;
        Offset = offset;
        IsPreLoaded = false;
        Bytes = null;
    }

    internal FileRefOLD(byte* bytes, int offset, int length)
    {
        Bytes = bytes;
        Offset = offset;
        Length = length;
        IsPreLoaded = true;
        FileHandle = null;
    }

    public int ReadData(Span<byte> buffer)
    {
        if (FileHandle != null)
        {
            return RandomAccess.Read(FileHandle, buffer, Offset);
        }

        fixed (byte* pBuffer = buffer)
        {
            Buffer.MemoryCopy(Bytes + Offset, pBuffer, buffer.Length, Length);
        }
        return Length;
    }

    public ReadOnlySpan<byte> ReadData()
    {
        if (FileHandle != null)
        {
            var buffer = new byte[Length];
            ReadData(buffer);
            return buffer;
        }
        return new ReadOnlySpan<byte>(Bytes + Offset, Length);
    }
}

public class AssetBundle : IBundle
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

