﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CoreCompare
{
    /// <summary>
    /// This class compares all files in two directories on a byte-by-byte basis. Differences found in the new file are place inside a new folder.
    /// </summary>
    public class DirectoryComparer
    {
        DirectoryInfo oldDirectory;
        DirectoryInfo newDirectory;
        DirectoryInfo publishDirectory;

        /// <summary>
        /// <summary>
        /// Developer extension files that will be ignored in comparison. By default, *.pdb files will get ignored.
        /// </summary>
        public List<string> IgnoredExtensions { get; set; } = new List<string>() { ".pdb"};

        /// <summary>
        /// Name of the directory where differences are stored. Will delete all the contents of old folder on refresh.
        /// </summary>
        public string PublishName { get; set; } = "diff";

        /// <summary>
        /// Determines whether published should package diff directory into a .zip file for ease of transport. ZIP file name will be <see cref="PublishName"/>.zip
        /// </summary>
        public bool CreateZIP { get; set; } = true;

        /// <summary>
        /// Please specify each path exactly.
        /// </summary>
        /// <param name="oldDirectory">The path for the old directory.</param>
        /// <param name="newDirectory">The path of the new directory.</param>
        /// <param name="publishDirectory">The path where the publish directory will go.</param>
        public DirectoryComparer(string oldPath, string newPath, string publishPath)
        {
            if (!Directory.Exists(oldPath))
            {
                throw new DirectoryNotFoundException($"Directory {oldDirectory} was not found.");
            }
            if (!Directory.Exists(newPath))
            {
                throw new DirectoryNotFoundException($"Directory {newDirectory} was not found.");
            }
            if (!Directory.Exists(publishPath))
            {
                throw new DirectoryNotFoundException($"Directory {publishDirectory} was not found.");
            }

            oldDirectory = new DirectoryInfo(oldPath);
            newDirectory = new DirectoryInfo(newPath);
            publishDirectory = new DirectoryInfo(publishPath);
        }

        /// <summary>
        /// Compares the directories, and returns all the diff files into publishdirectory. Binary files rely on {project}.deps.json to be compared.
        /// </summary>
        public void Compare()
        {
            Console.WriteLine($"Starting comparison between OLD: {oldDirectory.Name} and NEW: {newDirectory.Name}.");
            var diffdirectory = publishDirectory.CreateSubdirectory(PublishName);

            //Clears out diffdirectory before recopying.
            foreach (FileInfo file in diffdirectory.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in diffdirectory.GetDirectories())
            {
                dir.Delete(true);
            }

            //Get all the non-dll differences. Copies them in diffdirectory.
            Console.WriteLine("\r\nComparing non-dll files.");
            var diffs = CompareDirectories(oldDirectory, newDirectory, FilesAreEqual);

            //Adds any dll length differences.
            Console.WriteLine("\r\nComparing dll files by length.");
            var difflengthdlls = CompareDirectories(oldDirectory, newDirectory, BinariesAboutEqual);

            Console.WriteLine($"\r\nCopying files to {publishDirectory.Name}.");
            foreach (var diff in diffs)
            {
                string relpath = null;
                DirectoryInfo reldir = null;

                try
                {
                    string diffpath = diff.FullName;
                    //Added exception for lib/netstandard
                    if (!Directory.Exists(diff.DirectoryName) )
                    {
                       relpath = string.Empty;
                       diffpath = Path.Combine(newDirectory.FullName, diff.Name);
                        reldir = diffdirectory;
                    }
                    else
                    {
                        relpath = Path.GetRelativePath(newDirectory.FullName, diff.DirectoryName);
                        reldir = Directory.CreateDirectory(Path.Combine(diffdirectory.FullName, relpath));
                    }

                    File.Copy(diffpath, Path.Combine(reldir.FullName, diff.Name));
                    Console.WriteLine($"Copied file {diff.Name} from {diffpath} to {Path.Combine(reldir.FullName, diff.Name)}.");
                }
                catch (FileNotFoundException exc)
                {
                    Console.WriteLine("ERROR:\t" + exc.Message);
                }

            }

            //Creates a ZIP files if needed.
            if (CreateZIP)
            {
                string zippath = Path.Combine(publishDirectory.FullName, PublishName + ".zip");

                if (File.Exists(zippath))
                {
                    File.Delete(zippath);
                }

                Console.WriteLine("\r\nCreating ZIP file at " + zippath + ".");
                ZipFile.CreateFromDirectory(diffdirectory.FullName, zippath);
            }
        }

        /// <summary>
        /// Compares files in directories for differences, then recursively compares subdirectories.
        /// </summary>
        /// <param name="olddirectory">The old directory. Files/folder that don't exist in here will be added in differences.</param>
        /// <param name="newdirectory">The new directory. Files/folder here that don't exist in old will be added.</param>
        /// <param name="comparer">A method that takes two files and returns if they are the same or not.</param>
        /// <returns>A <see cref="List{FileInfo}"/> of differences found in new but not in old.</returns>
        List<FileInfo> CompareDirectories(DirectoryInfo olddirectory, DirectoryInfo newdirectory, Func<FileInfo, FileInfo, bool> comparer)
        {
            var diffs = new List<FileInfo>();

            Console.WriteLine($"Retrieving files from OLD: {olddirectory.Name} and NEW: {newdirectory.Name}.");
            var oldfiles = olddirectory.GetFiles();
            var newfiles = newdirectory.GetFiles();

            foreach (var newfile in newfiles)
            {
                //Skips ignore files.
                if (IgnoredExtensions.Contains(newfile.Extension) || newfile.Extension == ".dll")
                {
                    continue;
                }

                bool found = false;

                foreach (var oldfile in oldfiles)
                {
                    //Compares each new file in the directory with each old file. If an old file is found to match the new, then the new file is thrown out.
                    if (comparer(newfile, oldfile))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Console.WriteLine($"Unique file {newfile.Name} was found in {newDirectory.Name}.");
                    diffs.Add(newfile);
                }
            }

            var newdirs = newdirectory.GetDirectories();
            var olddirs = olddirectory.GetDirectories();

            //If a new directory doesn't match the name of any of the olds, then all it's files are added.
            //Otherwise, recursively searches through the directories files for new files.
            var oldnames = olddirs.Select(X => X.Name);

            foreach (var newdir in newdirs)
            {
                if (!oldnames.Contains(newdir.Name))
                {
                    var files = newdir.GetFiles("*", SearchOption.AllDirectories);
                    var filtered = files.Where(X => !IgnoredExtensions.Contains(X.Extension) || X.Extension == ".dll");
                    diffs.AddRange(filtered);
                }
                else
                {
                    var olddir = olddirs.Where(X => X.Name == newdir.Name).First();
                    var subdiffs = CompareDirectories(olddir, newdir, comparer);
                    diffs.AddRange(subdiffs);
                }
            }

            return diffs;
        }

        //Taken from https://stackoverflow.com/questions/1358510/how-to-compare-2-files-fast-using-net. Changed slightly so that same filenames don't return true.
        static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            int BYTES_TO_READ = sizeof(long);
            if (first.Length != second.Length)
                return false;

            if (!string.Equals(first.Name, second.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            //Performs a batch operation on byte reading, on assumption that bit comparison is slow, but bit conversion to long is significantly faster.
            //In comp tests, its usually 2nd place to Span<T> .SequenceEqual(), tied with P/Invoke.
            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }

            return true;
        }

        //A quick comparison to see if the binaries are really the same length or not.
        static bool BinariesAboutEqual(FileInfo first, FileInfo second)
        {
            if (first.Length != second.Length)
                return false;

            if (!string.Equals(first.Name, second.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
    }
}
