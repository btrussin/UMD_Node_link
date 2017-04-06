using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CMJSONLoader : MonoBehaviour
{

    CMData[] cmData;
    Dictionary<string, List<CMData>> comicMap = new Dictionary<string, List<CMData>>();
    Dictionary<int, List<CMData>> yearMap = new Dictionary<int, List<CMData>>();
    Dictionary<string, List<CMData>> publisherMap = new Dictionary<string, List<CMData>>();
    Dictionary<string, List<CMData>> groupingMap = new Dictionary<string, List<CMData>>();
    Dictionary<string, List<CMData>> distributorMap = new Dictionary<string, List<CMData>>();
    Dictionary<string, List<CMData>> studioMap = new Dictionary<string, List<CMData>>();
    Dictionary<string, List<CMData>> roleMap = new Dictionary<string, List<CMData>>();
    Dictionary<string, List<CMData>> actorMap = new Dictionary<string, List<CMData>>();
    Dictionary<string, List<CMData>> nameMap = new Dictionary<string, List<CMData>>();


    void Start()
    {

    }

    public void LoadData()
    {
        var asset = Resources.Load<TextAsset>("ComicsMovies");

        var cmDataArray = JsonUtility.FromJson<CMDataArray>(asset.text);
        cmData = cmDataArray.data;
        processData();
    }

    void processData()
    {
        for (int i = 0; i < cmData.Length; i++)
        {
            List<CMData> comicList;
            List<CMData> yearList;
            List<CMData> publisherList;
            List<CMData> groupingList;
            List<CMData> distributorList;

            if (!comicMap.TryGetValue(cmData[i].comic, out comicList))
            {
                comicList = new List<CMData>();
                comicMap.Add(cmData[i].comic, comicList);
            }
            comicList.Add(cmData[i]);

            if (!yearMap.TryGetValue(cmData[i].year, out yearList))
            {
                yearList = new List<CMData>();
                yearMap.Add(cmData[i].year, yearList);
            }
            yearList.Add(cmData[i]);

            if (!publisherMap.TryGetValue(cmData[i].publisher, out publisherList))
            {
                publisherList = new List<CMData>();
                publisherMap.Add(cmData[i].publisher, publisherList);
            }
            publisherList.Add(cmData[i]);

            if (!groupingMap.TryGetValue(cmData[i].grouping, out groupingList))
            {
                groupingList = new List<CMData>();
                groupingMap.Add(cmData[i].grouping, groupingList);
            }
            groupingList.Add(cmData[i]);

            if (!distributorMap.TryGetValue(cmData[i].distributor, out distributorList))
            {
                distributorList = new List<CMData>();
                distributorMap.Add(cmData[i].distributor, distributorList);
            }
            distributorList.Add(cmData[i]);



            List<CMData> studioList;
            for (int j = 0; j < cmData[i].studios.Length; j++)
            {
                if (!studioMap.TryGetValue(cmData[i].studios[j], out studioList))
                {
                    studioList = new List<CMData>();
                    studioMap.Add(cmData[i].studios[j], studioList);
                }
                studioList.Add(cmData[i]);
            }

            List<CMData> roleList;
            List<CMData> actorList;
            List<CMData> nameList;
            for (int j = 0; j < cmData[i].roles.Length; j++)
            {
                if (!roleMap.TryGetValue(cmData[i].roles[j].role, out roleList))
                {
                    roleList = new List<CMData>();
                    roleMap.Add(cmData[i].roles[j].role, roleList);
                }
                roleList.Add(cmData[i]);

                if (!actorMap.TryGetValue(cmData[i].roles[j].actor, out actorList))
                {
                    actorList = new List<CMData>();
                    actorMap.Add(cmData[i].roles[j].actor, actorList);
                }
                actorList.Add(cmData[i]);

                if (!nameMap.TryGetValue(cmData[i].roles[j].name, out nameList))
                {
                    nameList = new List<CMData>();
                    nameMap.Add(cmData[i].roles[j].name, nameList);
                }
                nameList.Add(cmData[i]);
            }

        }
    }


    public CMData[] getComicMovieData()
    {
        return cmData;
    }

    public string[] getAllComics()
    {
        string[] vals = new string[comicMap.Keys.Count];
        var e = comicMap.GetEnumerator();
        int idx = 0;
        while (e.MoveNext()) vals[idx++] = e.Current.Key;

        return vals;
    }

    public int[] getAllYears()
    {
        int[] vals = new int[yearMap.Keys.Count];
        var e = yearMap.GetEnumerator();
        int idx = 0;
        while (e.MoveNext()) vals[idx++] = e.Current.Key;

        return vals;
    }

    public string[] getAllPublishers()
    {
        string[] vals = new string[publisherMap.Keys.Count];
        var e = publisherMap.GetEnumerator();
        int idx = 0;
        while (e.MoveNext()) vals[idx++] = e.Current.Key;

        return vals;
    }

    public string[] getAllGroupings()
    {
        string[] vals = new string[groupingMap.Keys.Count];
        var e = groupingMap.GetEnumerator();
        int idx = 0;
        while (e.MoveNext()) vals[idx++] = e.Current.Key;

        return vals;
    }

    public string[] getAllDistributors()
    {
        string[] vals = new string[distributorMap.Keys.Count];
        var e = distributorMap.GetEnumerator();
        int idx = 0;
        while (e.MoveNext()) vals[idx++] = e.Current.Key;

        return vals;
    }

    public string[] getAllStudios()
    {
        string[] vals = new string[studioMap.Keys.Count];
        var e = studioMap.GetEnumerator();
        int idx = 0;
        while (e.MoveNext()) vals[idx++] = e.Current.Key;

        return vals;
    }

    public string[] getAllRoles()
    {
        string[] vals = new string[roleMap.Keys.Count];
        var e = roleMap.GetEnumerator();
        int idx = 0;
        while (e.MoveNext()) vals[idx++] = e.Current.Key;

        return vals;
    }

    public string[] getAllActors()
    {
        string[] vals = new string[actorMap.Keys.Count];
        var e = actorMap.GetEnumerator();
        int idx = 0;
        while (e.MoveNext()) vals[idx++] = e.Current.Key;

        return vals;
    }

    public string[] getAllNames()
    {
        string[] vals = new string[nameMap.Keys.Count];
        var e = nameMap.GetEnumerator();
        int idx = 0;
        while (e.MoveNext()) vals[idx++] = e.Current.Key;

        return vals;
    }

    public List<CMData> getCMDataForComic(string val)
    {
        List<CMData> list = null;
        comicMap.TryGetValue(val, out list);
        return list;
    }

    public List<CMData> getCMDataForYear(int val)
    {
        List<CMData> list = null;
        yearMap.TryGetValue(val, out list);
        return list;
    }

    public List<CMData> getCMDataForPublisher(string val)
    {
        List<CMData> list = null;
        publisherMap.TryGetValue(val, out list);
        return list;
    }

    public List<CMData> getCMDataForGrouping(string val)
    {
        List<CMData> list = null;
        groupingMap.TryGetValue(val, out list);
        return list;
    }

    public List<CMData> getCMDataForDistributor(string val)
    {
        List<CMData> list = null;
        distributorMap.TryGetValue(val, out list);
        return list;
    }

    public List<CMData> getCMDataForStudio(string val)
    {
        List<CMData> list = null;
        studioMap.TryGetValue(val, out list);
        return list;
    }

    public List<CMData> getCMDataForRole(string val)
    {
        List<CMData> list = null;
        roleMap.TryGetValue(val, out list);
        return list;
    }

    public List<CMData> getCMDataForActor(string val)
    {
        List<CMData> list = null;
        actorMap.TryGetValue(val, out list);
        return list;
    }

    public List<CMData> getCMDataForName(string val)
    {
        List<CMData> list = null;
        nameMap.TryGetValue(val, out list);
        return list;
    }


}

public class CMDataArray
{
    public CMData[] data;
}

[System.Serializable]
public class CMData
{
    public string comic;
    public string movie;
    public int year;
    public string publisher;
    public string grouping;
    public string distributor;
    public string[] studios;
    public CMRole[] roles;
}


[System.Serializable]
public class CMRole
{
    public string role;
    public string actor;
    public string name;
    public bool active = true;
}

[System.Serializable]
public class CMType
{
    public uint type;
    public bool active = true;
}
