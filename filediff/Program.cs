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
            string oldpath = @"D:\Documents\Coding\filediff\test";
            string newpath = @"D:\Documents\Coding\filediff\test 1";
            string publishpath = @"D:\Documents\Coding\filediff";

            var diffclient = new DirectoryComparer(oldpath, newpath, publishpath);
            diffclient.CreateZIP = true;
            diffclient.IgnoredExtensions.Add(".log");
            diffclient.Compare();
        }
    }
}
