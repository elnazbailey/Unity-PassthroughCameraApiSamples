using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class ForceSkyOff
{
    void Start()
    {
        // Create a new volume to override everything
        GameObject volObj = new GameObject("MR_Override_Volume");
        Volume vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 999; // High priority to override others

        // Create profile
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vol.profile = profile;

        // Add Visual Environment and set sky to none
        VisualEnvironment visualEnv = profile.Add<VisualEnvironment>(true);

        Debug.Log("Sky override volume created - Sky Type set to None");
    }
}
