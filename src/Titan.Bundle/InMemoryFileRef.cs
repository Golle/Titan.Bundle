using System.Diagnostics;

namespace Titan.Bundle;

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