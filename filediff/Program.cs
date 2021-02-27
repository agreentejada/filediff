using System;

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

            //var diffclient = new FileDiff(oldpath, newpath, publishpath);
            //diffclient.Compare();

            //Testing a single file compared to other binaries.
            MeasureBinaries.Test(oldpath, newpath);
        }
    }
}
