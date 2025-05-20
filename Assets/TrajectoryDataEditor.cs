using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(TrajectoryDataLoader))]
public class TrajectoryDataLoaderEditor : Editor
{
    private TrajectoryDataLoader loader;
    private string filePath;

    private void OnEnable()
    {
        loader = (TrajectoryDataLoader)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Trajectory Controls", EditorStyles.boldLabel);

        // Display file selection
        EditorGUILayout.BeginHorizontal();
        filePath = EditorGUILayout.TextField("JSON File Path", filePath);
        
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFilePanel("Select Trajectory JSON", "Assets", "json");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert to project-relative path if possible
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                filePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Load JSON File"))
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                loader.SetJsonFilePath(filePath);
            }
        }

        EditorGUILayout.Space();
        
        // Playback controls
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play"))
        {
            loader.PlayTrajectory();
        }
        
        if (GUILayout.Button("Pause"))
        {
            loader.PauseTrajectory();
        }
        EditorGUILayout.EndHorizontal();

        // Speed controls
        EditorGUILayout.BeginHorizontal();
        float speed = loader.GetPlaybackSpeed();
        float newSpeed = EditorGUILayout.Slider("Speed", speed, 0.1f, 5f);
        
        if (newSpeed != speed)
        {
            loader.SetPlaybackSpeed(newSpeed);
        }
        
        if (GUILayout.Button("1x", GUILayout.Width(30)))
        {
            loader.SetPlaybackSpeed(1f);
        }
        
        if (GUILayout.Button("2x", GUILayout.Width(30)))
        {
            loader.SetPlaybackSpeed(2f);
        }
        
        if (GUILayout.Button("0.5x", GUILayout.Width(40)))
        {
            loader.SetPlaybackSpeed(0.5f);
        }
        EditorGUILayout.EndHorizontal();
        
        // Apply changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}

[CustomEditor(typeof(TrajectoryVisualizer))]
public class TrajectoryVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Make sure the TrajectoryDataLoader component is assigned properly and both scripts are on active GameObjects in your scene.", MessageType.Info);
    }
}
#endif