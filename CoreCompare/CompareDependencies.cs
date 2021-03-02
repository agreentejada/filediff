using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace CoreCompare
{
    public static class CompareDependencies
    {
        /// <summary>
        /// Uses the .deps.json files in old and new directories as comparison contexts. Retrieves all <see cref="RuntimeFile"/> paths that are unique,
        /// uncomparable, or new to new directory.
        /// </summary>
        /// <returns>A <see cref="List{FileInfo}"/> of <see cref="RuntimeFile.Path"/> unique or new to <paramref name="newDirectory"/>.</returns>
        public static List<FileInfo> CompareRuntimes(DirectoryInfo oldDirectory, DirectoryInfo newDirectory)
        {
            var olddepquery = oldDirectory.GetFiles("*.deps.json", SearchOption.AllDirectories);
            var newdepquery = newDirectory.GetFiles("*.deps.json", SearchOption.AllDirectories);

            if (olddepquery.Length == 0)
            {
                throw new FileNotFoundException($"No dependency file for {oldDirectory.Name} could be found.");
            }
            if (newdepquery.Length == 0)
            {
                throw new FileNotFoundException($"No dependency file for {newDirectory.Name} could be found.");
            }

            var oldcontext = Import(olddepquery[0].FullName);
            var newcontext = Import(newdepquery[0].FullName);

            //Retrieve a list of runtimeFiles that compare old and new, the relative paths of the runtime files should
            //match with real paths in newDirectory.
            var extractedruntimes = CompareRuntimeFiles(oldcontext, newcontext);

            List<FileInfo> diffdlls = new List<FileInfo>();
            foreach (var extract in extractedruntimes)
            {
                diffdlls.Add(new FileInfo(Path.Combine(newDirectory.FullName, extract.Path)));
            }

            return diffdlls;
        }

        static DependencyContext Import(string depsPath)
        {
            using (var filestream = File.OpenRead(depsPath))
            {
                return new DependencyContextJsonReader().Read(filestream);
            }
        }

        //Now that the JSON has been parsed, it'll be pretty simple to check for version update in new and extract them.
        static List<RuntimeFile> CompareRuntimeFiles(DependencyContext oldctxt, DependencyContext newctxt)
        {
            List<RuntimeFile> extractedFiles = new List<RuntimeFile>();

            var oldfiles = oldctxt.RuntimeLibraries.SelectMany(X => X.RuntimeAssemblyGroups)
                .SelectMany(X => X.RuntimeFiles);
            var newfiles = newctxt.RuntimeLibraries.SelectMany(X => X.RuntimeAssemblyGroups)
                .SelectMany(X => X.RuntimeFiles);

            //Alright, so we have a list of all of the new binaries. To be conservative,
            //a) if new contains a file not in old, extract it
            //b) if neither new or old have version number to compare, extract new
            //c) if either assembly or version number are different in new, extract new
            foreach (var file in newfiles)
            {
                var oldquery = oldfiles.Where(X => X.Path == file.Path);

                if (!oldquery.Any())
                {
                    extractedFiles.Add(file);
                    Console.WriteLine("New binary " + file.Path + " was not found in OLD.");
                    continue;
                }
                else
                {
                    var oldfile = oldquery.First();

                    if (string.IsNullOrEmpty(oldfile.AssemblyVersion) ||
                        string.IsNullOrEmpty(oldfile.FileVersion) ||
                        string.IsNullOrEmpty(file.AssemblyVersion) ||
                        string.IsNullOrEmpty(file.FileVersion))
                    {
                        Console.WriteLine("Binary " + file.Path + " could not be compared.");
                        extractedFiles.Add(file);
                        continue;
                    }

                    if (file.FileVersion != oldfile.FileVersion ||
                        file.AssemblyVersion != oldfile.AssemblyVersion)
                    {
                        Console.WriteLine("Binary " + file.Path + " had a different Assembly/FileVersion in NEW than OLD.");
                        extractedFiles.Add(file);
                        continue;
                    }
                }
            }

            return extractedFiles;
        }
    }
}