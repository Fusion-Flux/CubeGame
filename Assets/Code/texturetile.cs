using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways]
public class texturetile : MonoBehaviour
{
    public Vector2 tilingMultiplier = new Vector2(1f, 1f);
    
    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateTiling();
    }

    private void Update()
    {
        // If the object can be resized at runtime, update tiling every frame:
        UpdateTiling();
    }

    void UpdateTiling()
    {
        Vector3 scale = transform.localScale;

        // For a cube-like shape, we might assume X corresponds to tiling X, and Z corresponds to tiling Y
        // Adjust as needed for your specific shape/UV orientation
        float tileX = scale.x * tilingMultiplier.x;
        float tileY = scale.z * tilingMultiplier.y;
        // Example: Setting the tiling on a material
        Material myMat = GetComponent<Renderer>().material;
        myMat.SetTextureScale("_BaseColorMap", new Vector2(tileX, tileY));
        myMat.SetTextureOffset("_BaseColorMap", new Vector2(tileX, tileY));

        //rend.sharedMaterial.mainTextureScale = new Vector2(tileX, tileY);
    }
}
