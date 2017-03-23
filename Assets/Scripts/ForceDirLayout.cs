using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ForceDirLayout : GraphGenerator
{
    int numIter = 0;
    float k = 1.0f;

    float W = 2.0f;
    float L = 2.0f;
    float D = 2.0f;

    float C1 = 0.1f;
    float C2 = 0.05f;
    float C3 = 0.05f;
    float C4 = 0.005f;

    public float maxDistFromCenter;
    public float gravityAmt = 0.0f;
    public bool updateForceLayout = true;

    // Use this for initialization
    void Start () {
        NodeLinkDataLoader dataLoader = new NodeLinkDataLoader();
        dataLoader.srcType = DataSourceType.MOVIES;
        dataLoader.LoadData();
        populateMaps(dataLoader);
        generate3DPoints();
        generateNodesAndLinks();

        k = Mathf.Sqrt(W*L*D / (float)dataLoader.nodes.Length);
        Debug.Log("K: " + k);
    }
	
	// Update is called once per frame
	void Update () {
        if( updateForceLayout )
        {
            recalcPositions();
            updateLineEdges();
        }
    }

    public NodeInfo getNodeInfo(string nodeName)
    {
        NodeInfo info = null;
        if (nodeMap.TryGetValue(nodeName, out info))
        {
            return info;
        }
        else return null;
    }

    float repelForce(float dist)
    {
        if (dist == 0.0f) return 10000.0f;
        return C3 / (dist * dist);
    }

    float attractForce(float dist)
    {
        return C1* Mathf.Log(dist / C2);
    }

    void recalcPositions()
    {

        NodeInfo outerInfo, innerInfo;
        Vector3 tVec;
        float dist;

        // calculate the repel forces for each node
        foreach (KeyValuePair<string, NodeInfo> outerEntry in nodeMap)
        {
            outerInfo = outerEntry.Value;
            outerInfo.dir = Vector3.zero;

            foreach (KeyValuePair<string, NodeInfo> innerEntry in nodeMap)
            {
                if (outerEntry.Key.Equals(innerEntry.Key)) continue;

                innerInfo = innerEntry.Value;

                tVec = outerInfo.pos3d - innerInfo.pos3d;
                if (tVec.sqrMagnitude == 0.0f) tVec = new Vector3(1.0f, 1.0f, 1.0f);

                dist = tVec.magnitude;
                outerInfo.dir += tVec / dist * repelForce(dist);
            }
        }


        // calculate the attract forces for each node
        foreach (LinkInfo link in linkList)
        {
            outerInfo = link.start;
            innerInfo = link.end;

            // dir from inner to outer
            tVec = outerInfo.pos3d - innerInfo.pos3d;

            if (tVec.sqrMagnitude == 0.0f) tVec = new Vector3(0.01f, 0.01f, 1.01f);

            dist = tVec.magnitude;

            tVec = tVec / dist * attractForce(dist);

            outerInfo.dir -= tVec;
            innerInfo.dir += tVec;
        }

        Vector3 tPos;
        Vector3 gravDir;
        float maxDist = 0.0f;
        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            if (entry.Value.positionIsStationary) continue;

            gravDir = sphereCenter - entry.Value.pos3d;
            gravDir.Normalize();
            tPos = entry.Value.pos3d + entry.Value.dir * C4 + gravDir*gravityAmt;

            entry.Value.pos3d = tPos;
            entry.Value.nodeObj.transform.position = tPos;

            gravDir = sphereCenter - entry.Value.pos3d;
            dist = gravDir.magnitude;
            if (dist > maxDist) maxDist = dist;
        }

        maxDistFromCenter = maxDist;

    }

    void generate3DPoints()
    {
        float minX = 0.0f;
        float maxX = 0.0f;
        float minY = 0.0f;
        float maxY = 0.0f;

        NodeInfo currNode;

        bool baseMinMaxSet = false;

        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            currNode = entry.Value;

            if (!baseMinMaxSet)
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

        float xRangeInv = 1.0f / (maxX - minX);
        float yRangeInv = 1.0f / (maxY - minY);

        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            currNode = entry.Value;
            currNode.pos2d.x = (currNode.pos2d.x - minX) * xRangeInv;
            currNode.pos2d.y = (currNode.pos2d.y - minY) * yRangeInv;

        }

        Vector2 curr2DPt;
        Vector3 dir = maxPlanePt - minPlanePt;
        Vector3 xDir = new Vector3(dir.x, 0.0f, dir.z);
        Vector3 yDir = new Vector3(0.0f, dir.y, 0.0f);


        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            currNode = entry.Value;
            curr2DPt = currNode.pos2d;

            //currNode.pos3d = xDir * curr2DPt.x + yDir * curr2DPt.y + minPlanePt;

            currNode.pos3d = Random.insideUnitSphere + sphereCenter;
        }

    }

    void updateLineEdges()
    {
        if (!drawEdges) return;

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

            pts = new Vector3[2];
            pts[0] = startPt;
            pts[1] = endPt;

            GameObject lineObj = link.lineObj;
            LineRenderer rend = lineObj.GetComponent<LineRenderer>();
            rend.SetPositions(pts);
        }
    }

    void generateNodesAndLinks()
    {
        Vector3 graphCenter = Vector3.zero;
        if (graphLayout == GraphLayout.SPHERE)
        {
            graphCenter = sphereCenter;
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

        if (!drawEdges) return;

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

            pts = new Vector3[2];
            pts[0] = startPt;
            pts[1] = endPt;

            GameObject lineObj = new GameObject();
            lineObj.AddComponent<LineRenderer>();
            LineRenderer rend = lineObj.GetComponent<LineRenderer>();
            rend.material = lineMaterial;
            rend.SetWidth(edgeThickness, edgeThickness);
            rend.SetVertexCount(pts.Length);
            rend.SetPositions(pts);
            rend.SetColors(startInfo.color, endInfo.color);

            link.lineObj = lineObj;
        }

    }

}
