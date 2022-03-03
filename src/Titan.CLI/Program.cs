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
    var bundleFile = reader.Read(TestFileName1, readEntireFile: false);
    var bundlePartial = reader.Read(TestFileName1, readEntireFile: true);
}



