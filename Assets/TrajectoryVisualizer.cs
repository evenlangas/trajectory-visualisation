using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrajectoryDataLoader dataLoader;
    
    [Header("History Visualization")]
    [SerializeField] private int maxHistoryPoints = 30;
    [SerializeField] private LineRenderer historyLineRenderer;
    [SerializeField] private Color historyLineColor = new Color(0.7f, 0.9f, 1f, 0.8f);
    [SerializeField] private float historyLineWidth = 0.1f;
    
    [Header("Prediction Visualization")]
    [SerializeField] private LineRenderer predictionLineRenderer;
    [SerializeField] private Color predictionLineColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private float predictionLineWidth = 0.1f;
    
    [Header("Current Position Visualization")]
    [SerializeField] private GameObject currentPositionMarker;
    [SerializeField] private Color currentPositionColor = Color.green;
    
    // Private variables
    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private TrajectoryFrame currentFrame;

    void Awake()
    {
        // Create line renderers if not set
        if (historyLineRenderer == null)
        {
            GameObject historyObj = new GameObject("HistoryLine");
            historyObj.transform.SetParent(transform);
            historyLineRenderer = historyObj.AddComponent<LineRenderer>();
            SetupLineRenderer(historyLineRenderer, historyLineColor, historyLineWidth);
        }
        
        if (predictionLineRenderer == null)
        {
            GameObject predictionObj = new GameObject("PredictionLine");
            predictionObj.transform.SetParent(transform);
            predictionLineRenderer = predictionObj.AddComponent<LineRenderer>();
            SetupLineRenderer(predictionLineRenderer, predictionLineColor, predictionLineWidth, true);
        }
        
        if (currentPositionMarker == null && dataLoader != null && dataLoader.GetComponent<Transform>() != null)
        {
            // Use the same object as the data loader is attached to
            currentPositionMarker = dataLoader.gameObject;
        }
    }

    void OnEnable()
    {
        if (dataLoader != null)
        {
            // Subscribe to events
            dataLoader.OnFrameChanged.AddListener(OnFrameChanged);
            dataLoader.OnTrajectoryChanged.AddListener(OnTrajectoryChanged);
        }
        else
        {
            Debug.LogError("TrajectoryVisualizer: No TrajectoryDataLoader component assigned!");
            // Try to find it in the scene
            dataLoader = FindObjectOfType<TrajectoryDataLoader>();
            if (dataLoader != null)
            {
                dataLoader.OnFrameChanged.AddListener(OnFrameChanged);
                dataLoader.OnTrajectoryChanged.AddListener(OnTrajectoryChanged);
                Debug.Log("TrajectoryVisualizer: Found TrajectoryDataLoader in scene.");
            }
        }
    }

    void OnDisable()
    {
        if (dataLoader != null)
        {
            // Unsubscribe from events
            dataLoader.OnFrameChanged.RemoveListener(OnFrameChanged);
            dataLoader.OnTrajectoryChanged.RemoveListener(OnTrajectoryChanged);
        }
    }

    private void SetupLineRenderer(LineRenderer lineRenderer, Color color, float width, bool dashed = false)
    {
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        
        if (dashed)
        {
            // Set texture to create dashed line effect
            lineRenderer.material.SetTextureScale("_MainTex", new Vector2(1f, 3f));
            lineRenderer.material.SetTextureOffset("_MainTex", new Vector2(0f, 0f));
            // Create a dash texture
            Texture2D dashTexture = new Texture2D(2, 2);
            dashTexture.wrapMode = TextureWrapMode.Repeat;
            Color[] colors = new Color[] { color, color, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0) };
            dashTexture.SetPixels(colors);
            dashTexture.Apply();
            lineRenderer.material.mainTexture = dashTexture;
        }
    }

    void OnFrameChanged(TrajectoryFrame frame)
    {
        currentFrame = frame;
        
        // Add current position to history
        Vector3 currentPos = new Vector3(frame.x, 0f, frame.y);
        
        if (positionHistory.Count >= maxHistoryPoints)
        {
            positionHistory.Dequeue(); // Remove oldest position
        }
        positionHistory.Enqueue(currentPos);
        
        // Update visualizations
        UpdateHistoryLine();
        UpdatePredictionLine();
    }

    void OnTrajectoryChanged(string trajectoryId)
    {
        // Clear history when trajectory changes
        positionHistory.Clear();
        
        // Reset visualizations
        if (historyLineRenderer != null)
            historyLineRenderer.positionCount = 0;
        
        if (predictionLineRenderer != null)
            predictionLineRenderer.positionCount = 0;
    }

    void UpdateHistoryLine()
    {
        if (historyLineRenderer == null || positionHistory.Count == 0)
            return;

        // Set positions for the history line
        historyLineRenderer.positionCount = positionHistory.Count;
        
        // Convert queue to array
        Vector3[] positions = positionHistory.ToArray();
        historyLineRenderer.SetPositions(positions);
    }

    void UpdatePredictionLine()
    {
        if (predictionLineRenderer == null || currentFrame == null)
            return;
        
        // Check if we have prediction data
        if (currentFrame.p_x == null || currentFrame.p_y == null || 
            currentFrame.p_x.Length == 0 || currentFrame.p_y.Length == 0)
        {
            predictionLineRenderer.positionCount = 0;
            return;
        }
        
        // Set up prediction line
        int predictionLength = Mathf.Min(currentFrame.p_x.Length, currentFrame.p_y.Length);
        predictionLineRenderer.positionCount = predictionLength + 1; // +1 for current position
        
        // Start from current position
        Vector3 currentPos = new Vector3(currentFrame.x, 0f, currentFrame.y);
        predictionLineRenderer.SetPosition(0, currentPos);
        
        // Add predicted points
        for (int i = 0; i < predictionLength; i++)
        {
            Vector3 predictedPos = new Vector3(currentFrame.p_x[i], 0f, currentFrame.p_y[i]);
            predictionLineRenderer.SetPosition(i + 1, predictedPos);
        }
    }

    // Optional: Gizmo to show current position if marker object isn't set
    void OnDrawGizmos()
    {
        if (currentPositionMarker == null && currentFrame != null)
        {
            Gizmos.color = currentPositionColor;
            Gizmos.DrawSphere(new Vector3(currentFrame.x, 0f, currentFrame.y), 0.2f);
        }
    }
}