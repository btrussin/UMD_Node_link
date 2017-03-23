using UnityEngine;
using System.Collections;

public enum DataSourceType
{
    ROLES,
    ACTORS_ACTORS,
    ACTORS,
    MOVIES
}

public class NodeLinkDataLoader {

    public DataSourceType srcType = DataSourceType.ROLES;

    public NLNode[] nodes;
    public NLLink[] links;
    public NLCoord[] coords;

    public void LoadData()
    {
        string srcName = "";
        switch(srcType)
        {
            case DataSourceType.ROLES:
                srcName = "roles";
                break;

            case DataSourceType.ACTORS_ACTORS:
                srcName = "actors_actors";
                break;

            case DataSourceType.ACTORS:
                srcName = "actors1";
                break;

            case DataSourceType.MOVIES:
                srcName = "movies";
                break;
        }
        var asset = Resources.Load<TextAsset>(srcName);

        var dataObj = JsonUtility.FromJson<NLData>(asset.text);
        nodes = dataObj.nodes;
        links = dataObj.links;
        coords = dataObj.coords;
        
    }
}


public class NLData
{
    public NLNode[] nodes;
    public NLLink[] links;
    public NLCoord[] coords;
}

[System.Serializable]
public class NLNode
{
    public string id;
    public int group;
}

[System.Serializable]
public class NLLink
{
    public string source;
    public string target;
    public int value;
}

[System.Serializable]
public class NLCoord
{
    public string id;
    public float x;
    public float y;
}
