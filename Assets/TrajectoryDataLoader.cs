using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TrajectoryFrame
{
    public int t_id;              // Trajectory ID
    public long t;                // Timestamp
    public float x;               // Current X position
    public float y;               // Current Y position
    public float[] p_x;           // Predicted X positions
    public float[] p_y;           // Predicted Y positions
}

[Serializable]
public class TrajectoryData
{
    public Dictionary<string, List<TrajectoryFrame>> trajectories = new Dictionary<string, List<TrajectoryFrame>>();
}

public class TrajectoryDataLoader : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string jsonFilePath = "Assets/Data/all_simplified_trajectories.json";
    [SerializeField] private Transform humanObject;
    [SerializeField] private float playbackSpeed = 1.0f;
    [SerializeField] private bool autoPlay = true;
    [SerializeField] private bool loopTrajectories = true;

    [Header("Unity Events")]
    public UnityEvent<TrajectoryFrame> OnFrameChanged;
    public UnityEvent<string> OnTrajectoryChanged;
    public UnityEvent<string, int, int> OnTrajectoryProgress;

    // Private variables
    private TrajectoryData trajectoryData;
    private List<string> trajectoryIds = new List<string>();
    private int currentTrajectoryIndex = 0;
    private int currentFrameIndex = 0;
    private string currentTrajectoryId;
    private List<TrajectoryFrame> currentTrajectory;
    private bool isPlaying = false;
    private float lastFrameTime;
    private float frameDuration = 0.1f; // Default 100ms between frames

    void Start()
    {
        LoadTrajectoryData();
        
        if (autoPlay && trajectoryIds.Count > 0)
        {
            PlayTrajectory();
        }
    }

    void Update()
    {
        if (!isPlaying || currentTrajectory == null || currentTrajectory.Count == 0)
            return;

        // Calculate time between frames based on playback speed
        float elapsedTime = Time.time - lastFrameTime;
        
        // Move to next frame based on speed
        if (elapsedTime >= frameDuration / playbackSpeed)
        {
            lastFrameTime = Time.time;
            AdvanceFrame();
        }
    }

    public void LoadTrajectoryData()
    {
        try
        {
            // Load and parse JSON file
            if (File.Exists(jsonFilePath))
            {
                string jsonText = File.ReadAllText(jsonFilePath);
                ParseTrajectoryJson(jsonText);
                Debug.Log($"Successfully loaded trajectory data with {trajectoryIds.Count} trajectories");
            }
            else
            {
                Debug.LogError($"JSON file not found at path: {jsonFilePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading trajectory data: {e.Message}");
        }
    }

    public void ParseTrajectoryJson(string jsonText)
    {
        try
        {
            // We'll use a more direct approach since Unity's JsonUtility doesn't handle dictionaries
            trajectoryData = new TrajectoryData();
            trajectoryData.trajectories = new Dictionary<string, List<TrajectoryFrame>>();
            
            // Check if the JSON starts with a curly brace (indicating a dictionary/object)
            if (jsonText.TrimStart().StartsWith("{"))
            {
                Debug.Log("JSON appears to be in dictionary format, parsing manually...");
                
                // Replace this simplified parsing with a more robust solution using Unity's JsonUtility for each trajectory
                // First we'll preprocess the JSON to handle the outer dictionary structure
                Dictionary<string, List<TrajectoryFrame>> parsedData = new Dictionary<string, List<TrajectoryFrame>>();
                
                // Start recursive descent parser
                int index = 0;
                SkipWhitespace(jsonText, ref index);
                
                // Expect opening brace
                if (jsonText[index] != '{')
                {
                    throw new Exception("Expected '{' at start of JSON");
                }
                index++;
                
                SkipWhitespace(jsonText, ref index);
                
                // Parse main dictionary entries
                while (index < jsonText.Length && jsonText[index] != '}')
                {
                    // Parse key (trajectory ID)
                    SkipWhitespace(jsonText, ref index);
                    string key = ParseString(jsonText, ref index);
                    SkipWhitespace(jsonText, ref index);
                    
                    // Expect colon
                    if (jsonText[index] != ':')
                    {
                        throw new Exception($"Expected ':' after key at position {index}");
                    }
                    index++;
                    
                    SkipWhitespace(jsonText, ref index);
                    
                    // Parse array of frames
                    List<TrajectoryFrame> frames = ParseFrameArray(jsonText, ref index);
                    parsedData[key] = frames;
                    
                    SkipWhitespace(jsonText, ref index);
                    
                    // If comma, continue to next entry
                    if (jsonText[index] == ',')
                    {
                        index++;
                        SkipWhitespace(jsonText, ref index);
                    }
                    else if (jsonText[index] != '}')
                    {
                        throw new Exception($"Expected ',' or '}}' after value at position {index}");
                    }
                }
                
                // Copy to trajectory data
                trajectoryData.trajectories = parsedData;
            }
            else
            {
                Debug.LogError("JSON format not recognized. Expected dictionary format.");
                return;
            }
            
            // Extract trajectory IDs
            trajectoryIds.Clear();
            foreach (var trajId in trajectoryData.trajectories.Keys)
            {
                trajectoryIds.Add(trajId);
            }
            
            // Sort trajectory IDs numerically if possible
            trajectoryIds.Sort((a, b) => {
                if (int.TryParse(a, out int aInt) && int.TryParse(b, out int bInt))
                    return aInt.CompareTo(bInt);
                else
                    return string.Compare(a, b);
            });
            
            // Reset to first trajectory
            currentTrajectoryIndex = 0;
            if (trajectoryIds.Count > 0)
            {
                SetTrajectory(trajectoryIds[0]);
            }
            
            Debug.Log($"Successfully parsed {trajectoryIds.Count} trajectories");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing trajectory JSON: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private List<TrajectoryFrame> ParseFrameArray(string json, ref int index)
    {
        List<TrajectoryFrame> frames = new List<TrajectoryFrame>();
        
        // Expect opening bracket
        if (json[index] != '[')
        {
            throw new Exception($"Expected '[' for frame array at position {index}");
        }
        index++;
        
        SkipWhitespace(json, ref index);
        
        // Handle empty array
        if (json[index] == ']')
        {
            index++;
            return frames;
        }
        
        // Parse frames
        while (index < json.Length)
        {
            SkipWhitespace(json, ref index);
            
            // Parse frame object
            TrajectoryFrame frame = ParseFrameObject(json, ref index);
            frames.Add(frame);
            
            SkipWhitespace(json, ref index);
            
            // If comma, continue to next frame
            if (json[index] == ',')
            {
                index++;
                SkipWhitespace(json, ref index);
            }
            else if (json[index] == ']')
            {
                // End of array
                index++;
                break;
            }
            else
            {
                throw new Exception($"Expected ',' or ']' after frame at position {index}");
            }
        }
        
        return frames;
    }
    
    private TrajectoryFrame ParseFrameObject(string json, ref int index)
    {
        TrajectoryFrame frame = new TrajectoryFrame();
        
        // Expect opening brace
        if (json[index] != '{')
        {
            throw new Exception($"Expected '{{' for frame object at position {index}");
        }
        index++;
        
        SkipWhitespace(json, ref index);
        
        // Parse frame properties
        while (index < json.Length && json[index] != '}')
        {
            // Parse property key
            SkipWhitespace(json, ref index);
            string key = ParseString(json, ref index);
            SkipWhitespace(json, ref index);
            
            // Expect colon
            if (json[index] != ':')
            {
                throw new Exception($"Expected ':' after property key at position {index}");
            }
            index++;
            
            SkipWhitespace(json, ref index);
            
            // Parse property value based on key
            switch (key)
            {
                case "t_id":
                    frame.t_id = ParseInt(json, ref index);
                    break;
                case "t":
                    frame.t = ParseLong(json, ref index);
                    break;
                case "x":
                    frame.x = ParseFloat(json, ref index);
                    break;
                case "y":
                    frame.y = ParseFloat(json, ref index);
                    break;
                case "p_x":
                    frame.p_x = ParseFloatArray(json, ref index);
                    break;
                case "p_y":
                    frame.p_y = ParseFloatArray(json, ref index);
                    break;
                default:
                    // Skip unknown properties
                    SkipValue(json, ref index);
                    break;
            }
            
            SkipWhitespace(json, ref index);
            
            // If comma, continue to next property
            if (json[index] == ',')
            {
                index++;
                SkipWhitespace(json, ref index);
            }
            else if (json[index] != '}')
            {
                throw new Exception($"Expected ',' or '}}' after property value at position {index}");
            }
        }
        
        // Expect closing brace
        if (json[index] != '}')
        {
            throw new Exception($"Expected '}}' at end of frame object at position {index}");
        }
        index++;
        
        return frame;
    }
    
    private void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length && char.IsWhiteSpace(json[index]))
        {
            index++;
        }
    }
    
    private string ParseString(string json, ref int index)
    {
        // Expect opening quote
        if (json[index] != '"')
        {
            throw new Exception($"Expected '\"' at start of string at position {index}");
        }
        index++;
        
        int startIndex = index;
        
        // Find closing quote
        while (index < json.Length && json[index] != '"')
        {
            // Skip escaped quotes
            if (json[index] == '\\' && index + 1 < json.Length && json[index + 1] == '"')
            {
                index += 2;
            }
            else
            {
                index++;
            }
        }
        
        if (index >= json.Length)
        {
            throw new Exception("Unterminated string");
        }
        
        string value = json.Substring(startIndex, index - startIndex);
        index++; // Skip closing quote
        
        return value;
    }
    
    private int ParseInt(string json, ref int index)
    {
        string numStr = ParseNumber(json, ref index);
        return int.Parse(numStr);
    }
    
    private long ParseLong(string json, ref int index)
    {
        string numStr = ParseNumber(json, ref index);
        return long.Parse(numStr);
    }
    
    private float ParseFloat(string json, ref int index)
    {
        string numStr = ParseNumber(json, ref index);
        return float.Parse(numStr, System.Globalization.CultureInfo.InvariantCulture);
    }
    
    private string ParseNumber(string json, ref int index)
    {
        int startIndex = index;
        
        // Parse sign
        if (json[index] == '-')
        {
            index++;
        }
        
        // Parse digits
        while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || 
               json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-'))
        {
            index++;
        }
        
        return json.Substring(startIndex, index - startIndex);
    }
    
    private float[] ParseFloatArray(string json, ref int index)
    {
        List<float> values = new List<float>();
        
        // Expect opening bracket
        if (json[index] != '[')
        {
            throw new Exception($"Expected '[' for float array at position {index}");
        }
        index++;
        
        SkipWhitespace(json, ref index);
        
        // Handle empty array
        if (json[index] == ']')
        {
            index++;
            return values.ToArray();
        }
        
        // Parse float values
        while (index < json.Length)
        {
            SkipWhitespace(json, ref index);
            
            // Parse float value
            float value = ParseFloat(json, ref index);
            values.Add(value);
            
            SkipWhitespace(json, ref index);
            
            // If comma, continue to next value
            if (json[index] == ',')
            {
                index++;
                SkipWhitespace(json, ref index);
            }
            else if (json[index] == ']')
            {
                // End of array
                index++;
                break;
            }
            else
            {
                throw new Exception($"Expected ',' or ']' after float value at position {index}");
            }
        }
        
        return values.ToArray();
    }
    
    private void SkipValue(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        
        if (index >= json.Length)
            return;
            
        char c = json[index];
        
        if (c == '"')
        {
            // Skip string
            index++;
            while (index < json.Length && json[index] != '"')
            {
                // Skip escaped quotes
                if (json[index] == '\\' && index + 1 < json.Length && json[index + 1] == '"')
                {
                    index += 2;
                }
                else
                {
                    index++;
                }
            }
            index++; // Skip closing quote
        }
        else if (c == '[')
        {
            // Skip array
            index++;
            int depth = 1;
            while (index < json.Length && depth > 0)
            {
                if (json[index] == '[')
                {
                    depth++;
                }
                else if (json[index] == ']')
                {
                    depth--;
                }
                else if (json[index] == '"')
                {
                    // Skip string within array
                    index++;
                    while (index < json.Length && json[index] != '"')
                    {
                        // Skip escaped quotes
                        if (json[index] == '\\' && index + 1 < json.Length && json[index + 1] == '"')
                        {
                            index += 2;
                        }
                        else
                        {
                            index++;
                        }
                    }
                }
                index++;
            }
        }
        else if (c == '{')
        {
            // Skip object
            index++;
            int depth = 1;
            while (index < json.Length && depth > 0)
            {
                if (json[index] == '{')
                {
                    depth++;
                }
                else if (json[index] == '}')
                {
                    depth--;
                }
                else if (json[index] == '"')
                {
                    // Skip string within object
                    index++;
                    while (index < json.Length && json[index] != '"')
                    {
                        // Skip escaped quotes
                        if (json[index] == '\\' && index + 1 < json.Length && json[index + 1] == '"')
                        {
                            index += 2;
                        }
                        else
                        {
                            index++;
                        }
                    }
                }
                index++;
            }
        }
        else if (c == 't' || c == 'f' || c == 'n')
        {
            // Skip true, false, null
            if (json.Substring(index, Math.Min(5, json.Length - index)).StartsWith("true"))
            {
                index += 4;
            }
            else if (json.Substring(index, Math.Min(6, json.Length - index)).StartsWith("false"))
            {
                index += 5;
            }
            else if (json.Substring(index, Math.Min(5, json.Length - index)).StartsWith("null"))
            {
                index += 4;
            }
            else
            {
                index++; // Unknown value, skip character
            }
        }
        else if (char.IsDigit(c) || c == '-')
        {
            // Skip number
            ParseNumber(json, ref index);
        }
        else
        {
            // Unknown value type, skip character
            index++;
        }
    }

    public void SetTrajectory(string trajectoryId)
    {
        if (trajectoryData.trajectories.TryGetValue(trajectoryId, out List<TrajectoryFrame> trajectory))
        {
            currentTrajectoryId = trajectoryId;
            currentTrajectory = trajectory;
            currentFrameIndex = 0;
            
            Debug.Log($"Set current trajectory to {trajectoryId} with {currentTrajectory.Count} frames");
            OnTrajectoryChanged?.Invoke(trajectoryId);
            
            // Set initial position
            if (currentTrajectory.Count > 0)
            {
                UpdateHumanPosition(currentTrajectory[0]);
                OnFrameChanged?.Invoke(currentTrajectory[0]);
                OnTrajectoryProgress?.Invoke(currentTrajectoryId, currentFrameIndex + 1, currentTrajectory.Count);
            }
        }
        else
        {
            Debug.LogError($"Trajectory ID {trajectoryId} not found in data");
        }
    }

    public void PlayTrajectory()
    {
        if (currentTrajectory != null && currentTrajectory.Count > 0)
        {
            isPlaying = true;
            lastFrameTime = Time.time;
            Debug.Log("Started trajectory playback");
        }
        else
        {
            Debug.LogWarning("No trajectory data to play");
        }
    }

    public void PauseTrajectory()
    {
        isPlaying = false;
        Debug.Log("Paused trajectory playback");
    }

    public void TogglePlayPause()
    {
        if (isPlaying)
            PauseTrajectory();
        else
            PlayTrajectory();
    }

    private void AdvanceFrame()
    {
        if (currentTrajectory == null || currentTrajectory.Count == 0)
            return;

        currentFrameIndex++;
        
        // Check if we've reached the end of the current trajectory
        if (currentFrameIndex >= currentTrajectory.Count)
        {
            // Move to next trajectory
            if (loopTrajectories)
            {
                currentTrajectoryIndex = (currentTrajectoryIndex + 1) % trajectoryIds.Count;
                string nextTrajectoryId = trajectoryIds[currentTrajectoryIndex];
                SetTrajectory(nextTrajectoryId);
            }
            else
            {
                // Just stop playback
                isPlaying = false;
                currentFrameIndex = currentTrajectory.Count - 1; // Stay on last frame
                Debug.Log("Reached end of trajectory playback");
            }
        }
        
        // Update position and notify
        if (currentFrameIndex < currentTrajectory.Count)
        {
            var frame = currentTrajectory[currentFrameIndex];
            UpdateHumanPosition(frame);
            OnFrameChanged?.Invoke(frame);
            OnTrajectoryProgress?.Invoke(currentTrajectoryId, currentFrameIndex + 1, currentTrajectory.Count);
        }
    }

    private void UpdateHumanPosition(TrajectoryFrame frame)
    {
        if (humanObject != null)
        {
            // Update the position of the human object
            // Note: Z coordinate is kept as is, we only update X and Y from the data
            Vector3 newPosition = new Vector3(frame.x, humanObject.position.y, frame.y);
            humanObject.position = newPosition;
        }
    }

    public float GetPlaybackSpeed()
    {
        return playbackSpeed;
    }

    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetJsonFilePath(string filePath)
    {
        jsonFilePath = filePath;
        LoadTrajectoryData();
    }
}