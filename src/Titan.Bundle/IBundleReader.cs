namespace Titan.Bundle;

internal interface IBundleReader
{
    IBundle Read(string filename, bool readEntireFile);
}