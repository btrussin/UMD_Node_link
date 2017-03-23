using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class ForceDirTrackedObject : SteamVR_TrackedObject
{
    public GameObject otherController;
    ForceDirTrackedObject otherTrackedObjScript;

    public GameObject menuObject;
    public bool menuActive = false;

    public GameObject sliderLeftPnt;
    public GameObject sliderRightPnt;
    public GameObject sliderPoint;
    float sliderPointDistance = 0.0f;
    bool updateSlider = false;
    bool triggerPulled = false;


    public Ray deviceRay;

    int nodeLayerMask;
    int menuSliderMask;

    GameObject currNodeSelected = null;
    bool updateNodePosition = false;
    float nodePointDistance = 0.0f;

    public GameObject beam;
    bool castBeamAnyway = false;

    CVRSystem vrSystem;
    VRControllerState_t state;
    VRControllerState_t prevState;

    public GameObject forceDirLayoutObj;
    ForceDirLayout fDirScript;

    // Use this for initialization
    void Start () {
        vrSystem = OpenVR.System;

        menuSliderMask = 1 << LayerMask.NameToLayer("MenuSlider");
        nodeLayerMask = 1 << LayerMask.NameToLayer("NodeLayer");
        beam.SetActive(false);

        otherTrackedObjScript = otherController.GetComponent<ForceDirTrackedObject>();

        fDirScript = forceDirLayoutObj.GetComponent<ForceDirLayout>();
    }

    // Update is called once per frame
    void Update () {

        // update the device ray per frame
        deviceRay.origin = transform.position;
        Quaternion rayRotation = Quaternion.AngleAxis(60.0f, transform.right);
        deviceRay.direction = rayRotation * transform.forward;


        handleStateChanges();
        projectBeam();



        if (updateSlider)
        {
            calcSliderPosition();
        }
        else if( updateNodePosition )
        {
            calcNodePosition();
        }

    }

    void projectBeam()
    {

        float beamDist = 10.0f;

        beam.SetActive(false);

        RaycastHit hitInfo;
        if (updateSlider)
        {
            beam.SetActive(true);
            beamDist = sliderPointDistance;
        }
        else if (updateNodePosition)
        {
            beam.SetActive(true);
            beamDist = nodePointDistance;
        }
        else if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, beamDist, menuSliderMask))
        {
            GameObject obj = hitInfo.collider.gameObject;
            beamDist = hitInfo.distance;
            beam.SetActive(true);

            if (triggerPulled && obj.name.Equals("Quad_Slider_Point"))
            {
                sliderPointDistance = beamDist;
                updateSlider = true;
            }
            else updateSlider = false;

        }
        else if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, beamDist, nodeLayerMask))
        {
            
            currNodeSelected = hitInfo.collider.gameObject;
            beamDist = hitInfo.distance;
            beam.SetActive(true);
            if (triggerPulled)
            {
                nodePointDistance = beamDist;
                updateNodePosition = true;
            }
        }
        else if(castBeamAnyway)
        {
            beam.SetActive(true);
        }

        LineRenderer lineRend = beam.GetComponent<LineRenderer>();
        Vector3 end = deviceRay.GetPoint(beamDist);

        lineRend.SetPosition(0, deviceRay.origin);
        lineRend.SetPosition(1, end);
    }

    void handleStateChanges()
    {
        bool stateIsValid = vrSystem.GetControllerState((uint)index, ref state);

        if (!stateIsValid) Debug.Log("Invalid State for Idx: " + index);

        if (stateIsValid && state.GetHashCode() != prevState.GetHashCode())
        {
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.ApplicationMenu) != 0 &&
                (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.ApplicationMenu) == 0)
            {
                toggleMenu();
            }


            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0 &&
                (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) == 0)
            {
                // just pulled the trigger
                castBeamAnyway = true;
                triggerPulled = true;
            }
            else if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) == 0 &&
                (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0)
            {
                // just released the trigger
                castBeamAnyway = false;
                updateSlider = false;
                updateNodePosition = false;
                triggerPulled = false;

                if(currNodeSelected != null)
                {
                    NodeInfo info = fDirScript.getNodeInfo(currNodeSelected.name);
                    if (info != null)
                    {
                        info.positionIsStationary = false;
                    }

                    currNodeSelected = null;
                }
            }



            prevState = state;
        }

       

       
    }

    void calcSliderPosition()
    {
        // get proposted position of the slider point in world space
        Vector3 tVec = deviceRay.GetPoint(sliderPointDistance);

        // project that point onto the world positions of the slider ends
        Vector3 v1 = sliderRightPnt.transform.position - sliderLeftPnt.transform.position;
        Vector3 v2 = tVec - sliderLeftPnt.transform.position;

        // 'd' is the vector-projection amount of v2 onto v1
        float d = Vector3.Dot(v1, v2) / Vector3.Dot(v1, v1);

        // 'd' is also the correct linear combination of the left and right slider edges
        // left * d + right * ( 1 - d )
        setSliderLocalPosition(d);
    }

    void setSliderLocalPosition(float dist)
    {
        // clamp dist to 0.0 and 1.0
        // float tDist = Mathf.Min(1.0f, Mathf.Max(0.0f, dist));
        float tDist = Mathf.Clamp(dist, 0.0f, 1.0f);
        Vector3 tVec = (sliderRightPnt.transform.localPosition - sliderLeftPnt.transform.localPosition) * tDist;
        sliderPoint.transform.localPosition = sliderLeftPnt.transform.localPosition + tVec;

        //fDirScript.gravityAmt = 0.04f * tDist;  // linear
        fDirScript.gravityAmt = 0.04f * tDist * tDist;  // quad
    }


    void calcNodePosition()
    {
        Vector3 pos = deviceRay.GetPoint(nodePointDistance);
        currNodeSelected.transform.position = pos;
        NodeInfo info = fDirScript.getNodeInfo(currNodeSelected.name);
        if (info != null) {
            info.pos3d = pos;
            info.positionIsStationary = true;
        }
    }


    public void toggleMenu()
    {
        if (menuActive) hideMainMenu();
        else showMainMenu();
    }

    public void showMainMenu()
    {
        menuActive = true;
        otherTrackedObjScript.menuActive = false;


        menuObject.transform.SetParent(gameObject.transform);

        menuObject.transform.localPosition = new Vector3(0.00f, 0.01f, 0.05f);
        menuObject.transform.localRotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        menuObject.transform.localScale = new Vector3(0.25f, 0.25f, 1.0f);

        menuObject.SetActive(true);
    }

    public void hideMainMenu()
    {
        menuActive = false;
        menuObject.SetActive(false);
    }
}
