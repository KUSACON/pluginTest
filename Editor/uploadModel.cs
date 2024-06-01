using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;
using static DownloadModel;

public class uploadModel : EditorWindow
{
    private string apiUrl = "https://convrse.ai/b/filemanager/upload-policy";
    private string fileName = "office.glb"; // This can be dynamic based on user input

    private string filePath;

    UploadPolicy policy;

    int sizeOfFile = 0;

    string featureName = "HIGH_MODEL_UPLOAD";

    private string projectId;
    private string versionId;

    [MenuItem("Convrse/Utils/uploadFile")]
    public static void ShowWindow()
    {
        GetWindow<uploadModel>("upload");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Select File"))
        {
            filePath = EditorUtility.OpenFilePanel("Select model file", "", "glb");
            if (!string.IsNullOrEmpty(filePath))
            {
                Debug.Log("File selected: " + filePath);
            }
        }

        if (!string.IsNullOrEmpty(filePath))
        {
            if (GUILayout.Button("Upload File"))
            {
                // Assuming you have a method to start the upload process
                upload();
            }
        }

        /*if (GUILayout.Button("test "))
        {

            EditorCoroutineUtility.StartCoroutineOwnerless(CreateVersionForOptimization());
            // Assuming you have a method to start the upload process
            //upload();
        }*/



        //
    }


    public void upload()
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(UploadModel());
    }

    private IEnumerator UploadModel()
    {
        var request = new UnityWebRequest(apiUrl, "POST");
        string bodyJsonString = $"{{\"fileName\":\"{fileName}\", \"featureName\":\"{featureName}\"}}";
        //var bodyJsonString = JsonUtility.ToJson(new { fileName = fileName, featureName = "MODEL_UPLOAD" });
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer "+ CustomEditorWindow.AccessToken); 

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error getting upload policy: " + request.error);
        }
        else
        {
            Debug.Log("Received upload policy");
            string json = request.downloadHandler.text;
            json = json.Replace("\"x-amz-algorithm\":", "\"x_amz_algorithm\":")
                       .Replace("\"x-amz-credential\":", "\"x_amz_credential\":")
                       .Replace("\"x-amz-date\":", "\"x_amz_date\":")
                       .Replace("\"x-amz-signature\":", "\"x_amz_signature\":");

            //response = JsonUtility.FromJson<ResponseClass>(json);
            policy = JsonUtility.FromJson<UploadPolicy>(json);
            //StartFileUpload(policy);
            EditorCoroutineUtility.StartCoroutineOwnerless(UploadToS3(filePath,policy));
            //Debug.Log(policy.policy);
            //Debug.Log(policy.x_amz_algorithm);
            //Debug.Log(policy.x_amz_credential);
            //Debug.Log(policy.x_amz_date);
            //Debug.Log(policy.x_amz_signature);
            //Debug.Log(policy.expiration);
            //Debug.Log(policy.bucket);
            //Debug.Log(policy.key);
            Debug.Log("content if :" + policy.contentId);
            //Debug.Log(policy.filePath);


        }
    }

    IEnumerator UploadToS3(string filePath, UploadPolicy _policy)
    {
        WWWForm form = new WWWForm();
        form.AddField("key", _policy.key);
        form.AddField("Policy", _policy.policy);
        form.AddField("X-Amz-Algorithm", _policy.x_amz_algorithm);
        form.AddField("X-Amz-Credential", _policy.x_amz_credential);
        form.AddField("X-Amz-Date", _policy.x_amz_date);
        form.AddField("X-Amz-Signature", _policy.x_amz_signature);
        form.AddField("x-amz-meta-contentid", _policy.contentId);
        //public string[] _filePath;

        byte[] fileData = File.ReadAllBytes(filePath);
        sizeOfFile = fileData.Length;

        form.AddBinaryData("file", fileData, Path.GetFileName(filePath));
        //form.AddBinaryData("file", fileData, System.IO.Path.GetFileName(_filePath[0]));

        Debug.Log(fileData.Length);


        UnityWebRequest www = UnityWebRequest.Post("https://convrseai-model-optimizer.s3-accelerate.amazonaws.com/", form);
        //www.SetRequestHeader("Content-Type", "application/json");
        //www.SetRequestHeader("Authorization", "Bearer " + CustomEditorWindow.AccessToken);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error uploading file: " + www.error);
        }
        else
        {
            Debug.Log("File successfully uploaded: " + www.downloadHandler.text);
            EditorCoroutineUtility.StartCoroutineOwnerless(CreateContent(policy.key, policy.contentId, featureName));
        }
    }

    IEnumerator CreateContent(string filePath, string contentId, string featureName)
    {
        string createContentAPI = "https://convrse.ai/b/filemanager/content";

        RequestBodyData rbd = new RequestBodyData();
        rbd.filePath = filePath;
        rbd.contentId = contentId;
        rbd.featureName = featureName;

        string json = JsonUtility.ToJson(rbd);

        UnityWebRequest request = new UnityWebRequest(createContentAPI, "POST");
        byte[] jsonToSend = new UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + CustomEditorWindow.AccessToken);


        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error notifying upload completion: " + request.error);
        }
        else
        {
            Debug.Log("Upload completion Create Content successfully: " + request.downloadHandler.text);
            EditorCoroutineUtility.StartCoroutineOwnerless(CreateProject());
        }
    }

    //create project 
    IEnumerator CreateProject()
    {

        //string createProjectApiUrl = "https://convrse.ai/project";
        string createProjectApiUrl = "https://f6d3-43-249-52-98.ngrok-free.app/project";

        createPojectData rbd = new createPojectData();
        rbd.content_id = policy.contentId;
        rbd.name = "mode_" + Random.Range(0,1000);
        rbd.size = sizeOfFile;
        rbd.viewer_enabled = true;

        string json = JsonUtility.ToJson(rbd);
        
        Debug.Log("JSON being sent: " + json);


        UnityWebRequest request = new UnityWebRequest(createProjectApiUrl, "POST");
        byte[] jsonToSend = new UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + CustomEditorWindow.AccessToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error creating project: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            ProjectResponseData responseData = JsonUtility.FromJson<ProjectResponseData>(jsonResponse);

            // Save project and version IDs
            projectId = responseData.project_id;
            versionId = responseData.version_id;

            Debug.Log("Project created successfully.");
            Debug.Log("Project ID: " + projectId);
            Debug.Log("Version ID: " + versionId);
            //Debug.Log("project created successfully: " + request.downloadHandler.text);
            EditorCoroutineUtility.StartCoroutineOwnerless(CreateVersionForOptimization());
        }
    }


    IEnumerator CreateVersionForOptimization ()
    {

        //yield return new WaitForSeconds(5);

        //string createVersion_Api_Url = "https://staging.kubernetes.convrse.ai/project/" + projectId + "/version/" + versionId;
        //string createVersion_Api_Url = "https://staging.kubernetes.convrse.ai/project/versions/"+ projectId;
        //////string createVersion_Api_Url = "https://convrse.ai/project/version/" + projectId;
        string createVersion_Api_Url = "https://f6d3-43-249-52-98.ngrok-free.app/project/version/" + projectId;

        Debug.Log(createVersion_Api_Url);

        var projectData = new ProjectData
        {
            decimation_ratio = 0.6f,
            texture_resize_value = 1,
            toggle_triangulate = false,
            toggle_symmetry = false,
            export_format = "EXPORT_ORIGINAL_FORMAT",
            bake_normals = false,
            normal_format = "",
            bake_ao = false,
            ao_format = "",
            preserve_pbr = false,
            draco_flag = false,
            optimization_mode = "",
            optimization_id = "",
            optimize_low_mesh = false,
            hide_from_optimization = new string[] { }
        };

        string json = JsonUtility.ToJson(projectData);
        var request = new UnityWebRequest(createVersion_Api_Url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + CustomEditorWindow.AccessToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error creating project: " + request.error);

            Debug.Log("error " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Project created successfully: " + request.downloadHandler.text);
        }
    }


    [System.Serializable]
    public class UploadPolicy
    {
        public string policy;
        public string x_amz_algorithm;
        public string x_amz_credential;
        public string x_amz_date;
        public string x_amz_signature;
        public string expiration;
        public string bucket;
        public string key;
        public string contentId;
        public string filePath;

    }

    [System.Serializable]
    public class RequestBodyData
    {
        public string filePath;
        public string contentId;
        public string featureName;
    }

    //to create project
    [System.Serializable]
    public class ProjectData
    {
        public double decimation_ratio;
        public int texture_resize_value;
        public bool toggle_triangulate;
        public bool toggle_symmetry;
        public string export_format;
        public bool bake_normals;
        public string normal_format;
        public bool bake_ao;
        public string ao_format;
        public bool preserve_pbr;
        public bool draco_flag;
        public string optimization_mode;
        public string optimization_id;
        public bool optimize_low_mesh;
        public string[] hide_from_optimization;
    }

    [System.Serializable]
    public class createPojectData
    {
        public string content_id;
        public string name;
        public int size;
        public bool viewer_enabled = false;
    }

    [System.Serializable]
    public class ProjectResponseData
    {
        public string project_id;
        public string version_id;
    }







}
