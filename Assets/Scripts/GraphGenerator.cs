using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GraphLayout
{
    PLANE,
    SPHERE,
    CYLINDER
}

public enum NodeInteractionState
{
    HIGHLIGHTED = 4,
    SELECTED = 3,
    DIM = 2,
    NONE = 1
}

public class GraphGenerator : MonoBehaviour {

    public DataSourceType graphType;
    public GraphLayout graphLayout;

    public Vector3 minPlanePt;
    public Vector3 maxPlanePt;

    public Vector3 sphereCenter;
    public float sphereRadius;

    public float cylinderHeight;
    public float cylinderRadius;

    public GameObject nodeObject;

    public Material lineMaterial;

    public GameObject basePlane;
    public GameObject baseSphere;
    public GameObject baseCylinder;

    public float edgeThickness = 0.005f;

    public bool drawEdges = true;
    public bool drawBaseObject = true;


    protected Dictionary<string, NodeInfo> nodeMap;
    protected List<LinkInfo> linkList;
    protected Dictionary<string, List<NodeInfo>> groupMap;
    protected Dictionary<string, GameObject> groupLabelMap;

    string[] outlierNames = {
        "From Hell",
        "League of Extraordinary Gentlemen",
        "Watchmen",
        "Mystery Men"};
        

    // Use this for initialization
    void Start () {

        NodeLinkDataLoader dataLoader = new NodeLinkDataLoader();
        dataLoader.srcType = graphType;
        dataLoader.LoadNodeLinkData();
        populateMaps(dataLoader);
        generate3DPoints();
        generateNodesAndLinks();
        activateBaseObject();
        
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    protected void populateMaps(NodeLinkDataLoader dataLoader)
    {
        nodeMap = new Dictionary<string, NodeInfo>();
        linkList = new List<LinkInfo>();
        groupMap = new Dictionary<string, List<NodeInfo> >();

        Color[] palette = ColorUtils.getColorPalette();
        ColorUtils.randomizeColorPalette(palette);

        Dictionary<int, int> colorSet = new Dictionary<int, int>();
        int currGroup;

        NodeInfo currNode;
        List<NodeInfo> currGrpList;
        foreach (NLNode node in dataLoader.nodes)
        {
            currNode = new NodeInfo();
            currNode.id = node.id;
            currNode.group = node.group;
            currNode.groupId = node.groupId;
            currNode.color = palette[currNode.group % palette.Length];

            nodeMap.Add(node.id, currNode);

            if(!groupMap.TryGetValue(currNode.groupId, out currGrpList ) )
            {
                currGrpList = new List<NodeInfo>();
                groupMap.Add(currNode.groupId, currGrpList);
            }

            currGrpList.Add(currNode);

            if ( !colorSet.TryGetValue(currNode.group, out currGroup))
            {
                colorSet.Add(currNode.group, 0);
            }
        }

        foreach (NLCoord coord in dataLoader.coords)
        {
            if( nodeMap.TryGetValue(coord.id, out currNode))
            {
                currNode.pos2d = new Vector2(coord.x, coord.y);
            }
        }

        NodeInfo startNode;
        NodeInfo endNode;
        LinkInfo currLink;

       

        Random.InitState(97);

        foreach ( NLLink link in dataLoader.links )
        {
            if (nodeMap.TryGetValue(link.source, out startNode) &&
               nodeMap.TryGetValue(link.target, out endNode))
            {
                currLink = new LinkInfo();
                currLink.start = startNode;
                currLink.end = endNode;
                currLink.lineWidth = link.lineWidth;
                currLink.forceValue = Mathf.Sqrt((float)link.value) + Random.value * 0.5f;
                linkList.Add(currLink);
            }
        }

    }

    void generate3DPoints()
    {
        float minX = 0.0f;
        float maxX = 0.0f;
        float minY = 0.0f;
        float maxY = 0.0f;

        NodeInfo currNode;

        NodeInfo special1 = null;
        NodeInfo special2 = null;
        NodeInfo special3 = null;
        NodeInfo special4 = null;

        bool baseMinMaxSet = false;

        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            currNode = entry.Value;

            if( nodeIsOutlier(currNode.id))
            {
                if (special1 == null) special1 = currNode;
                else if (special2 == null) special2 = currNode;
                else if (special3 == null) special3 = currNode;
                else if (special4 == null) special4 = currNode;
                continue;
            }
            else if( !baseMinMaxSet )
            {
                minX = maxX = currNode.pos2d.x;
                minY = maxY = currNode.pos2d.y;
                baseMinMaxSet = true;
            }
            else
            {
                if (currNode.pos2d.x < minX) minX = currNode.pos2d.x;
                else if (currNode.pos2d.x > maxX) maxX = currNode.pos2d.x;

                if (currNode.pos2d.y < minY) minY = currNode.pos2d.y;
                else if (currNode.pos2d.y > maxY) maxY = currNode.pos2d.y;
            }
            
        }

        
        special1.pos2d = new Vector2(minX, minY);
        special2.pos2d = new Vector2(maxX, minY);
        special3.pos2d = new Vector2(maxX, maxY);
        special4.pos2d = new Vector2(minX, maxY);
        
        float xRangeInv = 1.0f / (maxX - minX);
        float yRangeInv = 1.0f / (maxY - minY);

        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            currNode = entry.Value;
            currNode.pos2d.x = (currNode.pos2d.x - minX) * xRangeInv;
            currNode.pos2d.y = (currNode.pos2d.y - minY) * yRangeInv;

        }

        if (graphLayout == GraphLayout.PLANE)
        {
            Vector2 curr2DPt;
            Vector3 dir = maxPlanePt - minPlanePt;
            Vector3 xDir = new Vector3(dir.x, 0.0f, dir.z);
            Vector3 yDir = new Vector3(0.0f, dir.y, 0.0f);

            foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
            {
                currNode = entry.Value;
                curr2DPt = currNode.pos2d;

                currNode.pos3d = xDir * curr2DPt.x + yDir * curr2DPt.y + minPlanePt;
            }
        }

        else if (graphLayout == GraphLayout.SPHERE)
        {
            Vector2 curr2DPt;

            float lonAngle;
            float latAngle;

            Vector3 baseVec = new Vector3(0.0f, 0.0f, sphereRadius);

            Quaternion rotation;

            foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
            {
                currNode = entry.Value;
                curr2DPt = currNode.pos2d;
                lonAngle = curr2DPt.x * 350.0f - 175.0f;
                latAngle = curr2DPt.y * 120.0f - 60.0f;

                rotation = Quaternion.Euler(latAngle, lonAngle, 0.0f);

                currNode.pos3d = rotation * baseVec + sphereCenter;
            }
        }

        else if (graphLayout == GraphLayout.CYLINDER)
        {
            Vector2 curr2DPt;

            float lonAngle;
            float height;

            Vector3 baseVec = new Vector3(0.0f, 0.0f, sphereRadius);

            Quaternion rotation;

            foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
            {
                currNode = entry.Value;
                curr2DPt = currNode.pos2d;
                lonAngle = curr2DPt.x * 350.0f - 175.0f;
                height = curr2DPt.y * cylinderHeight;

                rotation = Quaternion.Euler(0.0f, lonAngle, 0.0f);

                currNode.pos3d = rotation * baseVec + new Vector3(0.0f, height, 0.0f) ;
            }
        }

    }

    void generateNodesAndLinks()
    {
        bool onlyActivateLinkedNodes = false;

        Vector3 graphCenter = Vector3.zero;
        if (graphLayout == GraphLayout.PLANE)
        {
            graphCenter.y = 1.5f;
        }
        else if (graphLayout == GraphLayout.SPHERE)
        {
            graphCenter = sphereCenter;
        }
        else if (graphLayout == GraphLayout.CYLINDER)
        {
            graphCenter.y = cylinderHeight*0.5f;
        }

        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            GameObject point = (GameObject)Instantiate(nodeObject);
            point.name = entry.Value.id;
            point.transform.position = entry.Value.pos3d;
            point.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            entry.Value.nodeObj = point;

            MeshRenderer rend = point.GetComponent<MeshRenderer>();
            rend.material.color = entry.Value.color;

            GameObject nodeLabel = new GameObject();
            nodeLabel.transform.SetParent(point.transform);
            nodeLabel.AddComponent<MeshRenderer>();
            nodeLabel.AddComponent<TextMesh>();
            nodeLabel.AddComponent<CameraOriented>();

            Vector3 dir = graphCenter - entry.Value.pos3d;
            dir.Normalize();
            nodeLabel.transform.position = entry.Value.pos3d + dir * 0.1f;
            nodeLabel.transform.localScale = Vector3.one * 0.5f;

            TextMesh textMesh = nodeLabel.GetComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.text = entry.Value.id;
            textMesh.color = entry.Value.color;
            textMesh.characterSize = 0.1f;
            textMesh.fontSize = 100;

        }
        
        if( onlyActivateLinkedNodes )
        {
            foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
            {
                entry.Value.nodeObj.SetActive(false);
            }
        }

        Vector3 startPt, endPt;
        NodeInfo startInfo, endInfo;
        float sphereCircumference = 2.0f * Mathf.PI * sphereRadius;

        Vector3[] pts = new Vector3[1];

        foreach (LinkInfo link in linkList)
        {
            startInfo = link.start;
            endInfo = link.end;

            startPt = startInfo.pos3d;
            endPt = endInfo.pos3d;

            if ( onlyActivateLinkedNodes )
            {
                startInfo.nodeObj.SetActive(true);
                endInfo.nodeObj.SetActive(true);
            }

            if (graphLayout == GraphLayout.PLANE)
            {
                pts = new Vector3[2];
                pts[0] = startPt;
                pts[1] = endPt;
            }

            else if (graphLayout == GraphLayout.SPHERE)
            {
                Vector3 v1 = startPt - sphereCenter;
                Vector3 v2 = endPt - sphereCenter;

                v1.Normalize();
                v2.Normalize();

                Vector3 rotVector = Vector3.Cross(v1, v2);
                rotVector.Normalize();

                float dot = Vector3.Dot(v1, v2);

                if (dot >= 0.999999f) continue;

                float radAngle = Mathf.Acos(dot);

                float arcDist = radAngle * sphereRadius;
                int numDivisions = (int)Mathf.Ceil(arcDist / 0.03f);

               pts = new Vector3[numDivisions + 1];

                float eulerAngle = radAngle / Mathf.PI * 180.0f / (float)numDivisions;

                Quaternion rotation = Quaternion.AngleAxis(eulerAngle, rotVector);

                pts[0] = v1;
                for (int i = 1; i < pts.Length; i++)
                {
                    pts[i] = rotation * pts[i - 1];
                }

                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = pts[i] * sphereRadius + sphereCenter;
                }

            }

            else if (graphLayout == GraphLayout.CYLINDER)
            {
                Vector3 v1 = startPt;
                Vector3 v2 = endPt;

                v1.y = 0.0f;
                v2.y = 0.0f;

                v1.Normalize();
                v2.Normalize();

                Vector3 rotVector = Vector3.Cross(v1, v2);
                rotVector.Normalize();

                float dot = Vector3.Dot(v1, v2);

                if (dot >= 0.999999f) continue;

                float radAngle = Mathf.Acos(dot);

                float arcDist = radAngle * cylinderRadius;
                int numDivisions = (int)Mathf.Ceil(arcDist / 0.03f);
                
                pts = new Vector3[numDivisions + 1];

                float eulerAngle = radAngle / Mathf.PI * 180.0f / (float)numDivisions;

                Quaternion rotation = Quaternion.AngleAxis(eulerAngle, rotVector);

                pts[0] = v1;
                for (int i = 1; i < pts.Length; i++)
                {
                    pts[i] = rotation * pts[i - 1];
                }

                float heighIncrement = (endPt.y- startPt.y) / (float)numDivisions;
                Vector3 currHeight = new Vector3(0.0f, startPt.y, 0.0f);
                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = pts[i] * cylinderRadius + currHeight;
                    currHeight.y += heighIncrement;
                } 
            }

            GameObject lineObj = new GameObject();
            lineObj.AddComponent<LineRenderer>();
            LineRenderer rend = lineObj.GetComponent<LineRenderer>();
            rend.material = lineMaterial;
            rend.SetWidth(edgeThickness, edgeThickness);
            rend.SetVertexCount(pts.Length);
            rend.SetPositions(pts);
            rend.SetColors(startInfo.color, endInfo.color);

        }


    }

    void activateBaseObject()
    {
        if (!drawBaseObject) return;

        if (graphLayout == GraphLayout.PLANE)
        {
            basePlane.SetActive(true);
            Vector3 v = maxPlanePt - minPlanePt;
            Vector3 up = v;
            up.z = 0.0f;
            float yScale = up.magnitude;
            up.Normalize();
            Vector3 right = maxPlanePt - minPlanePt;
            right.y = 0.0f;
            float xScale = right.magnitude;
            right.Normalize();

            v.Normalize();

            basePlane.transform.forward = Vector3.Cross(right, v);

            basePlane.transform.position = (minPlanePt + maxPlanePt) * 0.5f + basePlane.transform.forward * 0.05f;
            basePlane.transform.localScale = new Vector3(xScale, yScale * 0.5f, 1.0f);
        }

        else if (graphLayout == GraphLayout.SPHERE)
        {
            baseSphere.SetActive(true);
            baseSphere.transform.position = sphereCenter;
            baseSphere.transform.localScale = new Vector3(sphereRadius * 2.05f, sphereRadius * 2.05f, sphereRadius * 2.05f);
        }

        else if (graphLayout == GraphLayout.CYLINDER)
        {
            baseCylinder.SetActive(true);
            baseCylinder.transform.position = new Vector3(0.0f, cylinderHeight * 0.5f, 0.0f);
            baseCylinder.transform.localScale = new Vector3(cylinderRadius * 2.0f, cylinderHeight * 0.5f, cylinderRadius * 2.0f);
        }
    }

    bool nodeIsOutlier(string name)
    {
        for( int i = 0; i < outlierNames.Length; i++ )
        {
            if (name.Length < outlierNames[i].Length) continue;

            if (name.Substring(0, outlierNames[i].Length).CompareTo(outlierNames[i]) == 0) return true;
        }

        return false;
    }

}

public class NodeInfo
{
    public string id;
    public int group;
    public string groupId;
    public Vector2 pos2d;
    public Vector3 pos3d;
    public GameObject nodeObj;
    public Color color;
    public Vector3 dir;
    public bool positionIsStationary = false;
    public NodeInteractionState interState;
    public NodeInteractionState prevInterState;
}

public class LinkInfo
{
    public NodeInfo start;
    public NodeInfo end;
    public float lineWidth;
    public GameObject lineObj;
    public float forceValue;
}
