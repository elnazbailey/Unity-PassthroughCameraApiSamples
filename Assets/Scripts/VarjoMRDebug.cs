using System.Collections;
using UnityEngine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Varjo.XR;
public class VarjoMRDebug: MonoBehaviour
{
    void Start()
    {
        StartCoroutine(InitializeAndCheck());
    }

    IEnumerator InitializeAndCheck()
    {
        yield return new WaitForSeconds(1f);

        FixAllCameras();  // FIX CAMERAS FIRST
        ForceStartMR();

        yield return new WaitForSeconds(0.5f);

        CheckAllSettings();
    }

    [ContextMenu("Fix All Cameras")]
    public void FixAllCameras()
    {
        Debug.Log("===== FIXING ALL CAMERAS =====");

        Camera[] allCameras = GameObject.FindObjectsOfType<Camera>();

        foreach (Camera cam in allCameras)
        {
            // Fix base camera background - SET ALPHA TO 0
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // Fix HDRP camera data
            var hdCam = cam.GetComponent<HDAdditionalCameraData>();
            if (hdCam != null)
            {
                hdCam.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                hdCam.backgroundColorHDR = new Color(0f, 0f, 0f, 0f);
            }

            Debug.Log($"Fixed camera: {cam.name}");
            Debug.Log($"  New Background: {cam.backgroundColor}");
            Debug.Log($"  New Alpha: {cam.backgroundColor.a}");
        }
    }

    [ContextMenu("Force Start MR")]
    public void ForceStartMR()
    {
        Debug.Log("===== FORCING MR START =====");

        VarjoRendering.SetOpaque(false);
        Debug.Log($"Set Opaque to false. Current value: {VarjoRendering.GetOpaque()}");

        if (VarjoMixedReality.IsMRAvailable())
        {
            VarjoMixedReality.StartRender();
            Debug.Log("MR StartRender() called");
        }
        else
        {
            Debug.LogWarning("MR not available - trying to start anyway...");
            try
            {
                VarjoMixedReality.StartRender();
                Debug.Log("MR StartRender() called (forced)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start MR: {e.Message}");
            }
        }

        try
        {
            VarjoMixedReality.EnableDepthEstimation();
            Debug.Log("Depth estimation enabled");
        }
        catch (System.Exception e)
        {
            Debug.Log($"Depth estimation not available: {e.Message}");
        }

        Debug.Log($"MR Ready after force start: {VarjoMixedReality.IsMRReady()}");
    }

    [ContextMenu("Check All Settings")]
    public void CheckAllSettings()
    {
        Debug.Log("===== VARJO MR DEBUG CHECK =====");
        CheckVarjoSettings();
        CheckCameraSettings();
        CheckVolumeSettings();
        Debug.Log("===== END DEBUG CHECK =====");
    }

    void CheckVarjoSettings()
    {
        Debug.Log("--- VARJO SETTINGS ---");
        Debug.Log($"MR Available: {VarjoMixedReality.IsMRAvailable()}");
        Debug.Log($"MR Ready: {VarjoMixedReality.IsMRReady()}");
        Debug.Log($"Opaque Setting: {VarjoRendering.GetOpaque()} (should be FALSE)");
    }

    void CheckCameraSettings()
    {
        Debug.Log("--- CAMERA SETTINGS ---");

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("No Main Camera found!");
            return;
        }

        Debug.Log($"Camera found: {mainCam.name}");
        Debug.Log($"Clear Flags: {mainCam.clearFlags}");
        Debug.Log($"Background Color: {mainCam.backgroundColor}");
        Debug.Log($"Background Alpha: {mainCam.backgroundColor.a} (should be 0)");

        var hdCamera = mainCam.GetComponent<HDAdditionalCameraData>();
        if (hdCamera != null)
        {
            Debug.Log($"HDRP Camera Data found");
            Debug.Log($"Background Color HDR: {hdCamera.backgroundColorHDR}");
            Debug.Log($"Clear Mode: {hdCamera.clearColorMode}");
        }
    }

    void CheckVolumeSettings()
    {
        Debug.Log("--- VOLUME SETTINGS ---");

        Volume[] volumes = GameObject.FindObjectsOfType<Volume>();
        Debug.Log($"Found {volumes.Length} Volume(s) in scene");

        foreach (Volume vol in volumes)
        {
            Debug.Log($"Volume: {vol.gameObject.name} | Global: {vol.isGlobal} | Priority: {vol.priority}");

            if (vol.profile != null && vol.profile.TryGet<VisualEnvironment>(out var visualEnv))
            {
                Debug.Log($"  Visual Environment found:");
                Debug.Log($"    Sky Type: {visualEnv.skyType.value}");
                Debug.Log($"    Sky Ambient Mode: {visualEnv.skyAmbientMode.value}");
            }
        }
    }
}


