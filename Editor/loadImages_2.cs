using GLTFast.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using static DownloadModel;


public class loadImages_2 : EditorWindow
{
    private string apiUrl = "https://convrse.ai/projects_by_user?user_id=" + CustomEditorWindow.uid + "&next_page=0&limit=4";
    //private string authorizationToken = "Bearer eyJraWQiOiI5NDI1YmFlZS01OGJiLTQ1NDEtYTJkMi04ZWUxZWM1MTMyYmYiLCJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJxRDRLU0NIbDVMYXIiLCJhdWQiOiJvcHRpbWl6ZXIiLCJuYmYiOjE3MTY0NTA4MzUsInJvbGUiOiJ1c2VyIiwic3R5cGUiOiJ1c2VyIiwiaXNzIjoiaHR0cDovL2F1dGhvcml6YXRvbi1zZXJ2aWNlOjkwMDIiLCJleHAiOjE5NzU2NTA4MzUsImlhdCI6MTcxNjQ1MDgzNX0.C0vp9FHSKaEceqN0Pz0t0oX_DBF0GNUY3oAZvHPl9yfjZbd_LbGxD_255_ELATEG0Prh_9HQAm5jYgi45DyRsamqxePFBHjTzEpHNzffHFJB-lGo_i0cggXBc8fNKoaPujsfUqtWhi4v4c-xGTceIr__LmOLPjUJCN1FZyb_UmTTbpbz4-VbnWEhobdQYZ6UlK-18osZQW8nv6qtzAxoY5NrkHEsd4ZuqlvEz1Dqy8mVhn6plm3k3RHrcx_bU3p7MXf9hG-wS2RZ2a_Hc-ie9Svahq5xFCImoDtAAgWkD7N9rf8TjF8CINEeffBVfAeKnP4pXp351i_LaKkxiL0dRg"; 
    private ProjectData[] projects;
    private Texture2D defaultTexture;
    private Texture2D LoadingTexture;
    private Texture2D QuestionTexture;

    private bool isLoading = false;
    private Texture2D[] placeholders = new Texture2D[10];


    private DownloadModel downloadModel;

    private Vector2 scrollPosition;

    private int currentPage = 0;
    private bool canLoadNextPage = true;


    uploadModel upload;



    [MenuItem("Convrse/Utils/LoadImage")]
    public static void ShowWindow()
    {
        GetWindow<loadImages_2>("testImg3333333");
    } 

    private void OnEnable()
    {

        // Load a default texture from Resources folder
        defaultTexture = Resources.Load<Texture2D>("viewer-placeholder"); //  viewer-placeholderl.png in Resources folder
        LoadingTexture = Resources.Load<Texture2D>("loading");
        QuestionTexture = Resources.Load<Texture2D>("question");
        InitializePlaceholders_empty();
        downloadModel = new DownloadModel();
        upload = new uploadModel(); 
        //EditorCoroutineUtility.StartCoroutineOwnerless(FetchProjectData());


        if (!isLoading)
        {
            isLoading = true;
            //Debug.Log("Repaint > ");
            InitializePlaceholders();
            if (CustomEditorWindow.log)
                Debug.Log("-------------> " + projects.Length);
            EditorCoroutineUtility.StartCoroutineOwnerless(FetchProjectData());
        }

    }

    private void InitializePlaceholders()
    {
        projects = new ProjectData[10];
        for (int i = 0; i < 10; i++)
        {

            if (projects[i] == null)
                projects[i] = new ProjectData { thumbnailTexture = LoadingTexture };
        }
    }

    private void InitializePlaceholders_empty()
    {
        projects = new ProjectData[10];
        for (int i = 0; i < 10; i++)
        {

            if (projects[i] == null)
                projects[i] = new ProjectData { thumbnailTexture = null };
        }
    }

    public void updateApiForFetching(bool increment)
    {
        if (increment)
        {
            currentPage++;
        }
        else
        {
            if (currentPage - 1 >= 0)
                currentPage--;

        }

        apiUrl = "https://convrse.ai/projects_by_user?user_id=" + CustomEditorWindow.uid + "&next_page="+ currentPage + "&limit=4";

    }


    void OnGUI()
    {
        //GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        //Debug.Log("-------------> " + isLoading);
        if (GUILayout.Button("Load Images"))
        {
            //Repaint();
            //Debug.Log("-------------> " + isLoading);
            

            if (!isLoading)
            {
                isLoading = true;
                //Debug.Log("Repaint > ");
                InitializePlaceholders();
                if (CustomEditorWindow.log)
                    Debug.Log("-------------> " + projects.Length);
                EditorCoroutineUtility.StartCoroutineOwnerless(FetchProjectData());
            }
        }
        if (GUILayout.Button("Upload model"))
        {
            uploadModel.ShowWindow();

            //upload.upload();
        }


        if (isLoading)
        {
            GUILayout.Label("Loading...");
        }

        // Start a scroll view
        //scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(600));

        for (int i = 0; i < projects.Length; i++)
        {
            var project = projects[i];
            if (project != null && project.thumbnailTexture != null)
            {
                EditorGUILayout.BeginHorizontal();
                // Thumbnail image
                if (GUILayout.Button(project.thumbnailTexture, GUILayout.Width(150), GUILayout.Height(150)))
                {
                    if (CustomEditorWindow.log)
                        Debug.Log("Project clicked: " + project.project_id);
                    //downloadModel.FetchVersionIdAndDownload(project.project_id);
                }

                EditorGUILayout.BeginVertical();
                // Display the model name
                GUILayout.Label("Model: " + project.name);
                // Display created date
                string rawDate = project.createdAt;
                string presentableDate;
                DateTime date;

                if (DateTime.TryParse(rawDate, out date))
                {
                    presentableDate = date.ToString("MMMM dd, yyyy HH:mm:ss");
                    // Now you can use presentableDate where you need to display the date
                    //Debug.Log("Formatted Date: " + presentableDate);
                }
                else
                {
                    presentableDate = rawDate;
                }


                GUILayout.Label("Created At: " + presentableDate);
                // Button to download the model
                if (GUILayout.Button("Download Model", GUILayout.Width(120)))
                {
                    downloadModel.FetchVersionIdAndDownload(project.project_id);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                //GUILayout.Label("No image available.");
            }
        }
        EditorGUILayout.EndScrollView();

        // Pagination Controls
        GUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(currentPage <= 0);
        if (GUILayout.Button("Previous Page"))
        {
            currentPage--;
            isLoading = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(FetchProjectData());
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!canLoadNextPage);
        if (GUILayout.Button("Next Page"))
        {
            currentPage++;
            isLoading = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(FetchProjectData());
        }
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();


        /*for (int i = 0; i < projects.Length; i++)
        {
            Texture2D displayTexture = projects[i]?.thumbnailTexture ?? null;
            if (!displayTexture)
            {
                continue;
            }
            if (GUILayout.Button(displayTexture, GUILayout.Width(100), GUILayout.Height(100)))
            {

                Debug.Log("len: " + projects.Length);
                downloadModel.FetchVersionIdAndDownload(projects[i].project_id);
                Debug.Log("Project clicked: " + (projects[i] != null ? projects[i].name : "Placeholder"));

            }
        }*/


        /*
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
            for (int i = 0; i < projects.Length; i++)
            {
                if (GUILayout.Button(placeholders[i], GUILayout.Width(100), GUILayout.Height(100)))
                {
                    Debug.Log("Project clicked: " + (projects[i] != null ? projects[i].name : "Placeholder"));
                }
            }
        }
    */


    }



    private IEnumerator FetchProjectData()
    {
        apiUrl = "https://convrse.ai/projects_by_user?user_id=" + CustomEditorWindow.uid + "&next_page=" + currentPage + "&limit=4";
        //apiUrl = "https://convrse.ai/projects_by_user?user_id=" + CustomEditorWindow.uid + "&next_page=0&limit=4"; 
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

                
                //projects = JsonUtility.FromJson<ProjectDataResponse>("{\"data\":" + www.downloadHandler.text + "}").data;
                //int count = Mathf.Min(projects.Length, projects.Length);
                ProjectDataResponse response = JsonUtility.FromJson<ProjectDataResponse>("{\"data\":" + www.downloadHandler.text + "}");
                int count = Mathf.Min(response.data.Length, projects.Length);

                //projects = JsonUtility.FromJson<ProjectDataResponse>(www.downloadHandler.text).data;
                //foreach (var project in projects)
                for (int i = 0; i < count; i++)
                {
                    projects[i] = response.data[i];

                    if (!projects[i].IsUnityNull())
                    {
                        if (!projects[i].thumbnail_url.IsUnityNull())
                        {
                            if (!string.IsNullOrEmpty(projects[i].thumbnail_url[0].thumbnail_url))
                            {
                                //Debug.Log("data :" + projects[i].name);
                                // Debug.Log("data :" + projects[i].thumbnail_url[0].thumbnail_url);
                                EditorCoroutineUtility.StartCoroutineOwnerless(LoadThumbnail(projects[i]));
                            }
                            else
                            {
                                projects[i].thumbnailTexture = defaultTexture;
                            }
                        }
                        else
                        {
                            projects[i].thumbnailTexture = defaultTexture;
                        }
                    }
                }

                // Clear excess placeholders if fewer projects were fetched
                for (int i = count; i < projects.Length; i++)
                {
                    projects[i] = null;
                    //projects[i].thumbnailTexture = null;

                }

                canLoadNextPage = count == 10;
            }
        }
    }

    private IEnumerator LoadThumbnail(ProjectData project)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(project.thumbnail_url[0].thumbnail_url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                if (CustomEditorWindow.log)
                    Debug.Log("done task :" + project.name + " downloaded png");
                project.thumbnailTexture = DownloadHandlerTexture.GetContent(www);
                //placeholders[index] = DownloadHandlerTexture.GetContent(www);
                //project.thumbnailTexture = DownloadHandlerTexture.GetContent(www);
            }
            else
            {
                //placeholders[index] = defaultTexture;
                project.thumbnailTexture = defaultTexture;
                if (CustomEditorWindow.log)
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
        public string createdAt;
        public string size;
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
