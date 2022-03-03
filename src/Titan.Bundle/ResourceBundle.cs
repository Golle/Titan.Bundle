namespace Titan.Bundle;

/// <summary>
/// Read all data into memory
/// </summary>
internal class ResourceBundle : IBundle
{
    public Span<byte> GetFile(string identifier)
    {
        return default;
    }

    public IEnumerable<string> GetIdentifiers()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}