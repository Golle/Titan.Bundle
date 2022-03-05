using System.Diagnostics;
using System.Runtime.InteropServices;
using Titan.Bundle.Types;

namespace Titan.Bundle;

public class FileBundleReader : IBundleReader
{
    public unsafe IBundle Read(string filename, bool readEntireFile)
    {
        var fileHandle = File.OpenHandle(filename, FileMode.Open, FileAccess.Read, FileShare.Read, readEntireFile ? FileOptions.SequentialScan : FileOptions.RandomAccess);

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

        var manifestBuffer = NativeMemory.Alloc((nuint)manifestSize);
        {
            var bytesRead = RandomAccess.Read(fileHandle, new Span<byte>(manifestBuffer, manifestSize), sizeof(Header));
            Debug.Assert(bytesRead == manifestSize);
        }
        var pStrings = (byte*)manifestBuffer;
        var pAssets = (AssetEntry*)(pStrings + header.StringSize);
        var pDependencies = (DependenciesEntry*)(pAssets + header.AssetCount);
        var pFileEntries = (FileEntry*)(pDependencies + header.DependenciesCount);
        var fileStartOffset = sizeof(Header) + manifestSize;

        byte* pFiles = null;
        if (readEntireFile)
        {
            pFiles = (byte*)NativeMemory.Alloc((nuint)header.FileSize);
            var _ = RandomAccess.Read(fileHandle, new Span<byte>(pFiles, header.FileSize), fileStartOffset);
        }

        Func<FileEntry, IFileRef> fileRefCreator =
            readEntireFile
                ? fileEntry => new InMemoryFileRef(fileEntry.Name.CreateString(pStrings), pFiles + fileEntry.Offset, fileEntry.Length)
                : fileEntry => new FileRef(fileEntry.Name.CreateString(pStrings), fileHandle, fileStartOffset + fileEntry.Offset, fileEntry.Length);

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
                files[i] = fileRefCreator(fileEntry);
            }
            _assets.Add(id, new Asset(id, dependencies, files));
        }
        NativeMemory.Free(manifestBuffer);

        if (readEntireFile)
        {
            fileHandle.Dispose();
            return new AssetBundle(new DisposableHandle(pFiles), _assets, true);
        }
        return new AssetBundle(fileHandle, _assets, false);
    }

    private static bool IsValid(in Header header) => header.Version == Constants.CurrentVersion && header.MagicNumber == Constants.FileMagicNumber;

    private unsafe class DisposableHandle : IDisposable
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