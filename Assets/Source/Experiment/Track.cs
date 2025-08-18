using UnityEngine;

/// <summary>
/// Scriptable object for storing coaster track data.
/// </summary>
[CreateAssetMenu(menuName = "Cybersick Coaster Track")]
public class Track : ScriptableObject
{
    /// <summary>
    /// The natural gravity affecting the coaster along the track.
    /// </summary>
    [SerializeField] private float gravity = 9.81f;

    /// <summary>
    /// Whether the track is a continuous loop or has a disjointed start and end.
    /// </summary>
    [SerializeField] private bool isContinuousLoop;

    /// <summary>
    /// The number of times the coaster will travel around the track loop.
    /// </summary>
    [SerializeField] private int loops;

    /// <summary>
    /// The points that make up the path of the track. Four points make up a Bézier curve. A collection of Bézier curves makes a spline.
    /// </summary>
    [SerializeField] private SplinePoint.StoredSplinePoint[] points;

    /// <summary>
    /// Look up tables for each Bézier curve along the track.
    /// </summary>
    [SerializeField] private CumulativeDistanceLUT[] curveLookUpTables;

    /// <summary>
    /// Getter/setter for the gravity of the track.
    /// </summary>
    public float Gravity { get => gravity; set => gravity = value; }

    /// <summary>
    /// Getter/setter for whether the track is a continuous loop.
    /// </summary>
    public bool IsContinuousLoop { get => isContinuousLoop; set => isContinuousLoop = value; }

    /// <summary>
    /// Getter/setter for the number of loops for the track.
    /// </summary>
    public int Loops { get => loops; set => loops = value; }

    /// <summary>
    /// Getter/setter for the points of the track.
    /// </summary>
    public SplinePoint.StoredSplinePoint[] Points { get => points; set => points = value; }

    /// <summary>
    /// Getter/setter for the curve look up tables of the track.
    /// </summary>
    public CumulativeDistanceLUT[] CurveLookUpTables { get => curveLookUpTables; set => curveLookUpTables = value; }
}
