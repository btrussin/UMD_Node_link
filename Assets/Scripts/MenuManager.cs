using UnityEngine;
using System.Collections;

public class MenuManager : MonoBehaviour {

    public GameObject optionsSubMenu;
    public GameObject gravitySubMenu;

    public GameObject forceBox;
    public GameObject linesBox;
    public GameObject nodePointerBox;

    public GameObject sliderLeftPnt;
    public GameObject sliderRightPnt;
    public GameObject sliderPoint;

    public Material emptyBoxMaterial;
    public Material checkBoxMaterial;

    public GameObject forceDirLayoutObj;
    ForceDirLayout fDirScript;

    public bool useNodePointers = false;

    // Use this for initialization
    void Start () {
        fDirScript = forceDirLayoutObj.GetComponent<ForceDirLayout>();
    }
	
	// Update is called once per frame
	void Update () {
        

    }

    public void updateInterface()
    {
        MeshRenderer forceBoxRend = forceBox.GetComponent<MeshRenderer>();
        MeshRenderer lineBoxRend = linesBox.GetComponent<MeshRenderer>();
        MeshRenderer ndPntBoxRend = nodePointerBox.GetComponent<MeshRenderer>();

        if ( fDirScript.updateForceLayout ) forceBoxRend.material = checkBoxMaterial;
        else forceBoxRend.material = emptyBoxMaterial;

        if (fDirScript.getShowLines()) lineBoxRend.material = checkBoxMaterial;
        else lineBoxRend.material = emptyBoxMaterial;

        if (useNodePointers) ndPntBoxRend.material = checkBoxMaterial;
        else ndPntBoxRend.material = emptyBoxMaterial;

    }

    public void calcSliderPosition(Vector3 pos)
    {
        // project that point onto the world positions of the slider ends
        Vector3 v1 = sliderRightPnt.transform.position - sliderLeftPnt.transform.position;
        Vector3 v2 = pos - sliderLeftPnt.transform.position;

        // 'd' is the vector-projection amount of v2 onto v1
        float d = Vector3.Dot(v1, v2) / Vector3.Dot(v1, v1);

        // 'd' is also the correct linear combination of the left and right slider edges
        // left * d + right * ( 1 - d )
        setSliderLocalPosition(d);
    }

    public void setSliderLocalPosition(float dist)
    {
        // clamp dist to 0.0 and 1.0
        // float tDist = Mathf.Min(1.0f, Mathf.Max(0.0f, dist));
        float tDist = Mathf.Clamp(dist, 0.0f, 1.0f);
        Vector3 tVec = (sliderRightPnt.transform.localPosition - sliderLeftPnt.transform.localPosition) * tDist;
        sliderPoint.transform.localPosition = sliderLeftPnt.transform.localPosition + tVec;

        //fDirScript.gravityAmt = 0.04f * tDist;  // linear
        fDirScript.gravityAmt = 0.04f * tDist * tDist;  // quad
    }

    public void toggleShowLines()
    {
        fDirScript.toggleShowLines();
        updateInterface();
    }

    public void toggleForce()
    {
        fDirScript.toggleActiveForce();
        updateInterface();
    }

    public void toggleNodePointers()
    {
        useNodePointers = !useNodePointers;
        updateInterface();
    }

}
