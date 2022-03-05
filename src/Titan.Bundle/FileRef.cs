using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace Titan.Bundle;

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