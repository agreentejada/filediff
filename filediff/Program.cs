using System;
using System.IO;
using System.Text.Json;

namespace CoreCompare
{
    class Program
    {
        static void Main(string[] args)
        {
            //FileDiff test for publishing an update.
            string newpath = @"D:\Documents\Coding\DropboxClient\publish";
            string oldpath = @"D:\Documents\Coding\DropboxClient\DropboxClientManager";
            string publishpath = @"D:\Documents\Coding\DropboxClient";

            var diffclient = new DirectoryComparer(oldpath, newpath, publishpath);
            diffclient.CreateZIP = true;
            diffclient.IgnoredExtensions.Add(".log");
            diffclient.Compare();
        }
    }
}
