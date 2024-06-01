using GLTFast.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using static DownloadModel;


public class bu_loadimage_1 : EditorWindow
{
    private string apiUrl = "https://convrse.ai/projects_by_user?user_id=" + CustomEditorWindow.uid + "&next_page=0&limit=4";
    //private string authorizationToken = "Bearer eyJraWQiOiI5NDI1YmFlZS01OGJiLTQ1NDEtYTJkMi04ZWUxZWM1MTMyYmYiLCJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJxRDRLU0NIbDVMYXIiLCJhdWQiOiJvcHRpbWl6ZXIiLCJuYmYiOjE3MTY0NTA4MzUsInJvbGUiOiJ1c2VyIiwic3R5cGUiOiJ1c2VyIiwiaXNzIjoiaHR0cDovL2F1dGhvcml6YXRvbi1zZXJ2aWNlOjkwMDIiLCJleHAiOjE5NzU2NTA4MzUsImlhdCI6MTcxNjQ1MDgzNX0.C0vp9FHSKaEceqN0Pz0t0oX_DBF0GNUY3oAZvHPl9yfjZbd_LbGxD_255_ELATEG0Prh_9HQAm5jYgi45DyRsamqxePFBHjTzEpHNzffHFJB-lGo_i0cggXBc8fNKoaPujsfUqtWhi4v4c-xGTceIr__LmOLPjUJCN1FZyb_UmTTbpbz4-VbnWEhobdQYZ6UlK-18osZQW8nv6qtzAxoY5NrkHEsd4ZuqlvEz1Dqy8mVhn6plm3k3RHrcx_bU3p7MXf9hG-wS2RZ2a_Hc-ie9Svahq5xFCImoDtAAgWkD7N9rf8TjF8CINEeffBVfAeKnP4pXp351i_LaKkxiL0dRg"; 
    private ProjectData[] projects;
    private Texture2D defaultTexture;
    private Texture2D LoadingTexture;

    private bool isLoading = false;
    private Texture2D[] placeholders = new Texture2D[10];


    private DownloadModel downloadModel;


    [MenuItem("Convrse/Test/bu_loadimage_1")]
    public static void ShowWindow()
    {
        GetWindow<bu_loadimage_1>("bu_loadimage_1");
    }

    private void OnEnable()
    {
        // Load a default texture from Resources folder
        defaultTexture = Resources.Load<Texture2D>("viewer-placeholder"); //  viewer-placeholderl.png in Resources folder
        LoadingTexture = Resources.Load<Texture2D>("loading");

        downloadModel = new DownloadModel();
        //EditorCoroutineUtility.StartCoroutineOwnerless(FetchProjectData());
    }



    void OnGUI()
    {
        //GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        if (GUILayout.Button("Load Images"))
        {
            Repaint();
            Debug.Log("-------------> " + isLoading);
            if (!isLoading)
            {
                isLoading = true;
                Debug.Log("Repaint > ");
                for (int i = 0; i < placeholders.Length; i++)
                {
                    placeholders[i] = LoadingTexture; // Initialize all placeholders to the default texture
                    if (GUILayout.Button(placeholders[i], GUILayout.Width(100), GUILayout.Height(100)))
                    {
                        Debug.Log("Project clicked: " + (projects[i] != null ? projects[i].name : "Placeholder"));
                    }
                }



                EditorCoroutineUtility.StartCoroutineOwnerless(FetchProjectData());
            }
        }
        if (isLoading)
        {
            GUILayout.Label("Loading...");
        }


        if (projects != null)
        {
            foreach (var project in projects)
            {
                if (project != null && project.thumbnailTexture != null)
                {
                    if (GUILayout.Button(project.thumbnailTexture, GUILayout.Width(100), GUILayout.Height(100)))
                    {
                        Debug.Log("Project clicked: " + project.project_id);
                        downloadModel.FetchVersionIdAndDownload(project.project_id);
                    }
                }
                else
                {
                    GUILayout.Label("No image available.");
                }
            }
        }
        else
        {
            //Debug.Log("no data in projects list");
            /*for (int i = 0; i < projects.Length; i++)
            {
                if (GUILayout.Button(placeholders[i], GUILayout.Width(100), GUILayout.Height(100)))
                {
                    Debug.Log("Project clicked: " + (projects[i] != null ? projects[i].name : "Placeholder"));
                }
            }*/
        }


    }



    private IEnumerator FetchProjectData()
    {
        apiUrl = "https://convrse.ai/projects_by_user?user_id=" + CustomEditorWindow.uid + "&next_page=0&limit=4";
        using (UnityWebRequest www = UnityWebRequest.Get(apiUrl))
        {
            www.SetRequestHeader("Authorization", "Bearer " + CustomEditorWindow.AccessToken);
            yield return www.SendWebRequest();


            isLoading = false;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching projects: " + www.error);
            }
            else
            {
                projects = JsonUtility.FromJson<ProjectDataResponse>("{\"data\":" + www.downloadHandler.text + "}").data;


                //projects = JsonUtility.FromJson<ProjectDataResponse>(www.downloadHandler.text).data;
                //foreach (var project in projects)
                for (int i = 0; i < projects.Length; i++)
                {
                    if (!projects[i].IsUnityNull())
                    {
                        if (!string.IsNullOrEmpty(projects[i].thumbnail_url[0].thumbnail_url))
                        {
                            Debug.Log("data :" + projects[i].name);
                            Debug.Log("data :" + projects[i].thumbnail_url[0].thumbnail_url);
                            EditorCoroutineUtility.StartCoroutineOwnerless(LoadThumbnail(projects[i], i));
                        }
                        else
                        {
                            projects[i].thumbnailTexture = defaultTexture;
                        }
                    }
                }
            }
        }
    }

    private IEnumerator LoadThumbnail(ProjectData project, int index)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(project.thumbnail_url[0].thumbnail_url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("done task :" + project.name + " downloaded png");
                project.thumbnailTexture = DownloadHandlerTexture.GetContent(www);
                placeholders[index] = DownloadHandlerTexture.GetContent(www);

            }
            else
            {
                placeholders[index] = defaultTexture;
                project.thumbnailTexture = defaultTexture;
                Debug.LogError("Failed to load thumbnail: " + www.error);
            }
        }
    }


    //////////////////////////////////////////////////////////////////
    ///// Helper classes to deserialize JSON response
    [System.Serializable]
    private class ProjectData
    {
        public string _id;
        public string name;
        public string project_id;
        public Thumbnail[] thumbnail_url;
        public Texture2D thumbnailTexture;
    }

    [System.Serializable]
    private class ProjectDataResponse
    {
        public ProjectData[] data;
    }

    [System.Serializable]
    public class Thumbnail
    {
        public string content_id;
        public string thumbnail_url;
    }

}
