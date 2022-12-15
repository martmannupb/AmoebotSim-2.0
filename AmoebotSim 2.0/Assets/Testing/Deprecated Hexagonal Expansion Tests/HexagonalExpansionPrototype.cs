using System.Collections;
using System.Collections.Generic;
using AS2.Visuals;
using UnityEngine;

public class HexagonalExpansionPrototype : MonoBehaviour
{
    public float hexScale = 1f;
    public float hexBorderWidth = 0.1f;

    private Mesh mesh;
    public Material hexFullMat;
    public Material hexExpMat;

    private MaterialPropertyBlock propertyBlockExpanding;

    // Start is called before the first frame update
    void Start()
    {
        CreateMesh();

        // Repeating Animation
        propertyBlockExpanding = new MaterialPropertyBlock();
        Time.fixedDeltaTime = hexExpMat.GetFloat("_AnimDuration");
    }

    // Update is called once per frame
    void Update()
    {
        //Graphics.DrawMesh(mesh, Matrix4x4.TRS(new Vector3(-1f, 0f, 0f), Quaternion.identity, new Vector3(1f, 1f, 1f)), hexFullMat, 0);
        Graphics.DrawMesh(mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 1f)), hexExpMat, 0, null, 0, propertyBlockExpanding);
    }

    void FixedUpdate()
    {
        // Reset Animation
        if(propertyBlockExpanding != null) propertyBlockExpanding.SetFloat("_AnimTriggerTime", Time.timeSinceLevelLoad);
    }

    private void CreateMesh()
    {
        mesh = MeshCreator_HexagonalView.GetMesh_BaseExpansionHexagon();
    }

    

    // Time.timeSinceLevelLoad
}
