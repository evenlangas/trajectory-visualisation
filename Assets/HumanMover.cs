using UnityEngine;

public class HumanMover : MonoBehaviour
{
    [SerializeField] private TrajectoryReplay replaySystem;
    [SerializeField] private float heightOffset = 0f; // In case you need Z-axis offset
    [SerializeField] private bool rotateWithOrientation = true;
    
    private void Start()
    {
        if (replaySystem == null)
        {
            replaySystem = FindObjectOfType<TrajectoryReplay>();
        }
        
        if (replaySystem != null)
        {
            replaySystem.OnDataPointUpdated.AddListener(OnDataPointReceived);
        }
        else
        {
            Debug.LogError("No TrajectoryReplay found!");
        }
    }
    
    private void OnDataPointReceived(ReplayDataPoint dataPoint)
    {
        // Update position
        transform.position = new Vector3(dataPoint.Position.x, heightOffset, dataPoint.Position.y);
        
        // Update rotation if enabled
        if (rotateWithOrientation)
        {
            transform.rotation = Quaternion.Euler(0, -dataPoint.Orientation, 0);
        }
    }
}