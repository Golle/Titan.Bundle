using System.Runtime.InteropServices;

namespace Titan.Bundle.Types;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct Header
{
    public int MagicNumber;
    public int Version;
    public int StringSize;
    public int AssetCount;
    public int DependenciesCount;
    public int FileEntriesCount;
    public int FileSize;
}