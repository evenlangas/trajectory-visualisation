using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

public class TrajectoryReplay : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private TextAsset csvFile;
    [SerializeField] private float timeoutThreshold = 5.0f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loopPlayback = false;
    
    [Header("Playback Controls")]
    [SerializeField] [Range(0.1f, 10f)] private float playbackSpeed = 1.0f;
    [SerializeField] private bool useRealTimestamps = true;
    [SerializeField] private float fixedTimeStep = 0.1f; // Used if not using real timestamps

    [Header("Events")]
    public UnityEvent<ReplayDataPoint> OnDataPointUpdated;
    public UnityEvent OnReplayStarted;
    public UnityEvent OnReplayPaused;
    public UnityEvent OnReplayCompleted;
    
    private List<ReplayDataPoint> dataPoints = new List<ReplayDataPoint>();
    private int currentPointIndex = 0;
    private bool isPlaying = false;
    private Coroutine replayCoroutine;

    // Public properties for external access
    public Vector2 CurrentPosition { get; private set; }
    public float CurrentVelocity { get; private set; }
    public float CurrentOrientation { get; private set; }
    public string TrajectoryID { get; private set; }
    public bool IsPlaying => isPlaying;
    public int TotalPoints => dataPoints.Count;
    public int CurrentIndex => currentPointIndex;
    public float ReplayProgress => dataPoints.Count > 0 ? (float)currentPointIndex / dataPoints.Count : 0f;
    
    // Properties for playback control
    public float PlaybackSpeed {
        get => playbackSpeed;
        set => playbackSpeed = Mathf.Clamp(value, 0.1f, 10f);
    }

    void Start()
    {
        if (csvFile != null)
        {
            ParseCSVData();
            
            if (playOnStart && dataPoints.Count > 0)
            {
                StartReplay();
            }
        }
        else
        {
            Debug.LogError("CSV file not assigned!");
        }
    }

    public void ParseCSVData()
    {
        dataPoints.Clear();
        
        StringReader reader = new StringReader(csvFile.text);
        
        // Skip header
        reader.ReadLine();
        
        int lineNumber = 1; // Start at 1 since we already read the header
        string line;
        
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            try
            {
                string[] values = line.Split(',');
                
                if (values.Length >= 11)
                {
                    // Create safe parsing methods
                    string idPrefix = values[0].Trim();
                    string id = values[1].Trim();
                    
                    // Try to parse the floats safely
                    if (!TryParseFloat(values[2], out float x))
                    {
                        Debug.LogWarning($"Line {lineNumber}: Failed to parse X coordinate '{values[2]}'");
                        continue;
                    }
                    
                    if (!TryParseFloat(values[3], out float y))
                    {
                        Debug.LogWarning($"Line {lineNumber}: Failed to parse Y coordinate '{values[3]}'");
                        continue;
                    }
                    
                    if (!TryParseFloat(values[4], out float velocityScalar))
                    {
                        Debug.LogWarning($"Line {lineNumber}: Failed to parse velocity '{values[4]}'");
                        continue;
                    }
                    
                    if (!TryParseFloat(values[5], out float orientation))
                    {
                        Debug.LogWarning($"Line {lineNumber}: Failed to parse orientation '{values[5]}'");
                        continue;
                    }
                    
                    if (!long.TryParse(values[6], out long timestamp))
                    {
                        Debug.LogWarning($"Line {lineNumber}: Failed to parse timestamp '{values[6]}'");
                        continue;
                    }
                    
                    if (!int.TryParse(values[7], out int workstation))
                    {
                        Debug.LogWarning($"Line {lineNumber}: Failed to parse workstation '{values[7]}'");
                        continue;
                    }
                    
                    string trajectoryId = values[8].Trim();
                    
                    if (!TryParseFloat(values[9], out float start))
                    {
                        Debug.LogWarning($"Line {lineNumber}: Failed to parse start '{values[9]}'");
                        continue;
                    }
                    
                    if (!TryParseFloat(values[10], out float goal))
                    {
                        Debug.LogWarning($"Line {lineNumber}: Failed to parse goal '{values[10]}'");
                        continue;
                    }
                    
                    // Create the data point
                    ReplayDataPoint point = new ReplayDataPoint
                    {
                        IdPrefix = idPrefix,
                        Id = id,
                        Position = new Vector2(x, y),
                        VelocityScalar = velocityScalar,
                        Orientation = orientation,
                        Timestamp = timestamp,
                        Workstation = workstation,
                        TrajectoryId = trajectoryId,
                        Start = start,
                        Goal = goal
                    };
                    
                    dataPoints.Add(point);
                }
                else
                {
                    Debug.LogWarning($"Line {lineNumber}: Insufficient values (expected 11, got {values.Length})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Line {lineNumber}: Error parsing CSV line: '{line}'. Exception: {ex.Message}");
            }
        }
        
        Debug.Log($"Successfully loaded {dataPoints.Count} data points from CSV");
        
        // Analyze timestamp format for the first few entries to understand the time scale
        if (dataPoints.Count >= 2)
        {
            long firstTimestamp = dataPoints[0].Timestamp;
            long secondTimestamp = dataPoints[1].Timestamp;
            
            long diff = secondTimestamp - firstTimestamp;
            Debug.Log($"Time difference between first two entries: {diff} timestamp units");
            
            // Check if timestamps are in microseconds (typical for Unix timestamps)
            if (diff > 1000000)
            {
                Debug.Log("Timestamps appear to be in microseconds - adjusting playback accordingly");
            }
            else if (diff > 1000)
            {
                Debug.Log("Timestamps appear to be in milliseconds");
            }
            else
            {
                Debug.Log("Timestamps appear to be in seconds or custom units");
            }
        }
    }
    
    private bool TryParseFloat(string value, out float result)
    {
        // Trim any whitespace
        string trimmed = value.Trim();
        
        // Try parsing in multiple ways
        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }
        
        // Try handling scientific notation explicitly
        if (trimmed.Contains("E") || trimmed.Contains("e"))
        {
            try
            {
                result = Convert.ToSingle(trimmed, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                result = 0f;
                return false;
            }
        }
        
        result = 0f;
        return false;
    }

    public void StartReplay()
    {
        if (dataPoints.Count == 0)
        {
            Debug.LogWarning("No data to replay!");
            return;
        }
        
        if (isPlaying)
        {
            StopReplay();
        }
        
        OnReplayStarted?.Invoke();
        isPlaying = true;
        replayCoroutine = StartCoroutine(ReplayData());
    }
    
    public void PauseReplay()
    {
        if (isPlaying)
        {
            isPlaying = false;
            
            if (replayCoroutine != null)
            {
                StopCoroutine(replayCoroutine);
                replayCoroutine = null;
            }
            
            OnReplayPaused?.Invoke();
        }
    }
    
    public void ResumeReplay()
    {
        if (!isPlaying && currentPointIndex < dataPoints.Count)
        {
            isPlaying = true;
            replayCoroutine = StartCoroutine(ReplayData());
            OnReplayStarted?.Invoke();
        }
    }
    
    public void StopReplay()
    {
        PauseReplay();
        currentPointIndex = 0;
    }
    
    public void JumpToTime(float normalizedTime)
    {
        int targetIndex = Mathf.Clamp(Mathf.FloorToInt(normalizedTime * dataPoints.Count), 0, dataPoints.Count - 1);
        currentPointIndex = targetIndex;
        
        if (currentPointIndex < dataPoints.Count)
        {
            ReplayDataPoint point = dataPoints[currentPointIndex];
            UpdateCurrentData(point);
            OnDataPointUpdated?.Invoke(point);
        }
    }

    private IEnumerator ReplayData()
    {
        if (dataPoints.Count == 0 || currentPointIndex >= dataPoints.Count)
        {
            yield break;
        }
        
        // Get the first data point
        ReplayDataPoint currentPoint = dataPoints[currentPointIndex];
        UpdateCurrentData(currentPoint);
        OnDataPointUpdated?.Invoke(currentPoint);
        
        while (currentPointIndex < dataPoints.Count - 1 && isPlaying)
        {
            ReplayDataPoint nextPoint = dataPoints[currentPointIndex + 1];
            
            float waitTime;
            
            if (useRealTimestamps)
            {
                // Calculate real-world time difference between these points
                long timeDiffMicroseconds = nextPoint.Timestamp - currentPoint.Timestamp;
                
                // Convert microseconds to seconds
                float timeDiffSeconds = timeDiffMicroseconds / 1000000000f;
                
                // Apply playback speed
                waitTime = timeDiffSeconds / playbackSpeed;
                
                // Check for data gaps
                if (timeDiffSeconds > timeoutThreshold)
                {
                    Debug.Log($"Data gap detected ({timeDiffSeconds} seconds) - continuing without delay");
                    waitTime = 0f; // Skip waiting if there's a large gap
                }
            }
            else
            {
                // Use fixed time step instead of real timestamps
                waitTime = fixedTimeStep / playbackSpeed;
            }
            
            // Wait for the calculated time
            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }
            
            // Move to next point
            currentPointIndex++;
            currentPoint = dataPoints[currentPointIndex];
            
            // Update current data and fire event
            UpdateCurrentData(currentPoint);
            OnDataPointUpdated?.Invoke(currentPoint);
        }
        
        // Replay finished
        isPlaying = false;
        OnReplayCompleted?.Invoke();
        
        // Handle looping
        if (loopPlayback)
        {
            currentPointIndex = 0;
            StartReplay();
        }
    }
    
    private void UpdateCurrentData(ReplayDataPoint point)
    {
        CurrentPosition = point.Position;
        CurrentVelocity = point.VelocityScalar;
        CurrentOrientation = point.Orientation;
        TrajectoryID = point.TrajectoryId;
    }
    
    // Public methods for UI controls
    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = Mathf.Clamp(speed, 0.1f, 10f);
    }
    
    public void IncreasePlaybackSpeed()
    {
        playbackSpeed = Mathf.Clamp(playbackSpeed * 1.5f, 0.1f, 10f);
        Debug.Log($"Playback speed increased to {playbackSpeed}x");
    }
    
    public void DecreasePlaybackSpeed()
    {
        playbackSpeed = Mathf.Clamp(playbackSpeed / 1.5f, 0.1f, 10f);
        Debug.Log($"Playback speed decreased to {playbackSpeed}x");
    }
    
    public void ToggleUseRealTimestamps()
    {
        useRealTimestamps = !useRealTimestamps;
        Debug.Log($"Using real timestamps: {useRealTimestamps}");
    }
}

[System.Serializable]
public class ReplayDataPoint
{
    public string IdPrefix;
    public string Id;
    public Vector2 Position;
    public float VelocityScalar;
    public float Orientation;
    public long Timestamp;
    public int Workstation;
    public string TrajectoryId;
    public float Start;
    public float Goal;
}