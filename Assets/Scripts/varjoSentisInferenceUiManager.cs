using UnityEngine;
// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
//using Meta.XR;
//using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Varjo.XR; 

public class varjoSentisInferenceUiManager: MonoBehaviour
{
    
        [Header("Placement configuration")]
        //[SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        //[SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private Camera m_xrCamera;
        [SerializeField] private LayerMask m_raycastLayers = Physics.DefaultRaycastLayers;
        [SerializeField] private float m_fallbackDepth = 2.0f;

        [SerializeField] private RectTransform m_detectionBoxPrefab;
        [Space(10)]
        public UnityEvent<int> OnObjectsDetected;

        internal readonly List<BoundingBoxData> m_boxDrawn = new();
        private string[] m_labels;
        private readonly List<BoundingBoxData> m_boxPool = new();

        private Vector2Int m_currentResolution = new Vector2Int(1920, 1080);
        public void UpdateResolution(int width, int height)
       {     m_currentResolution = new Vector2Int(width, height);
       }
            
        internal class BoundingBoxData
        {
            public string ClassName;
            public int ClassId;
            public RectTransform BoxRectTransform;
            public float lastUpdateTime;
        }

        private void Awake() => m_detectionBoxPrefab.gameObject.SetActive(false);

        private void Update()
        {
            // Remove boxes that haven't been updated recently
            for (int i = m_boxDrawn.Count - 1; i >= 0; i--)
            {
                var box = m_boxDrawn[i];
                const float timeToPersistBoxes = 3f;
                if (Time.time - box.lastUpdateTime > timeToPersistBoxes)
                {
                    ReturnToPool(box);
                    m_boxDrawn.RemoveAt(i);
                }
            }
        }

        public void SetLabels(TextAsset labelsAsset)
        {
            // Parse neural net labels
            m_labels = labelsAsset.text.Split('\n');
        }

        public void DrawUIBoxes(List<(int classId, Vector4 boundingBox)> detections, Vector2 inputSize, Pose cameraPose)
        {
                Vector2 currentResolution = m_currentResolution;

            if (detections.Count == 0)
            {
                OnObjectsDetected?.Invoke(0);
                return;
            }

            OnObjectsDetected?.Invoke(detections.Count);

            for (var i = 0; i < detections.Count; i++)
            {
                var detection = detections[i];
                float x1 = detection.boundingBox[0];
                float y1 = detection.boundingBox[1];
                float x2 = detection.boundingBox[2];
                float y2 = detection.boundingBox[3];
                Rect rect = new Rect(x1, y1, x2 - x1, y2 - y1);

                Vector2 normalizedCenter = rect.center / inputSize;
                Vector2 center = currentResolution * (normalizedCenter - Vector2.one * 0.5f);

                var classname = m_labels[detection.classId].Replace(" ", "_");

                // Create ray FROM cameraPose position WITH cameraPose rotation
                var rayDirection = cameraPose.rotation * 
                    m_xrCamera.ViewportPointToRay(
                        new Vector3(normalizedCenter.x, 1.0f - normalizedCenter.y, 0f)).direction;
                
                var ray = new Ray(cameraPose.position, rayDirection);

                Vector3? worldPos = Physics.Raycast(ray, out RaycastHit hit, 20f, m_raycastLayers)
                    ? hit.point
                    : (Vector3?)null;

                if (!worldPos.HasValue)
                {
                    worldPos = ray.origin + ray.direction * m_fallbackDepth;
                }

                var normRect = new Rect(
                    rect.x / inputSize.x,
                    1f - rect.yMax / inputSize.y,
                    rect.width / inputSize.x,
                    rect.height / inputSize.y
                );

                // Use consistent camera pose origin
                float distance = Vector3.Distance(cameraPose.position, worldPos.Value);
                
                var centerRayDirection = cameraPose.rotation * 
                    m_xrCamera.ViewportPointToRay(
                        new Vector3(normRect.center.x, normRect.center.y, 0f)).direction;
                
                var worldSpaceCenter = cameraPose.position + centerRayDirection * distance;

                // Plane calculations
                var normal = (worldSpaceCenter - cameraPose.position).normalized;
                var plane = new Plane(normal, worldSpaceCenter);
                
                var minRayDirection = cameraPose.rotation * 
                    m_xrCamera.ViewportPointToRay(
                        new Vector3(normRect.min.x, normRect.min.y, 0f)).direction;
                var minRay = new Ray(cameraPose.position, minRayDirection);
                
                var maxRayDirection = cameraPose.rotation * 
                    m_xrCamera.ViewportPointToRay(
                        new Vector3(normRect.max.x, normRect.max.y, 0f)).direction;
                var maxRay = new Ray(cameraPose.position, maxRayDirection);

                plane.Raycast(minRay, out float intersectionDistanceMin);
                plane.Raycast(maxRay, out float intersectionDistanceMax);
                var min = minRay.GetPoint(intersectionDistanceMin);
                var max = maxRay.GetPoint(intersectionDistanceMax);

                // Transform to camera local space
                var topLeftLocal = Quaternion.Inverse(cameraPose.rotation) * (min - cameraPose.position);
                var bottomRightLocal = Quaternion.Inverse(cameraPose.rotation) * (max - cameraPose.position);
                var size = new Vector2(
                    Mathf.Abs(bottomRightLocal.x - topLeftLocal.x),
                    Mathf.Abs(bottomRightLocal.y - topLeftLocal.y));

                var boxData = GetOrCreateBoundingBoxData(detection.classId, worldSpaceCenter, size);
                var boxRectTransform = boxData.BoxRectTransform;
                boxRectTransform.GetComponentInChildren<Text>().text = 
                    $"Id: {detection.classId} Class: {classname} Center (px): {center:0.0} Center (%): {normalizedCenter:0.0}";
                boxRectTransform.SetPositionAndRotation(worldSpaceCenter, Quaternion.LookRotation(normal));
                boxRectTransform.sizeDelta = size;
                boxData.lastUpdateTime = Time.time;
            }
        }

        private BoundingBoxData GetOrCreateBoundingBoxData(int classId, Vector3 worldSpaceCenter, Vector2 worldSpaceSize)
        {
            BoundingBoxData reusedBox = null;
            for (int i = m_boxDrawn.Count - 1; i >= 0; i--)
            {
                var box = m_boxDrawn[i];
                var localPos = box.BoxRectTransform.InverseTransformPoint(worldSpaceCenter);
                var newBox = new Vector4(
                    localPos.x - worldSpaceSize.x * 0.5f,
                    localPos.y - worldSpaceSize.y * 0.5f,
                    localPos.x + worldSpaceSize.x * 0.5f,
                    localPos.y + worldSpaceSize.y * 0.5f
                );

                var sizeDelta = box.BoxRectTransform.sizeDelta;
                var currentBox = new Vector4(
                    -sizeDelta.x * 0.5f,
                    -sizeDelta.y * 0.5f,
                    sizeDelta.x * 0.5f,
                    sizeDelta.y * 0.5f);

                if (box.ClassId == classId)
                {
                    // If the new box overlaps with an existing one of the same class, reuse it
                    if (varjosentisInferenceRunManager.CalculateIoU(newBox, currentBox) > 0f)
                    {
                        if (reusedBox == null)
                        {
                            reusedBox = box;
                        }
                        else
                        {
                            // Same overlapping class - remove the existing box
                            ReturnToPool(box);
                            m_boxDrawn.RemoveAt(i);
                        }
                    }
                }
                // If the new box's IoU with another class is significant, remove the existing box
                else if (varjosentisInferenceRunManager.CalculateIoU(newBox, currentBox) > 0.1f)
                {
                    // Different overlapping class - remove the existing box
                    ReturnToPool(box);
                    m_boxDrawn.RemoveAt(i);
                }
            }

            if (reusedBox != null)
            {
                return reusedBox;
            }

            // Create a new box
            var newData = GetBoxFromPoolOrCreate();
            newData.ClassId = classId;
            newData.ClassName = m_labels[classId].Replace(" ", "_");
            m_boxDrawn.Add(newData);
            return newData;
        }

        private BoundingBoxData GetBoxFromPoolOrCreate()
        {
            if (m_boxPool.Count > 0)
            {
                var pooled = m_boxPool[m_boxPool.Count - 1];
                pooled.BoxRectTransform.gameObject.SetActive(true);
                m_boxPool.RemoveAt(m_boxPool.Count - 1);
                return pooled;
            }

            var boxRectTransform = Instantiate(m_detectionBoxPrefab, ContentParent);
            boxRectTransform.gameObject.SetActive(true);
            return new BoundingBoxData
            {
                BoxRectTransform = boxRectTransform
            };
        }

        internal Transform ContentParent => m_detectionBoxPrefab.parent;

        private void ReturnToPool(BoundingBoxData box)
        {
            box.BoxRectTransform.gameObject.SetActive(false);
            m_boxPool.Add(box);
        }

        internal void ClearAnnotations()
        {
            foreach (var box in m_boxDrawn)
            {
                ReturnToPool(box);
            }
            m_boxDrawn.Clear();
        }
    }

