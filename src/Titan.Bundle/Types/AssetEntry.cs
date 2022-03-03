using System.Runtime.InteropServices;

namespace Titan.Bundle.Types;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct AssetEntry 
{
    public StringRef Id;
    public byte DependenciesCount;
    public byte FilesCount;
    public AssetFlags Flags;
}