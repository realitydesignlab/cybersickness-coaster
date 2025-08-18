using UnityEngine;

/// <summary>
/// Class for storing data about the coaster train.
/// </summary>
[System.Serializable]
public class CoasterTrain
{
    private Transform movingObject;
    /// <summary>
    /// Getter for the transform that moves along the track.
    /// </summary>
    public Transform MovingObject => movingObject;

    private float speed;

    /// <summary>
    /// Getter/setter for the speed of the coaster train.
    /// </summary>
    public float Speed { get => speed; set => speed = value; }

    private Vector3 velocity;

    /// <summary>
    /// Getter/setter for the velocity of the coaster train.
    /// </summary>
    public Vector3 Velocity { get => velocity; set => velocity = value; }

    private float initialPoweredSpeed;

    /// <summary>
    /// Getter/setter for the initial speed of the coaster train when it reaches a powered section.
    /// </summary>
    public float InitialPoweredSpeed { get => initialPoweredSpeed; set => initialPoweredSpeed = value; }

    private Vector3 initialPoweredVelocity;

    /// <summary>
    /// Getter/setter for the initial velocity of the coaster train when it reaches a powered section.
    /// </summary>
    public Vector3 InitialVelocity { get => initialPoweredVelocity; set => initialPoweredVelocity = value; }

    private int curveIndex;

    /// <summary>
    /// Getter/setter for the index of the current Bézier curve along the track spline.
    /// </summary>
    public int CurveIndex { get => curveIndex; set => curveIndex = value; }

    private float curveDistance;

    /// <summary>
    /// Getter/setter for the distance traveled along the current curve.
    /// </summary>
    public float CurveDistance { get => curveDistance; set => curveDistance = value; }

    private int currentLoop;

    /// <summary>
    /// Getter/setter for the current loop.
    /// </summary>
    public int CurrentLoop { get => currentLoop; set => currentLoop = value; }

    private bool isInitialPowered;

    /// <summary>
    /// Getter/setter for the initial powered state.
    /// </summary>
    public bool IsInitialPowered { get => isInitialPowered; set => isInitialPowered = value; }

    private int initialPoweredCurveIndex;
    
    /// <summary>
    /// Getter/setter for the index of the initial powered curve.
    /// </summary>
    public int InitialPoweredCurveIndex { get => initialPoweredCurveIndex; set => initialPoweredCurveIndex = value; }

    /// <summary>
    /// Constructor for the coaster train.
    /// </summary>
    /// <param name="movingObject">The object that moves along the track. The user transform.</param>
    public CoasterTrain(Transform movingObject)
    {
        this.movingObject = movingObject;
        Reset();
    }

    /// <summary>
    /// Resets the coaster train to its initial state.
    /// </summary>
    public void Reset()
    {
        speed = 0f;
        velocity = Vector3.zero;
        initialPoweredSpeed = 0f;
        initialPoweredVelocity = Vector3.zero;
        currentLoop = 1;
        curveIndex = 0;
        curveDistance = 0.0f;
        isInitialPowered = false;
        initialPoweredCurveIndex = 0;
    }
}