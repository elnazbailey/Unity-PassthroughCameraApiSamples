using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Varjo.XR;
public class TEST : MonoBehaviour
{
    void Start()
    {
        FixCamera();
        ForceSkyOff();
        SetupVarjo();
    }

    void FixCamera()
    {
        Camera cam = Camera.main;
        var hdCam = cam.GetComponent<HDAdditionalCameraData>();

        if (hdCam != null)
        {
            hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hdCam.backgroundColorHDR = new Color(0f, 0f, 0f, 0f);
            Debug.Log("Camera fixed");
        }
    }

    void ForceSkyOff()
    {
        GameObject volObj = new GameObject("MR_Sky_Override");
        Volume vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 999;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vol.profile = profile;

        VisualEnvironment visualEnv = profile.Add<VisualEnvironment>(true);

        // Sky Type values:
        // 0 = None
        // 1 = HDRI Sky
        // 2 = Gradient Sky
        // 3 = Physically Based Sky
        visualEnv.skyType.Override(0); // 0 = None
        visualEnv.skyAmbientMode.Override(0); // Static

        Debug.Log($"Sky forced off - skyType: {visualEnv.skyType.value}");
    }

    void SetupVarjo()
    {
        VarjoRendering.SetOpaque(false);

        if (VarjoMixedReality.IsMRAvailable())
        {
            VarjoMixedReality.StartRender();
            Debug.Log("Varjo MR Started");
        }
        else
        {
            Debug.Log("MR not available (ok if no headset)");
        }
    }
}
