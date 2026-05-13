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
using PassthroughCameraSamples.MultiObjectDetection;
using UnityEngine.XR.ARSubsystems;


public class varjosentisInferenceRunManager : MonoBehaviour
{
    
        //[SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private DetectionUiMenuManager m_uiMenuManager;
        [SerializeField] private varjoDetectionManager m_detectionManager;

        [Header("Sentis Model config")]
        [SerializeField] private BackendType m_backend = BackendType.CPU;
        [SerializeField] private ModelAsset m_sentisModel;
        [SerializeField] private TextAsset m_labelsAsset;
        [SerializeField, Range(0, 1)] private float m_iouThreshold = 0.6f;
        [SerializeField, Range(0, 1)] private float m_scoreThreshold = 0.23f;

        [Header("UI display references")]
        [SerializeField] private varjoSentisInferenceUiManager m_uiInference;

        //[Header("[Editor Only] Convert to Sentis")]
        //public ModelAsset OnnxModel;
        [Space(40)]

        private Worker m_engine;
        private Vector2Int m_inputSize;
        private readonly List<(int classId, Vector4 boundingBox)> m_detections = new List<(int classId, Vector4 boundingBox)>();

        private bool varjo_colorStreaming=false;
        private VarjoCameraSubsystem m_cameraSubsystem;
        private void Awake()
        {
            var model = ModelLoader.Load(m_sentisModel);
            var inputShape = model.inputs[0].shape;
            m_inputSize = new Vector2Int(inputShape.Get(2), inputShape.Get(3));
            m_engine = new Worker(model, m_backend);
        }
        
        private IEnumerator Start()
        {
            m_uiInference.SetLabels(m_labelsAsset);

            // Check if Mixed Reality is available
            if (!VarjoMixedReality.IsMRAvailable())
            {
                Debug.LogError("[VarjoSentis] Mixed Reality not available");
                yield break;
            }

            // Start video see-through / camera stream
            VarjoMixedReality.StartRender();
            
            // Enable VST camera
            if (!VarjoRendering.GetOpaque())
            {
                VarjoRendering.SetOpaque(false); // Enable video pass-through
            }

            var xrLoader = UnityEngine.XR.Management.XRGeneralSettings.Instance?.Manager?.activeLoader;
            if (xrLoader != null)
            {
                m_cameraSubsystem = xrLoader.GetLoadedSubsystem<VarjoCameraSubsystem>();
            }

            if (m_cameraSubsystem == null)
            {
                Debug.LogError("[VarjoSentis] VarjoCameraSubsystem not available");
                yield break;
            }

            m_cameraSubsystem.Start();
            m_cameraSubsystem.EnableColorStream();
            varjo_colorStreaming = true;

            while (true)
            {
                while (m_uiMenuManager.IsPaused)
                {
                    yield return null;
                }
                yield return RunInference();
            }
        }

        private void OnDestroy()
        {
            m_engine.PeekOutput(0)?.CompleteAllPendingOperations();
            m_engine.PeekOutput(1)?.CompleteAllPendingOperations();
            m_engine.PeekOutput(2)?.CompleteAllPendingOperations();
            m_engine.Dispose();
            if (varjo_colorStreaming && m_cameraSubsystem != null)
                {
                    m_cameraSubsystem.DisableColorStream();
                    m_cameraSubsystem.Stop();
                }
                
                if (VarjoMixedReality.IsMRAvailable())
                {
                    VarjoMixedReality.StopRender();
                }
        }
        private bool TryGetHeadPose(out Pose headPose)
        {
            var nodes = new List<XRNodeState>();
            InputTracking.GetNodeStates(nodes);
            foreach (var node in nodes)
            {
                if (node.nodeType == XRNode.Head)
                {
                    if (node.TryGetPosition(out var pos) && node.TryGetRotation(out var rot))
                    {
                        headPose = new Pose(pos, rot);
                        return true;
                    }
                }
            }
            headPose = Pose.identity;
            return false;
        }

        internal static void PreloadModel(ModelAsset modelAsset)
        {
            // Load model
            var model = ModelLoader.Load(modelAsset);
            var inputShape = model.inputs[0].shape;

            // Create engine to run model
            using var worker = new Worker(model, BackendType.CPU);

            // Run inference with an empty image to load the model in the memory. The first inference blocks the main thread for a long time, so we're doing it on the app launch
            Texture tempTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var textureTransform = new TextureTransform().SetDimensions(tempTexture.width, tempTexture.height, 3);
            using var input = new Tensor<float>(new TensorShape(1, 3, inputShape.Get(2), inputShape.Get(3)));
            TextureConverter.ToTensor(tempTexture, input, textureTransform);
            worker.Schedule(input);

            // Complete the inference immediately and destroy the temporary texture
            worker.PeekOutput(0).CompleteAllPendingOperations();
            worker.PeekOutput(1).CompleteAllPendingOperations();
            worker.PeekOutput(2).CompleteAllPendingOperations();
            Destroy(tempTexture);
        }

        private IEnumerator RunInference()
        {
            /*
            if (!m_cameraAccess.IsPlaying)
            {
                yield break;
            }
            
            [DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
            static extern OVRPlugin.Result ovrp_GetNodePoseStateAtTime(double time, OVRPlugin.Node nodeId, out OVRPlugin.PoseStatef nodePoseState);
            if (!ovrp_GetNodePoseStateAtTime(OVRPlugin.GetTimeInSeconds(), OVRPlugin.Node.Head, out _).IsSuccess())
            {
                Debug.Log("ovrp_GetNodePoseStateAtTime failed, which means 'm_cameraAccess.GetCameraPose()' is not reliable, skipping.");
                yield break;
            }
            */
            if (!varjo_colorStreaming || !VarjoMixedReality.IsMRReady())
                    yield break;
                    
                if (!TryGetHeadPose(out _))
                {
                    Debug.Log("[VarjoSentis] Head pose unavailable, skipping frame.");
                    yield break;
                }

                // Get camera frame using TryAcquireLatestCpuImage
                if (!m_cameraSubsystem.TryAcquireLatestCpuImage(out var cpuImage))
                {
                    yield break;
                }

            //var frame = VarjoMixedReality.colorStream.GetFrame();            
            //var cachedCameraPose = m_cameraAccess.GetCameraPose();
            //var cachedCameraPose = new Pose(frame.pose.position, frame.pose.rotation);


            // Update Capture data
            //Texture targetTexture = m_cameraAccess.GetTexture();
            //Texture targetTexture = frame.texture;
            // Get camera frame from Varjo
            //Texture targetTexture = VarjoMixedReality.GetCameraTexture();
            //if (targetTexture == null)
            //    yield break;
            Texture2D targetTexture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.RGBA32, false);
            cpuImage.Convert(new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.None
            }, targetTexture.GetRawTextureData<byte>());
            targetTexture.Apply();
            
            // Dispose the CPU image when done
            cpuImage.Dispose();
            TryGetHeadPose(out var cachedCameraPose);

            // Convert the texture to a Tensor and schedule the inference
            var textureTransform = new TextureTransform().SetDimensions(targetTexture.width, targetTexture.height, 3);
            using var input = new Tensor<float>(new TensorShape(1, 3, m_inputSize.x, m_inputSize.y));
            TextureConverter.ToTensor(targetTexture, input, textureTransform);

            // Schedule all model layers
            m_engine.Schedule(input);

            // Get the results. ReadbackAndCloneAsync waits for all layers to complete before returning the result
            var boxesAwaiter = (m_engine.PeekOutput(0) as Tensor<float>).ReadbackAndCloneAsync().GetAwaiter();
            while (!boxesAwaiter.IsCompleted)
            {
                yield return null;
            }
            using var boxes = boxesAwaiter.GetResult();
            if (boxes.shape[0] == 0)
            {
                yield break;
            }

            var classIDsAwaiter = (m_engine.PeekOutput(1) as Tensor<int>).ReadbackAndCloneAsync().GetAwaiter();
            while (!classIDsAwaiter.IsCompleted)
            {
                yield return null;
            }
            using var classIDs = classIDsAwaiter.GetResult();
            if (classIDs.shape[0] == 0)
            {
                Debug.LogError("classIDs.shape[0] == 0");
                yield break;
            }

            var scoresAwaiter = (m_engine.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync().GetAwaiter();
            while (!scoresAwaiter.IsCompleted)
            {
                yield return null;
            }
            using var scores = scoresAwaiter.GetResult();
            if (scores.shape[0] == 0)
            {
                Debug.LogError("scores.shape[0] == 0");
                yield break;
            }

            NonMaxSuppression(m_detections, boxes, classIDs, scores, m_iouThreshold, m_scoreThreshold);
            /*
            // Checking if spatial anchor is tracked ensures bounding boxes are placed at correct world space positIons.
            if (!m_cameraAccess.IsPlaying || m_detectionManager.m_spatialAnchor == null || !m_detectionManager.m_spatialAnchor.IsTracked)
            {
                yield break;
            }
            */
            if (!varjo_colorStreaming || !TryGetHeadPose(out _)) 
            {
                yield break;
            }
            // Update UI.
            m_uiInference.DrawUIBoxes(m_detections, m_inputSize, cachedCameraPose);

        }

        private static void NonMaxSuppression(List<(int classId, Vector4 boundingBox)> outDetections, Tensor<float> boxes, Tensor<int> classIDs, Tensor<float> scores, float iouThreshold, float scoreThreshold)
        {
            outDetections.Clear();

            // Filter by score threshold first
            List<int> filteredIndices = new List<int>();
            NativeArray<float>.ReadOnly scoresArray = scores.AsReadOnlyNativeArray();
            for (int i = 0; i < scoresArray.Length; i++)
            {
                if (scoresArray[i] >= scoreThreshold)
                {
                    filteredIndices.Add(i);
                }
            }

            if (filteredIndices.Count == 0)
            {
                return;
            }

            // Sort filtered indices by scores in descending order
            filteredIndices.Sort((a, b) => scoresArray[b].CompareTo(scoresArray[a]));

            // Apply NMS algorithm
            bool[] suppressed = new bool[filteredIndices.Count];
            for (int i = 0; i < filteredIndices.Count; i++)
            {
                if (suppressed[i])
                    continue;

                int idx = filteredIndices[i];

                // Add this detection to results
                outDetections.Add((classIDs[idx], GetBox(idx)));

                // Suppress overlapping boxes regardless of class
                for (int j = i + 1; j < filteredIndices.Count; j++)
                {
                    if (suppressed[j])
                        continue;

                    int jdx = filteredIndices[j];

                    float iou = CalculateIoU(GetBox(idx), GetBox(jdx));
                    if (iou > iouThreshold)
                    {
                        suppressed[j] = true;
                    }
                }
            }

            Vector4 GetBox(int i) => new Vector4(boxes[i, 0], boxes[i, 1], boxes[i, 2], boxes[i, 3]);
        }

        internal static float CalculateIoU(Vector4 boxA, Vector4 boxB)
        {
            // Boxes are in format (topLeftX, topLeftY, bottomRightX, bottomRightY)
            // Calculate intersection coordinates
            float x1 = Mathf.Max(boxA.x, boxB.x);
            float y1 = Mathf.Max(boxA.y, boxB.y);
            float x2 = Mathf.Min(boxA.z, boxB.z);
            float y2 = Mathf.Min(boxA.w, boxB.w);

            // Calculate intersection area
            float intersectionWidth = Mathf.Max(0, x2 - x1);
            float intersectionHeight = Mathf.Max(0, y2 - y1);
            float intersectionArea = intersectionWidth * intersectionHeight;

            // Calculate individual box areas
            float boxAArea = (boxA.z - boxA.x) * (boxA.w - boxA.y);
            float boxBArea = (boxB.z - boxB.x) * (boxB.w - boxB.y);

            // Calculate union area
            float unionArea = boxAArea + boxBArea - intersectionArea;

            // Return IoU (Intersection over Union)
            if (unionArea == 0)
                return 0;

            return intersectionArea / unionArea;
        }
    }

