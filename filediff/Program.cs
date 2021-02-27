using System;
using System.IO;
using System.Text.Json;

namespace filediff
{
    class Program
    {
        static void Main(string[] args)
        {
            //FileDiff test for publishing an update.
            string oldpath = @"D:\Documents\Coding\filediff\DropboxClientManager";
            string newpath = @"D:\Documents\Coding\filediff\publish";
            string publishpath = @"D:\Documents\Coding\filediff";

            var diffclient = new DirectoryComparer(oldpath, newpath, publishpath);
            diffclient.Compare();

            //Testing a single file compared to other binaries.
            //MeasureBinaries.Test(oldpath, newpath);

            ////Attempt to extract deps.json.
            //string olddepspath = Path.Combine(oldpath, "DropboxClient.deps.json");
            //string newdepspath = Path.Combine(newpath, "DropboxClient.deps.json");
            //var result = ParseDependencies.CompareRuntimes(ParseDependencies.Import(olddepspath),
            //    ParseDependencies.Import(newdepspath));

            //Console.WriteLine(JsonSerializer.Serialize(result));
        }
    }
}
