namespace Titan.Bundle;

internal struct Asset
{
    public string Name { get; }
    public IFileRef[] Files { get; }
    public Dependency[] Dependencies { get; }

    public Asset(string name, Dependency[] dependencies, IFileRef[] files)
    {
        Name = name;
        Files = files;
        Dependencies = dependencies;
    }
}