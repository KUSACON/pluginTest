using System.Collections.Generic;

[System.Serializable]
public class ProjectResponse
{
    public List<Project_json> projects;
}

[System.Serializable]
public class Project_json
{
    public string _id;
    public string project_id;
    public string uid;
    public string content_id;
    public string name;
    public int size;
    public string status;
    public string createdAt;
    public string viewer_url;
    public int triangle_count;
    public int vertices;
    public int versions;
    public string version_id;
    public Thumbnail[] thumbnail_url;
}

[System.Serializable]
public class Thumbnail
{
    public string content_id;
    public string thumbnail_url;
}
