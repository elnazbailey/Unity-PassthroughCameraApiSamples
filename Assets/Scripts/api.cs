// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using Meta.XR;
//using Meta.XR.Samples;
using Unity.Collections;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.XR; 
using Varjo.XR;

public class api: MonoBehaviour
{
    private void DebugVarjoAPI()
{
    Debug.Log("=== VarjoFrameStream Methods ===");
    foreach (var m in typeof(Varjo.XR.VarjoFrameStream).GetMethods())
    {
        Debug.Log($"VarjoFrameStream: {m.Name}");
    }

    Debug.Log("=== VarjoCameraMetadataStream Methods ===");
    foreach (var m in typeof(Varjo.XR.VarjoCameraMetadataStream).GetMethods())
    {
        Debug.Log($"VarjoCameraMetadataStream: {m.Name}");
    }

    Debug.Log("=== VarjoCameraSubsystem Methods ===");
    foreach (var m in typeof(Varjo.XR.VarjoCameraSubsystem).GetMethods())
    {
        Debug.Log($"VarjoCameraSubsystem: {m.Name}");
    }

    Debug.Log("=== VarjoEnvironmentCubemapStream Methods ===");
    foreach (var m in typeof(Varjo.XR.VarjoEnvironmentCubemapStream).GetMethods())
    {
        Debug.Log($"VarjoEnvironmentCubemapStream: {m.Name}");
    }
}
private void Awake()
{
    DebugVarjoAPI();  // ADD THIS LINE
    
}
    
}
