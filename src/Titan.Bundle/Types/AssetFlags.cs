namespace Titan.Bundle.Types;

[Flags]
public enum AssetFlags : byte
{
    None,
    Preload = 0x1,
    Static = 0x2
}