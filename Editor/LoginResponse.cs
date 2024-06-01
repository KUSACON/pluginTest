[System.Serializable]
public class LoginResponse
{
    public string access_token;
    public string token_type;
    public string refresh_token;
    public User user;
}

[System.Serializable]
public class User
{
    public string email;
    public string username;
    public App app;
    public string f_name;
    public string l_name;
    public string mobile;
    public string uid;
    public bool enabled;
}

[System.Serializable]
public class App
{
    public string name;
    public int id;
}
