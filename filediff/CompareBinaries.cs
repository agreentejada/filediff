using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace filediff
{
    /* As per this issue, https://stackoverflow.com/questions/8927558/why-is-the-binary-output-not-equal-when-compiling-again,
     * we observe that a binary compiled over again has completely distinct metadata from the original compilation. However, if the only difference
     * between a two binaries of the same source code is metadata, in theory they should be 99% similar in bytes, and very different from a binary with a different source code.
     * 
     * Fast diff can then be found by guessing bytes where metadata is, avoiding them, and measuring the rest.
        */
    public static class MeasureBinaries
    {
        public static void Test(string oldpath, string newpath)
        {
            //Get the first .dll file in oldpath.
            var dlls = new DirectoryInfo(oldpath).GetFiles("*.dll", SearchOption.AllDirectories);

            //Compare with all the directories in the newpath.
            var newdlls = new DirectoryInfo(newpath).GetFiles("*.dll", SearchOption.AllDirectories);

            CompareSinglewithOthers(firstdll, newdlls.ToList());
        }

        //Comparies the bytes of a single .dll with all other in a folder. One of these should be a copy.
        public static void CompareSinglewithOthers(FileInfo origfile, List<FileInfo> newfiles)
        {
            byte[] origbytes = File.ReadAllBytes(origfile.FullName);

            //Attempts to find the find the copy file, which should have same length + name as orig.
            var copy = newfiles.Where(X => X.Name == origfile.Name && X.Length == origfile.Length);

            if (!copy.Any())
            {
                Console.WriteLine("No direct copy of the file in question could be found. Get better comparison parameters.");
                return;
            }

            var copyfile = copy.First();
            newfiles.Remove(copyfile);

            //Basic comparison. For two byte arrays of the same length, if the first array is "correct",
            //then we can consider any deviance in the second array as a mistake. Let's count the number of mistakes in the two arrays.

            //The copy makes a good control, since they should be similar.
            var copycomp = FindByteMistakes(origbytes, File.ReadAllBytes(copyfile.FullName));


            Console.WriteLine($"{origfile.Name} COMP {copyfile.Name}, {copycomp.Count} MISTAKES, " +
                $"{Math.Round((decimal)copycomp.Count*100/copyfile.Length)}% MISTAKE/BYTE.");

            foreach (var file in newfiles)
            {
                //We'll find the shortest length, and cut the arrays to fit that length.
                int shortestlength = file.Length < origbytes.Length ? (int)file.Length : origbytes.Length;

                byte[] correct = origbytes.Take(shortestlength).ToArray();
                byte[] compare = File.ReadAllBytes(file.FullName).Take(shortestlength).ToArray();

                var resultcomp = FindByteMistakes(correct, compare);
                Console.WriteLine($"{origfile.Name} COMP {file.Name}, {resultcomp.Count} MISTAKES, " +
                $"{Math.Round((decimal)resultcomp.Count * 100 / shortestlength)}% MISTAKE/BYTE.");
            }
        }

        //Compares all of the mistakes of copy dlls.
        public static void CompareCopies(FileInfo[] origfiles, FileInfo[] newfiles)
        {
            foreach (var item in collection)
            {

            }
        }


        public static Dictionary<int, byte> FindByteMistakes(byte[] correct, byte[] compare)
        {
            var dict = new Dictionary<int, byte>();

            for (int i = 0; i < correct.Length; i++)
            {
                if (compare[i] != correct[i])
                {
                    dict.Add(i, (byte)(correct[i] - compare[i]));
                }
            }

            return dict;
        }
    }
}
