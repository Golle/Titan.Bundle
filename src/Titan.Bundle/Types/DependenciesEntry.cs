using System.Runtime.InteropServices;

namespace Titan.Bundle.Types;

[StructLayout(LayoutKind.Sequential)]
internal struct DependenciesEntry
{
    public StringRef Id;
    public StringRef Name;
}