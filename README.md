# filediff
A small library for comparing published .NET Core directories. Uses the Microsoft.Extensions.DependencyModel library to compare .dll files.

filediff does two things. For all non binary files, filediff uses fast-byte comparison to compare every file in a newly published directory with an old version, and returns all the files that are unique or new to the new directory.
However, many .dll files contain metadata that changes with every compilation; this means that a byte based comparison will fail on two binary files with the same source code. filediff gets around this issue by parsing the *.deps.json of each published directory. It then loads both json files into a Microsoft.Extensions.DependencyModel contexts, and extracts all new/unique binary paths.

Lastly, filediff copies all of the extracted files to a new directory, optionally creating a ZIP folder.

# Usage
``` c#

//The path of the source directory.
string oldpath = @"D:\Documents\Coding\filediff\publish old";
//The path of the new directory.
string newpath = @"D:\Documents\Coding\filediff\publish";
//The path where you would like the directory of differences to be stored.
string publishpath = @"D:\Documents\Coding\filediff";

var diffclient = new DirectoryComparer(oldpath, newpath, publishpath);
diffclient.CreateZIP = true;
diffclient.IgnoredExtensions.Add(".log");
diffclient.Compare();
```

Nuget Package: https://www.nuget.org/packages/CoreCompare/
