using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace filediff
{
    public static class ParseDependencies
    {
        public static List<FileInfo> CompareBinaryDirectories(DirectoryInfo oldDirectory, DirectoryInfo newDirectory)
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
            var extractedruntimes = CompareRuntimes(oldcontext, newcontext);

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
        static List<RuntimeFile> CompareRuntimes(DependencyContext oldctxt, DependencyContext newctxt)
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

#region JsonParser
//public class VersionData
//{
//    [JsonPropertyName("assemblyVersion")]
//    public string AssemblyVersion { get; set; }

//    [JsonPropertyName("fileVersion")]
//    public string FileVersion { get; set; }
//}

//public static IDictionary<string, VersionData> ExtractMetadata(string depsPath)
//{
//    string json = File.ReadAllText(depsPath);
//    using (JsonDocument document = JsonDocument.Parse(json))
//    {
//        return DeepSearch(document.RootElement, ".dll");
//    }

//}

////Recursive method, if the current element contains a .dll property name, else, search it insides.
//static IDictionary<string, VersionData> DeepSearch(JsonElement ele, string searchword)
//{
//    var dict = new Dictionary<string, VersionData>();

//    if (ele.ValueKind == JsonValueKind.Object)
//    {
//        foreach (var objele in ele.EnumerateObject())
//        {
//            if (objele.Name.Contains(searchword))
//            {
//                var versiondata = JsonSerializer.Deserialize<VersionData>(objele.Value.GetRawText());
//                dict.Add(objele.Name, versiondata);
//            }
//            else
//            {
//                var results = DeepSearch(objele.Value, searchword);
//                foreach (var result in results)
//                {
//                    dict.Add(result.Key, result.Value);
//                }
//            }
//        }
//    }
//    else
//    if (ele.ValueKind == JsonValueKind.Array && ele.GetArrayLength() > 0)
//    {
//        foreach (var arrayele in ele.EnumerateArray())
//        {
//            var results = DeepSearch(arrayele, searchword);
//            foreach (var result in results)
//            {
//                dict.Add(result.Key, result.Value);
//            }
//        }
//    }

//    return dict;
//}
#endregion