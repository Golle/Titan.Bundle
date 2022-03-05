namespace Titan.Bundle;

internal readonly struct Dependency
{
    public readonly string Name;
    public readonly string Identifier;
    public Dependency(string identifier, string name)
    {
        Identifier = identifier;
        Name = name;
    }
}