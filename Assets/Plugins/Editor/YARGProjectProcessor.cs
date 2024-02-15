using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class YARGProjectProcessor : AssetPostprocessor
    {
        // Undocumented post-process hooks called by IDE packages
        // For the first two, return can be either void if no content modifications are made,
        // or string if the contents are being modified

        // Adds YARG.Core projects to the Unity solution
        // Avoids having to switch between solutions constantly
        private static string OnGeneratedSlnSolution(string path, string contents)
        {
            // Check for submodule
            string projectRoot = YARGCoreBuilder.ProjectRoot;
            string submodule = Path.Combine(projectRoot, "YARG.Core");
            if (!Directory.Exists(submodule))
            {
                Debug.LogError("YARG.Core submodule does not exist!");
                return contents;
            }

            // Write to temporary file
            string directory = Path.GetDirectoryName(path);
            string tempFile = Path.Combine(directory, "temp.sln");
            File.WriteAllText(tempFile, contents);

            // Find submodule projects
            // Collected separately so we can have a count
            var projectFiles = new List<string>();
            EditorUtility.DisplayProgressBar("Adding YARG.Core Projects to Solution", "Finding project files", 0f);
            foreach (string folder in Directory.EnumerateDirectories(submodule, "*.*", SearchOption.TopDirectoryOnly))
            {
                foreach (string projectFile in Directory.EnumerateFiles(folder, "*.csproj", SearchOption.TopDirectoryOnly))
                {
                    projectFiles.Add(projectFile);
                }
            }

            // Add submodule projects
            for (int i = 0; i < projectFiles.Count; i++)
            {
                string projectFile = projectFiles[i];
                try
                {
                    YARGCoreBuilder.RunCommand("dotnet", @$"sln ""{tempFile}"" add ""{projectFile}""",
                        "Adding YARG.Core Projects to Solution",
                        $"Adding {Path.GetFileName(projectFile)} ({i + 1} of {projectFiles.Count})",
                        (float) i / projectFiles.Count
                    ).Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to add YARG.Core project {projectFile} to solution {path}");
                    Debug.LogException(ex);
                }
            }
            EditorUtility.ClearProgressBar();

            // Read back temp file as new contents
            contents = File.ReadAllText(tempFile);
            File.Delete(tempFile);
            return contents;
        }

        // Adds YARG.Core to each of the Unity project files
        // Not really necessary and takes a long time, presumably since the project files are large
        // private static string OnGeneratedCSProject(string path, string contents)
        // {
        //     // Check for submodule
        //     string projectRoot = ProjectRoot;
        //     string submodule = Path.Combine(projectRoot, "YARG.Core");
        //     if (!Directory.Exists(submodule))
        //     {
        //         Debug.LogWarning($"Submodule {"YARG.Core"} does not exist!");
        //         return contents;
        //     }

        //     // Write to temporary file
        //     string directory = Path.GetDirectoryName(path);
        //     string tempFile = Path.Combine(directory, "temp.csproj");
        //     File.WriteAllText(tempFile, contents);

        //     // Add YARG.Core reference
        //     try
        //     {
        //         EditorUtility.DisplayProgressBar("Adding YARG.Core Reference to Project",
        //             $"Adding YARG.Core to {Path.GetFileName(path)}", 0f);
        //         string projectFile = Path.Join(submodule, "YARG.Core", $"YARG.Core.csproj");
        //         RunCommand("dotnet", @$"add ""{tempFile}"" reference ""{projectFile}""").Dispose();
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogError($"Failed to add YARG.Core project to project {path}");
        //         Debug.LogException(ex);
        //     }
        //     finally
        //     {
        //         EditorUtility.ClearProgressBar();
        //     }

        //     // Read back temp file as new contents
        //     contents = File.ReadAllText(tempFile);
        //     File.Delete(tempFile);
        //     return contents;
        // }
    }
}