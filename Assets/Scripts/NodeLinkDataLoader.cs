using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum DataSourceType
{
    ROLES,
    ACTORS_ACTORS,
    ACTORS,
    MOVIES
}

public class NodeLinkDataLoader {

    public DataSourceType srcType = DataSourceType.MOVIES;

    public NLNode[] nodes;
    public NLLink[] links;
    public NLCoord[] coords;

    public void LoadNodeLinkData()
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

    public void LoadRawData()
    {
        CMJSONLoader cmLoader = new CMJSONLoader();
        cmLoader.LoadData();


        string[] vals = cmLoader.getAllPublishers();
        Debug.Log("Have " + vals.Length + " publishers");
        List<CMData>[] lists = new List<CMData>[vals.Length];
        for (int i = 0; i < vals.Length; i++) lists[i] = cmLoader.getCMDataForPublisher(vals[i]);

        Random.InitState(97);

        SortBySize(vals, lists, 0, vals.Length - 1);
        OrderIntoFourGroups(vals, lists);

        int numNodes = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            numNodes += lists[i].Count;
        }



        nodes = new NLNode[numNodes];

        Debug.Log("Have " + nodes.Length + " nodes");

        int idx = 0;
        for (int i = 0; i < vals.Length; i++)
        {
            foreach( CMData data in lists[i])
            {
                NLNode node = new NLNode();
                node.id = getIdFromCMData(data);
                node.group = i;
                node.groupId = vals[i];
                nodes[idx] = node;

                idx++;
            }
        }

        

        List<NLLink> tList = new List<NLLink>();
        CMData[] cmData = cmLoader.getComicMovieData();

        Debug.Log("Have " + cmData.Length + " CMData");

        int numConnections = 0;

        for ( int i = 0; i < cmData.Length; i++ )
        {
            for (int j = i+1; j < cmData.Length; j++)
            {
                numConnections = 0;
                for( int m = 0; m < cmData[i].roles.Length; m++ )
                {
                    for (int n = 0; n < cmData[j].roles.Length; n++)
                    {
                        if(cmData[i].roles[m].actor.Equals(cmData[j].roles[n].actor))
                        {
                            numConnections++;
                        }
                    }
                }

                if(numConnections > 0)
                {
                    NLLink link = new NLLink();
                    link.source = getIdFromCMData(cmData[i]);
                    link.target = getIdFromCMData(cmData[j]);
                    link.value = numConnections;
                    link.lineWidth = 0.001f * Mathf.Max(Mathf.Min(numConnections - 1, 5), 1.0f);
                    tList.Add(link);
                }

            }
        }

        links = new NLLink[tList.Count];
        tList.CopyTo(links);

        coords = new NLCoord[nodes.Length];

        for (int i = 0; i < coords.Length; i++)
        {
            NLCoord c = new NLCoord();
            c.id = nodes[i].id;
            c.x = 0.0f;
            c.y = 0.0f;
            coords[i] = c;
        }

    }


    void swapElements(string[] vals, List<CMData>[] lists, int i, int j)
    {
        string t = vals[i];
        List<CMData> tList = lists[i];

        vals[i] = vals[j];
        lists[i] = lists[j];

        vals[j] = t;
        lists[j] = tList;
        return;
    }


    void SortBySize(string[] vals, List<CMData>[] lists, int beginIdx, int endIdx)
    {
        int idxDist = endIdx - beginIdx;
        if (idxDist < 1) return;
        else if (idxDist == 1)
        {
            if (lists[beginIdx].Count > lists[endIdx].Count) swapElements(vals, lists, beginIdx, endIdx);
            return;
        }

        int midIdx = (beginIdx + endIdx) / 2;
        int countVal = lists[midIdx].Count;

        swapElements(vals, lists, midIdx, endIdx);


        int s = beginIdx;
        int e = endIdx - 1;

        while (s < e)
        {
            if (lists[s].Count > countVal)
            {
                swapElements(vals, lists, s, e);
                e--;
            }
            else s++;
        }

        int divider = midIdx;

        for (int i = beginIdx; i < endIdx; i++)
        {
            if (lists[i].Count > countVal)
            {
                divider = i;
                swapElements(vals, lists, divider, endIdx);
                break;
            }
        }

        SortBySize(vals, lists, beginIdx, divider - 1);
        SortBySize(vals, lists, divider + 1, endIdx);
    }



    void OrderIntoFourGroups(string[] vals, List<CMData>[] lists)
    {
        List<string>[] sLinsts = new List<string>[4];
        List<List<CMData>>[] lLinsts = new List<List<CMData>>[4];
        for (int i = 0; i < 4; i++)
        {
            sLinsts[i] = new List<string>();
            lLinsts[i] = new List<List<CMData>>();
        }

        for (int i = 0; i < vals.Length; i += 4)
        {
            for (int j = 0; j < 4 && j + i < vals.Length; j++)
            {
                sLinsts[j].Add(vals[i + j]);
                lLinsts[j].Add(lists[i + j]);
            }
        }

        int sIIdx = 0;
        int lIIdx = 0;
        int tIdx = 0;
        for (int i = 0; i < 4; i++)
        {
            foreach (string s in sLinsts[tIdx % 4])
            {
                vals[sIIdx++] = s;
            }

            foreach (List<CMData> l in lLinsts[tIdx % 4])
            {
                lists[lIIdx++] = l;
            }

            tIdx += 3;
        }

    }


    public static string getIdFromCMData(CMData data)
    {
        return data.movie + " " + data.year;
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
    public string groupId;
}

[System.Serializable]
public class NLLink
{
    public string source;
    public string target;
    public int value;
    public float lineWidth;
}

[System.Serializable]
public class NLCoord
{
    public string id;
    public float x;
    public float y;
}




