using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

public class ScriptDownloaderEditor : EditorWindow
{
    private bool createScripts = true;
    private bool createMaterials = true;
    private bool createMusic = true;
    private bool createPrefabs = true;
    private bool createModels = true;
    private bool createTextures = true;
    private bool createEditor = true;
    private static string remoteRepositoryURL = "";

    public static string RemoteRepositoryURL
    {
        get { return remoteRepositoryURL; }
        set { remoteRepositoryURL = value; }
    }


    [MenuItem("Tools/Download Template Scripts")]
    public static void ShowWindow()
    {
        GetWindow<ScriptDownloaderEditor>("Download Template Scripts");
    }

    private async void OnGUI()
    {
        GUILayout.Label("Create Default Folders", EditorStyles.boldLabel);

        createScripts = GUILayout.Toggle(createScripts, "Scripts");
        createMaterials = GUILayout.Toggle(createMaterials, "Materials");
        createMusic = GUILayout.Toggle(createMusic, "Music");
        createPrefabs = GUILayout.Toggle(createPrefabs, "Prefabs");
        createModels = GUILayout.Toggle(createModels, "Models");
        createTextures = GUILayout.Toggle(createTextures, "Textures");
        createEditor = GUILayout.Toggle(createEditor, "Editor");

        if (GUILayout.Button("Create Default Folders"))
        {
            CreateDefaultFolders();
        }

        if (GUILayout.Button("Download .gitignore"))
        {
            await GettingGitIgnore();
        }
        GUILayout.Label("Remote Git Repository", EditorStyles.boldLabel);
        remoteRepositoryURL = EditorGUILayout.TextField("Remote Repository URL:", remoteRepositoryURL);
        if (GUILayout.Button("Initialize Git Repository"))
        {
            InitializeGitRepository();
        }

        if (GUILayout.Button("Download Scripts"))
        {
            await GettingTemplateScripts();
        }
        if (GUILayout.Button("Add Necessary Packages"))
        {
            await AddRemoveNecessaryPackages();
        }
        if (GUILayout.Button("Resolve Packages"))
        {
            Resolve();
        }




    }

    public static async Task GettingTemplateScripts()
    {
        string folderPath = Application.dataPath;

        string[] fileUrls = {
            "https://raw.githubusercontent.com/Avin19/UnityTools/main/CustomScriptsTemplate.cs",
            "https://raw.githubusercontent.com/Avin19/UnityTools/main/Template/NewScript.cs.txt",
            "https://raw.githubusercontent.com/Avin19/UnityTools/main/Template/NewEnum.cs.Txt",
            "https://raw.githubusercontent.com/Avin19/UnityTools/main/Template/NewScriptableObject.cs.txt",
            "https://raw.githubusercontent.com/Avin19/UnityTools/main/Template/NewClass.cs.txt"
        };
        string[] fileNames = {
            "CustomScriptsTemplate.cs",
            "NewScript.cs.txt",
            "NewEnum.cs.txt",
            "NewScriptableObject.cs.txt",
            "NewClass.cs.txt"
        };

        for (int i = 0; i < fileUrls.Length; i++)
        {
            string fullPath = Path.Combine(folderPath, "Project/Editor/Template", fileNames[i]);

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            EditorUtility.DisplayProgressBar("Downloading Scripts", $"Downloading {fileNames[i]}...", (float)i / fileUrls.Length);

            await DownloadFileAsync(fileUrls[i], fullPath);
        }

        EditorUtility.ClearProgressBar();
        UnityEngine.Debug.Log("All scripts downloaded successfully.");
    }

    private static async Task DownloadFileAsync(string url, string filePath)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Send a GET request to the specified URL
                HttpResponseMessage response = await client.GetAsync(url);

                // Ensure the request was successful
                response.EnsureSuccessStatusCode();

                // Read the response content as a byte array
                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                // Write the byte array to the specified file
                await File.WriteAllBytesAsync(filePath, fileBytes);

                UnityEngine.Debug.Log($"Downloaded and saved file to {filePath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"An error occurred while downloading {url}: {ex.Message}");
            }
        }
    }


    public static void Resolve()
    {
        Client.Resolve();
        UnityEngine.Debug.Log("Packages resolved.");
    }


    public static async Task AddRemoveNecessaryPackages()
    {
        string[] packagesToAdd = { "com.unity.ide.visualstudio", "com.unity.textmeshpro", "com.unity.inputsystem" };
        string[] packagesToRemove = { "com.unity.visualscripting", "com.unity.ide.rider", "com.unity.timeline" };

        await AddPackages(packagesToAdd);
        await RemovePackages(packagesToRemove);

        Resolve();
    }

    private static async Task AddPackages(string[] packages)
    {
        foreach (string package in packages)
        {
            AddRequest request = Client.Add(package);
            while (!request.IsCompleted)
                await Task.Yield();

            if (request.Status == StatusCode.Success)
            {
                UnityEngine.Debug.Log($"Successfully added package: {package}");
            }
            else if (request.Status >= StatusCode.Failure)
            {
                UnityEngine.Debug.LogError($"Failed to add package: {package}, Error: {request.Error.message}");
            }
        }
    }

    private static async Task RemovePackages(string[] packages)
    {
        foreach (string package in packages)
        {
            RemoveRequest request = Client.Remove(package);
            while (!request.IsCompleted)
                await Task.Yield();

            if (request.Status == StatusCode.Success)
            {
                UnityEngine.Debug.Log($"Successfully removed package: {package}");
            }
            else if (request.Status >= StatusCode.Failure)
            {
                UnityEngine.Debug.LogError($"Failed to remove package: {package}, Error: {request.Error.message}");
            }
        }
    }

    public void CreateDefaultFolders()
    {
        List<string> selectedFolders = new List<string>();

        if (createScripts) selectedFolders.Add("Scripts");
        if (createMaterials) selectedFolders.Add("Materials");
        if (createMusic) selectedFolders.Add("Music");
        if (createPrefabs) selectedFolders.Add("Prefabs");
        if (createModels) selectedFolders.Add("Models");
        if (createTextures) selectedFolders.Add("Textures");
        if (createEditor) selectedFolders.Add("Editor");

        CreateDirectories("Project", selectedFolders.ToArray());
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("Selected folders created.");
    }

    public static void CreateDirectories(string root, params string[] dirs)
    {
        string fullPath = Path.Combine(Application.dataPath, root);
        foreach (string dir in dirs)
        {
            string newDir = Path.Combine(fullPath, dir);
            if (!Directory.Exists(newDir))
            {
                Directory.CreateDirectory(newDir);
                UnityEngine.Debug.Log($"Created directory: {newDir}");
            }
        }
    }

    public static async Task GettingGitIgnore()
    {
        string folderPath = Application.dataPath;
        string fileUrl = "https://raw.githubusercontent.com/Avin19/UnityTools/main/.gitignore";
        string filePath = Path.Combine(folderPath, ".gitignore");
        await DownloadFileAsync(fileUrl, filePath);
        UnityEngine.Debug.Log("Downloaded .gitignore file.");
    }

    public static void InitializeGitRepository()
    {
        string projectPath = Application.dataPath.Replace("/Assets", "");
        RunGitCommand("init", projectPath);
        RunGitCommand("add .", projectPath);
        RunGitCommand("branch -M main", projectPath);
        RunGitCommand("commit -m \"Initial commit\"", projectPath);

        // Add remote repository
        if (!string.IsNullOrEmpty(RemoteRepositoryURL))
        {
            RunGitCommand($"remote add origin {RemoteRepositoryURL}", projectPath);

            // Push to the remote repository
            RunGitCommand("push -u origin master", projectPath);
        }

        UnityEngine.Debug.Log("Initialized a new Git repository and added remote repository.");
    }


    private static void RunGitCommand(string command, string workingDirectory)
    {
        ProcessStartInfo processStartInfo = new ProcessStartInfo("git", command)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process())
        {
            process.StartInfo = processStartInfo;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                UnityEngine.Debug.Log($"Git command '{command}' executed successfully.\n{output}");
            }
            else
            {
                UnityEngine.Debug.LogError($"Git command '{command}' failed with error:\n{error}");
            }
        }
    }
}
