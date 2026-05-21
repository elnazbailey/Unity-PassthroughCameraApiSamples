using UnityEngine;
using Varjo.XR;

public class FixMRDepth: MonoBehaviour
{
    void Start()
    {
        // Disable depth testing - this removes the distance cutoff
        VarjoMixedReality.DisableDepthEstimation();
        
        // If you need depth for occlusion but want further range:
        // VarjoMixedReality.SetDepthTestEnabled(true);
        // VarjoMixedReality.SetDepthTestRange(0.1f, 100f); // near, far in meters
        
        Debug.Log("Depth test disabled - full passthrough should now be visible");
    }

void Update()
{
    // Toggle with D key to test
    if (Input.GetKeyDown(KeyCode.D))
    {
        VarjoMixedReality.DisableDepthEstimation();
       // VarjoMixedReality.EnableDepthEstimation(!currentState);
        //Debug.Log($"Depth test: {!currentState}");
    }
}
}
