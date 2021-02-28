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
            string oldpath = @"C:\Users\ramsi\source\repos\sark\publish\v1.0.0.0";
            string newpath = @"C:\Users\ramsi\source\repos\sark\publish\sark-move";
            string publishpath = @"C:\Users\ramsi\source\repos\sark\publish\";

            var diffclient = new DirectoryComparer(oldpath, newpath, publishpath);
            diffclient.CreateZIP = true;
            diffclient.IgnoredExtensions.Add(".log");
            diffclient.Compare();
        }
    }
}
