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


public interface IBundle : IDisposable
{
    //Span<byte> GetFile(string identifier);
    //int GetFile(string identifier, Span<byte> buffer);
    //int GetSize(string identifier);
    //IEnumerable<string> GetIdentifiers();
    bool IsCached { get; }
    IFileRef[] GetFiles(string identifier);
}

internal interface IBundleWriter
{
    bool Write(string filename, string[] manifests);
}