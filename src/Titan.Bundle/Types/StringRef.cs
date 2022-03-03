using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace Titan.Bundle.Types;

[StructLayout(LayoutKind.Sequential)]
internal struct StringRef
{
    public int Offset; // NOTE(Jens): Offset is needed if we want to re-use strings. For example if 100 resources have a dependency on a single file.
    public int Length;
    public string CreateString(Span<byte> bytes)
    {
        Debug.Assert(Offset + Length < bytes.Length);
        return Encoding.UTF8.GetString(bytes[Offset..Length]);
    }

    [Pure]
    public unsafe string CreateString(byte* bytes)
    {
        if (Length == 0)
        {
            return string.Empty;
        }
        return Encoding.UTF8.GetString(bytes + Offset, Length);
    }

    public static readonly StringRef Null;
}