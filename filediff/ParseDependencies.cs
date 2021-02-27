using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace filediff
{
    public class VersionData
    {
        [JsonPropertyName("assemblyVersion")]
        public string AssemblyVersion { get; set; }

        [JsonPropertyName("fileVersion")]
        public string FileVersion { get; set; }
    }

    public static class ParseDependencies
    {
        public static IDictionary<string, VersionData> ExtractMetadata(string depsPath)
        {
            string json = File.ReadAllText(depsPath);
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                return DeepSearch(document.RootElement, ".dll");
            }

        }

        //Recursive method, if the current element contains a .dll property name, else, search it insides.
        static IDictionary<string, VersionData> DeepSearch(JsonElement ele, string searchword)
        {
            var dict = new Dictionary<string, VersionData>();

            if (ele.ValueKind == JsonValueKind.Object)
            {
                foreach (var objele in ele.EnumerateObject())
                {
                    if (objele.Name.Contains(searchword))
                    {
                        var versiondata = JsonSerializer.Deserialize<VersionData>(objele.Value.GetRawText());
                        dict.Add(objele.Name, versiondata);
                    }
                    else
                    {
                        var results = DeepSearch(objele.Value, searchword);
                        foreach (var result in results)
                        {
                            dict.Add(result.Key, result.Value);
                        }
                    }
                }
            }
            else
            if (ele.ValueKind == JsonValueKind.Array && ele.GetArrayLength() > 0)
            {
                foreach (var arrayele in ele.EnumerateArray())
                {
                    var results = DeepSearch(arrayele, searchword);
                    foreach (var result in results)
                    {
                        dict.Add(result.Key, result.Value);
                    }
                }
            }

            return dict;
        }

        public static DependencyContext Import(string depsPath)
        {
            using (var filestream = File.OpenRead(depsPath))
            {
                return new DependencyContextJsonReader().Read(filestream);
            }
        }
    }
}
