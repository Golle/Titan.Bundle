using System.Text;
using Titan.Bundle;

const string TestFileName = @"c:\tmp\test.titan";
const string TestFileName1 = @"c:\tmp\test1.titan";


var manifestFiles = new[]
{
    @"F:\Git\Titan\samples\Titan.Sandbox\assets\manifest.json",
    @"F:\Git\Titan\samples\Titan.Sandbox\assets\builtin\debug_manifest.json",
    @"F:\Git\Titan\samples\Titan.Sandbox\assets\builtin\manifest.json"
};


{
    var writer = new FileBundleWriter(new ManifestHandler());
    writer.Write(TestFileName1, manifestFiles);
}

{
    var reader = new FileBundleReader();

    Span<byte> buffer = stackalloc byte[100_000];

    for (var i = 0; i < 2; ++i)
    {
        using var bundleFile = reader.Read(TestFileName1, readEntireFile: i == 0);
        var files = bundleFile.GetFiles("shaders/default_vs");

        if (bundleFile.IsCached)
        {
            foreach (var fileRef in files)
            {
                var bytes = fileRef.ReadData();
                Console.WriteLine(Encoding.UTF8.GetString(bytes));
            }
        }
        else
        {
            foreach (var fileRef in files)
            {
                var bytesRead = fileRef.ReadData(buffer);
                Console.WriteLine(Encoding.UTF8.GetString(buffer[0..bytesRead]));
            }
        }
    }
}

