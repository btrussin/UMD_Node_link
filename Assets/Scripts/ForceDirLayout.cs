using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ForceDirLayout : GraphGenerator
{
    public int numHighlighed = 0;

    int numIter = 0;
   
    float W = 2.0f;
    float L = 2.0f;
    float D = 2.0f;

    
    float C1 = 0.1f;
    float C2 = 0.05f;
    float C3 = 0.05f;
    float C4 = 0.005f;
    
    public float gravityAmt = 0.0f;
    public bool updateForceLayout = true;
    public int maxFramesForLayout = 1200;

    ForceDirTrackedObject grabObject1 = null;
    ForceDirTrackedObject grabObject2 = null;
    ForceDirTrackedObject activeGrabObject = null;

    Vector3 initialScale = Vector3.zero;
    float initialDist = 0;
    Quaternion initialRotation;
    Vector3 inititalOffset = Vector3.zero;

    bool activeScale = false;
    bool activeMove = false;

    bool alwaysShowLines = false;

    public float edgeBrightness_selected = 0.75f;
    public float edgeBrightness_highlighted = 0.95f;
    public float edgeBrightness_none = 0.0f;   // 0.5 (orig)
    public float edgeBrightness_dimmed = 0.0f;    // 0.35 (orig)

    public float nodeBrightness_selected = 0.75f;
    public float nodeBrightness_highlighted = 0.95f;
    public float nodeBrightness_none = 0.5f;
    public float nodeBrightness_dimmed = 0.35f;

    public GameObject leftController;
    public GameObject rightController;

    ForceDirTrackedObject leftContManager;
    ForceDirTrackedObject rightContManager;

    // Use this for initialization
    void Start () {

        sphereCenter = gameObject.transform.position;

        NodeLinkDataLoader dataLoader = new NodeLinkDataLoader();
        dataLoader.srcType = DataSourceType.MOVIES;
        //dataLoader.LoadNodeLinkData();
        dataLoader.LoadRawData();
        populateMaps(dataLoader);
        generate3DPoints();
        generateNodesAndLinks();

        if (leftController != null) leftContManager = leftController.GetComponent<ForceDirTrackedObject>();
        if (rightController != null) rightContManager = rightController.GetComponent<ForceDirTrackedObject>();

    }
	
	// Update is called once per frame
	void Update () {

        sphereCenter = gameObject.transform.position;

        if ( updateForceLayout )
        {
            recalcPositions();
            updateNodePositions();
 
            if (Time.frameCount == maxFramesForLayout)
            {
                updateForceLayout = false;
            }
        }

        updateScale();
        updateMove();
        updateInfoBasedOnNodeMovementInWorld();

        updateLineEdges();
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

                //tVec = outerInfo.pos3d - innerInfo.pos3d;
                tVec = outerInfo.nodeObj.transform.position - innerInfo.nodeObj.transform.position;
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
            //tVec = outerInfo.pos3d - innerInfo.pos3d;
            tVec = outerInfo.nodeObj.transform.position - innerInfo.nodeObj.transform.position;

            if (tVec.sqrMagnitude == 0.0f) tVec = new Vector3(0.01f, 0.01f, 1.01f);

            dist = tVec.magnitude;

            tVec = tVec / dist * attractForce(dist) * link.forceValue;

            outerInfo.dir -= tVec;
            innerInfo.dir += tVec;
        }




        Vector3 tPos;
        Vector3 gravDir;
        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            if (entry.Value.positionIsStationary) continue;

            //gravDir = sphereCenter - entry.Value.pos3d;
            gravDir = sphereCenter - entry.Value.nodeObj.transform.position;
            gravDir.Normalize();
            //tPos = entry.Value.pos3d + entry.Value.dir * C4 + gravDir * gravityAmt;
            tPos = entry.Value.nodeObj.transform.position + entry.Value.dir * C4 + gravDir * gravityAmt;

            entry.Value.pos3d = tPos;
            entry.Value.nodeObj.transform.position = tPos;

            //gravDir = sphereCenter - entry.Value.pos3d;
            gravDir = sphereCenter - tPos;
            dist = gravDir.magnitude;
        }
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

    void generateNodesAndLinks()
    {
        Vector3 graphCenter = Vector3.zero;
        if (graphLayout == GraphLayout.SPHERE)
        {
            graphCenter = sphereCenter;
        }

        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            entry.Value.interState = NodeInteractionState.NONE;
            entry.Value.prevInterState = NodeInteractionState.NONE;

            GameObject point = (GameObject)Instantiate(nodeObject);
            point.name = entry.Value.id;
            point.transform.localPosition = entry.Value.pos3d;
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

            point.transform.SetParent(gameObject.transform);

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

            pts = new Vector3[2];
            pts[0] = startPt;
            pts[1] = endPt;

            GameObject lineObj = new GameObject();
            lineObj.AddComponent<LineRenderer>();
            LineRenderer rend = lineObj.GetComponent<LineRenderer>();
            rend.material = lineMaterial;
            rend.SetWidth(link.lineWidth, link.lineWidth);
            rend.SetVertexCount(pts.Length);
            rend.SetPositions(pts);

            rend.SetColors(startInfo.color * 0.0f, endInfo.color * 0.0f);
            
            link.lineObj = lineObj;
        }



        groupLabelMap = new Dictionary<string, GameObject>();

        foreach (KeyValuePair<string, List<NodeInfo>> kvPair in groupMap)
        {
            GameObject nodeLabel = new GameObject();
            nodeLabel.name = kvPair.Key + " [label]";
            nodeLabel.AddComponent<MeshRenderer>();
            nodeLabel.AddComponent<TextMesh>();
            nodeLabel.AddComponent<CameraOriented>();

            TextMesh textMesh = nodeLabel.GetComponent<TextMesh>();
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.text = kvPair.Key;
            //textMesh.color = Color.red;
            textMesh.characterSize = 0.01f;
            textMesh.fontSize = 100;

           
            foreach(NodeInfo ni in kvPair.Value)
            {
                textMesh.color = ni.color;
                break;
            }

            groupLabelMap.Add(kvPair.Key, nodeLabel);

        }

    }


    

    void updateNodePositions()
    {
        int highlightGrpLt = -1;
        int highlightGrpRt = -1;

        switch (numHighlighed)
        {
            case 0:
                foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
                {
                    entry.Value.nodeObj.transform.position = entry.Value.pos3d;

                    MeshRenderer rend = entry.Value.nodeObj.GetComponent<MeshRenderer>();

                    switch (entry.Value.interState)
                    {
                        case NodeInteractionState.SELECTED:
                            rend.material.color = entry.Value.color * nodeBrightness_selected;
                            break;
                        default:
                            rend.material.color = entry.Value.color * nodeBrightness_none;
                            break;
                    }

                }
                break;

            default:

                highlightGrpLt = leftContManager.highlightGrp;
                highlightGrpRt = rightContManager.highlightGrp;

                foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
                {
                    entry.Value.nodeObj.transform.position = entry.Value.pos3d;

                    MeshRenderer rend = entry.Value.nodeObj.GetComponent<MeshRenderer>();

                    if(entry.Value.group == highlightGrpLt || entry.Value.group == highlightGrpRt)
                    {
                        rend.material.color = entry.Value.color * nodeBrightness_highlighted;
                    }
                    else if(entry.Value.interState == NodeInteractionState.SELECTED)
                    {
                        rend.material.color = entry.Value.color * nodeBrightness_selected;
                    }
                    else
                    {
                        rend.material.color = entry.Value.color * nodeBrightness_dimmed;
                    }

                }

                break;
        }

        updateGroupLabels();

    }

    void updateGroupLabels()
    {
        GameObject labelObj = null;
        Vector3 averagePosition = Vector3.zero;
        int numNodes = 0;
        Vector3 offset = new Vector3(0.0f, 0.08f, 0.0f);
        foreach (KeyValuePair<string, List<NodeInfo>> entry in groupMap)
        {
            averagePosition = Vector3.zero;
            numNodes = 0;
            foreach(NodeInfo info in entry.Value)
            {
                averagePosition += info.nodeObj.transform.position;
                numNodes++;
            }

            averagePosition /= (float)numNodes;

            float minDist = float.MaxValue;
            float currDist;
            GameObject closestObj = null;

            Vector3 distVec;


            foreach (NodeInfo info in entry.Value)
            {
                if( closestObj == null ) closestObj = info.nodeObj;

                distVec = averagePosition - info.nodeObj.transform.position;
                currDist = distVec.sqrMagnitude;
                if( currDist+0.02f < minDist )
                {
                    minDist = currDist;
                    closestObj = info.nodeObj;
                }
            }

            groupLabelMap.TryGetValue(entry.Key, out labelObj);
            labelObj.transform.SetParent(closestObj.transform);
            labelObj.transform.position = closestObj.transform.position + offset;

        }
    }

    void updateLineEdges()
    {
        if (!drawEdges) return;

        NodeInfo startInfo, endInfo;
        float sphereCircumference = 2.0f * Mathf.PI * sphereRadius;

        Vector3[] pts = new Vector3[1];

        NodeInteractionState maxState;

        foreach (LinkInfo link in linkList)
        {
            startInfo = link.start;
            endInfo = link.end;

            if (startInfo.interState > endInfo.interState) maxState = startInfo.interState;
            else maxState = endInfo.interState;

            pts = new Vector3[2];
            pts[0] = startInfo.nodeObj.transform.position;
            pts[1] = endInfo.nodeObj.transform.position;

            GameObject lineObj = link.lineObj;
            LineRenderer rend = lineObj.GetComponent<LineRenderer>();
            rend.SetPositions(pts);

            switch (numHighlighed)
            {
                case 0:
                    switch (maxState)
                    {
                        case NodeInteractionState.SELECTED:
                            rend.SetColors(startInfo.color * edgeBrightness_selected, endInfo.color * edgeBrightness_selected);
                            break;
                        default:
                            rend.SetColors(startInfo.color * edgeBrightness_none, endInfo.color * edgeBrightness_none);
                            break;
                    }
                    break;
                default:
                    switch (maxState)
                    {
                        case NodeInteractionState.HIGHLIGHTED:
                            rend.SetColors(startInfo.color * edgeBrightness_highlighted, endInfo.color * edgeBrightness_highlighted);
                            break;
                        case NodeInteractionState.SELECTED:
                            rend.SetColors(startInfo.color * 0.75f, endInfo.color * 0.75f);
                            break;
                        default:
                            rend.SetColors(startInfo.color * edgeBrightness_dimmed, endInfo.color * edgeBrightness_dimmed);
                            break;
                    }
                    break;
            }

        }
    }

    public void rotateGraphHorizontal(float angle)
    {
        gameObject.transform.rotation = Quaternion.Euler(new Vector3(0.0f, angle, 0.0f)) * gameObject.transform.rotation;
        //updateInfoBasedOnNodeMovementInWorld();
    }

    public void rotateGraphVertical(float angle)
    {
        gameObject.transform.rotation = Quaternion.Euler(new Vector3(angle, 0.0f, 0.0f)) * gameObject.transform.rotation;
        //updateInfoBasedOnNodeMovementInWorld();
    }






    public void grabSphereWithObject(GameObject obj)
    {
        ForceDirTrackedObject fdto = obj.GetComponent<ForceDirTrackedObject>();

        if (grabObject1 == fdto || grabObject2 == fdto) return;

        if (grabObject1 == null)
        {
            grabObject1 = fdto;
            activeGrabObject = grabObject1;
        }
        else if (grabObject2 == null)
        {
            grabObject2 = fdto;
            activeGrabObject = grabObject2;
        }

        if (grabObject1 != null && grabObject2 != null)
        {

            initialScale = gameObject.transform.localScale;
            Vector3 tVec = grabObject1.transform.position - grabObject2.transform.position;
            initialDist = tVec.magnitude;
            activeScale = true;
            activeMove = false;
        }
        else
        {
            //initialRotation = Quaternion.Inverse(gameObject.transform.rotation) * activeGrabObject.currRotation;
            initialRotation = Quaternion.Inverse(activeGrabObject.transform.rotation) * gameObject.transform.rotation;
            Vector3 tmpVec = gameObject.transform.position - activeGrabObject.transform.position;

            Transform t = activeGrabObject.transform;

            inititalOffset.Set(
                Vector3.Dot(t.up, tmpVec),
                Vector3.Dot(t.right, tmpVec),
                Vector3.Dot(t.forward, tmpVec)
                );
            activeMove = true;
            activeScale = false;
        }

    }

    public void releaseSphereWithObject(GameObject obj)
    {
        ForceDirTrackedObject fdto = obj.GetComponent<ForceDirTrackedObject>();

        if (fdto == null) return;

        if (grabObject1 == fdto)
        {
            grabObject1 = null;
            activeScale = false;

            if (grabObject2 == null) activeMove = false;
        }

        else if (grabObject2 == fdto)
        {
            grabObject2 = null;
            activeScale = false;
            if (grabObject1 == null) activeMove = false;

        }

    }

    void updateMove()
    {
        if (activeMove)
        {
            gameObject.transform.rotation = activeGrabObject.transform.rotation * initialRotation;

            gameObject.transform.position = activeGrabObject.deviceRay.origin +
                inititalOffset.x * activeGrabObject.transform.up +
                inititalOffset.y * activeGrabObject.transform.right +
                inititalOffset.z * activeGrabObject.transform.forward;
        }
    }

    void updateScale()
    {
        if (activeScale)
        {
            Vector3 tVec = grabObject1.transform.position - grabObject2.transform.position;

            float scale = tVec.magnitude / initialDist;

            gameObject.transform.localScale = initialScale * scale;
        }
    }

    void updateInfoBasedOnNodeMovementInWorld()
    {
        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            entry.Value.pos3d = entry.Value.nodeObj.transform.position;
        }
    }

    public bool getShowLines()
    {
        return alwaysShowLines;
    }

    public void toggleShowLines()
    {
        alwaysShowLines = !alwaysShowLines;

        if( alwaysShowLines )
        {
            edgeBrightness_none = 0.5f;
            edgeBrightness_dimmed = 0.35f;
        }
        else
        {
            edgeBrightness_none = 0.0f;
            edgeBrightness_dimmed = 0.0f;
        }

        updateLineEdges();
    }

    public void toggleActiveForce()
    {
        updateForceLayout = !updateForceLayout;
    }
}
