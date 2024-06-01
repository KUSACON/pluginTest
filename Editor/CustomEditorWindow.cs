using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class CustomEditorWindow : EditorWindow
{
    private string email = "";
    private string password = "";

    public static string AccessToken;
    public static string uid;

    public static bool log = false;    

    private bool isRequesting = false;
    private UnityWebRequest www;

    [System.Serializable]
    public class LoginPayload
    {
        public string app;
        public string email;
        public string password;
    }


    [MenuItem("Convrse/AssetLoader")]
    public static void ShowWindow()
    {
        GetWindow<CustomEditorWindow>("Convrse Asset Loader");
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        email = EditorGUILayout.TextField("Email", email);
        password = EditorGUILayout.PasswordField("Password", password);

        if (GUILayout.Button("Login"))
        {
            if (CustomEditorWindow.log)
                Debug.Log("onClick login");
            StartLogin(email, password);
            //StartCoroutine(Login(email, password));

        }
    }



    void StartLogin(string email, string password)
    {
        LoginPayload payload = new LoginPayload
        {
            app = "optimizer",
            email = email,
            password = password
        };
        string jsonData = JsonUtility.ToJson(payload);

        www = UnityWebRequest.Put("https://convrse.ai/login", jsonData);
        www.method = "POST";
        www.SetRequestHeader("Content-Type", "application/json");
        www.SendWebRequest();

        isRequesting = true;
        EditorApplication.update += EditorUpdate;
    }


    void EditorUpdate()
    {
        if (!www.isDone)
            return;

        if (isRequesting)
        {
            isRequesting = false;
            EditorApplication.update -= EditorUpdate;

            //if (www.isNetworkError || www.isHttpError)
            //if(www.result == UnityWebRequest.Result.ConnectionError)
            if (www.result != UnityWebRequest.Result.Success)
            {

                Debug.LogError(www.error);
            }
            else
            {
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(www.downloadHandler.text);
                AccessToken = response.access_token;
                uid = response.user.uid;

                if (CustomEditorWindow.log)
                    Debug.Log("Login successful! Token: " + AccessToken);
                if (CustomEditorWindow.log)
                    Debug.Log("Login successful! uId: " + uid);
                loadImages_2.ShowWindow();

            }
        } 
    }
}
