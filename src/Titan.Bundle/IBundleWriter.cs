using System.Runtime.InteropServices;

namespace Titan.Bundle;

/*
 * HEADER
 *  - Version
 *  - Magic
 *  - String size
 *  - TOC count?
 *  - file start offset
 * String
 *  - bytes
 * TOC
 *  - string?
 *  - file offset (relative)
 *
 * Files
 * - bytes
 */

public record FileMetadata(long size);

public record FileDescriptor(string identifier, string filename, FileMetadata? metadata = null);


internal class TableOfContents
{

}


public interface IBundle : IDisposable
{
    Span<byte> GetFile(string identifier);

    IEnumerable<string> GetIdentifiers();
}

/// <summary>
/// Only read header, access files on-demand
/// </summary>
internal class FileBundle : IBundle
{
    private readonly SafeHandle _fileHandle;

    public FileBundle(SafeHandle fileHandle)
    {
        _fileHandle = fileHandle;
    }

    public Span<byte> GetFile(string identifier)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> GetIdentifiers()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (!_fileHandle.IsClosed)
        {
            _fileHandle.Close();
            _fileHandle.Dispose();
        }
    }
}

internal interface IBundleWriter
{
    bool Write(string filename, string[] manifests);
}