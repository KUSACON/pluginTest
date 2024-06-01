using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class DownloadModel : EditorWindow
{

    string modelUrl = "";
    string VERSION_ID = "";

    string PROJECT_ID = "";

    string localPath = "Assets/Convrse/01";



    [System.Serializable]
    public class Response
    {
        public string url;
    }

    [System.Serializable]
    public class VersionIdPayload
    {
        public string version_id;
    }

    [System.Serializable]
    public class ProjectVersion
    {
        public string version_id;
    }

    [System.Serializable]
    public class ProjectVersionsResponse
    {
        public ProjectVersion[] versions;
    }



    [MenuItem("Convrse/DownloadModel")]
    public static void ShowWindow()
    {
        GetWindow<DownloadModel>("Download Model");
    }


    void OnGUI()
    {
        GUILayout.Label("Model URL", EditorStyles.boldLabel);
        modelUrl = EditorGUILayout.TextField("URL:", modelUrl);
        VERSION_ID = EditorGUILayout.TextField("Version ID:", VERSION_ID);
        PROJECT_ID = EditorGUILayout.TextField("PROJECT ID:", PROJECT_ID);


        if (GUILayout.Button("Download Model with link"))
        {
            StartDownload_with_Link(modelUrl);
        }

        if (GUILayout.Button("Download Model with version id"))
        {
            StartDownload_with_VersionID(VERSION_ID);
        }

        if (GUILayout.Button("Download Model with PROJECT id"))
        {
            FetchVersionIdAndDownload(PROJECT_ID);
        }
    }

    void StartDownload_with_Link(string url)
    {
        // Extract the base file name without query parameters
        string fileName = Path.GetFileName(url);
        int queryIndex = fileName.IndexOf('?');
        if (queryIndex > -1)
        {
            fileName = fileName.Substring(0, queryIndex);
        }

        // Ensure the target directory exists
        if (!Directory.Exists(localPath))
        {
            Directory.CreateDirectory(localPath);
        }

        string filePath = Path.Combine(localPath, fileName);
        Debug.Log($"Downloading to: {filePath}");

        ////////////////////////////////////////////////////////////////////
        ///
        UnityWebRequest www = UnityWebRequest.Get(url);
        var downloadHandler = new DownloadHandlerFile(filePath);
        www.downloadHandler = downloadHandler;

        EditorApplication.CallbackFunction onDownloadUpdate = null; // Declare the variable first
        onDownloadUpdate = () =>
        {
            if (www.isDone)
            {
                EditorApplication.update -= onDownloadUpdate;
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error downloading model: " + www.error);
                }
                else
                {
                    Debug.Log("Model downloaded successfully!");
                    AssetDatabase.Refresh(); // Refresh the AssetDatabase to show the new file.
                }

                www.Dispose();
            }
        };

        EditorApplication.update += onDownloadUpdate;
        www.SendWebRequest();
    }


    void StartDownload_with_VersionID(string versionId)
    {
        if (CustomEditorWindow.log)
            Debug.Log(CustomEditorWindow.AccessToken);
        string apiUrl = "https://optimizer.convrse.ai/signed_url";

        VersionIdPayload payload = new VersionIdPayload { version_id = versionId };
        string jsonData = JsonUtility.ToJson(payload);

        //Debug.Log("JSON Data: " + jsonData);  // Should log {"version_id":"your_version_id_here"}


        UnityWebRequest www = new UnityWebRequest(apiUrl, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer "+ CustomEditorWindow.AccessToken);  // Replace 'your_jwt_token_here' with your actual JWT token.

        //Debug.Log(jsonData);



        EditorApplication.CallbackFunction onApiResponse = null;
        onApiResponse = () =>
        {
            if (www.isDone)
            {
                EditorApplication.update -= onApiResponse;
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error while fetching model URL: " + www.error);
                    if (CustomEditorWindow.log)
                        Debug.LogError("Error while fetching model URL: " + www.downloadHandler.text);

                }
                else
                {
                    try
                    {
                        var response = JsonUtility.FromJson<Response>(www.downloadHandler.text);
                        if (response != null && !string.IsNullOrEmpty(response.url))
                        {
                            if (CustomEditorWindow.log)
                                Debug.Log("Model URL fetched.");
                            StartDownload_with_Link(response.url); // Now start the download with the fetched URL
                        }
                        else
                        {
                            if (CustomEditorWindow.log)
                                Debug.LogError("Failed to parse response or URL is empty.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        if (CustomEditorWindow.log)
                            Debug.LogError("Exception parsing response: " + ex.Message);
                    }
                }

                www.Dispose();
            }
        };

        EditorApplication.update += onApiResponse;
        www.SendWebRequest();
    }

    // Method to fetch version id and start download
    public void FetchVersionIdAndDownload(string projectId)
    {
        string apiUrl = $"https://optimizer.convrse.ai/project/versions/{projectId}";
        UnityWebRequest www = UnityWebRequest.Get(apiUrl);
        www.SetRequestHeader("Authorization", "Bearer " + CustomEditorWindow.AccessToken);  // Use your JWT token here

        EditorApplication.CallbackFunction onVersionFetch = null;
        onVersionFetch = () =>
        {
            if (www.isDone)
            {
                EditorApplication.update -= onVersionFetch;
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    if (CustomEditorWindow.log)
                        Debug.LogError("Error fetching version ID: " + www.error);
                }
                else
                {
                    try
                    {
                        var response = JsonUtility.FromJson<ProjectVersionsResponse>("{\"versions\":" + www.downloadHandler.text + "}");

                        //ProjectVersion[] versions = JsonUtility.FromJson<ProjectVersion[]>(www.downloadHandler.text);
                        if (response.versions.Length > 0 && !string.IsNullOrEmpty(response.versions[0].version_id))
                        {
                            if (CustomEditorWindow.log)
                                Debug.Log("Version ID fetched: " + response.versions[response.versions.Length-1].version_id);
                            StartDownload_with_VersionID(response.versions[response.versions.Length - 1].version_id); // Download model using the fetched version ID
                        }
                        else
                        {
                            if (CustomEditorWindow.log)
                                Debug.LogError("No valid version ID found or empty response.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        if (CustomEditorWindow.log)
                            Debug.LogError("Exception parsing version ID: " + ex.Message);
                    }
                }
                www.Dispose();
            }
        };
        EditorApplication.update += onVersionFetch;
        www.SendWebRequest();
    }
}
