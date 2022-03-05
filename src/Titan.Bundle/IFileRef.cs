namespace Titan.Bundle;

public interface IFileRef
{
    bool IsCached { get; }
    string Name { get; }
    int Length { get; }
    int ReadData(Span<byte> buffer);
    ReadOnlySpan<byte> ReadData();
}