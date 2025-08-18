using UnityEngine;

/// <summary>
/// Represents a point belonging to a Bézier curve spline. A spline is a collection of Bézier curves. A Bézier curve is made of four spline points.
/// </summary>
[System.Serializable]
public class SplinePoint
{
    /// <summary>
    /// Serializable struct for storing a spline point in the Track scriptable object.
    /// </summary>
    [System.Serializable]
    public struct StoredSplinePoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool powerNext;
        public bool powerPrevious;
        public float speed;

        /// <summary>
        /// Constructor for the StoredSplinePoint struct.
        /// </summary>
        /// <param name="position">The position of the spline point.</param>
        /// <param name="rotation">The rotation of the spline point.</param>
        /// <param name="powerNext">Whether the track between this point and the next point is powered (overrides acceleration).</param>
        /// <param name="powerPrevious">Whether the track between this point and the previous point is powered (overrides acceleration).</param>
        /// <param name="speed">The speed of the spline point, if it is a powered point.</param>
        public StoredSplinePoint(Vector3 position, Quaternion rotation, 
            bool powerNext, bool powerPrevious, float speed)
        {
            this.position = position;
            this.rotation = rotation;
            this.powerNext = powerNext;
            this.powerPrevious = powerPrevious;
            this.speed = speed;
        }
    }

    [SerializeField] private Transform transform;
    [SerializeField] private bool powerNext;
    [SerializeField] private bool powerPrevious;
    [SerializeField] private float speed;

    /// <summary>
    /// Getter/setter for the local position of the 
    /// </summary>
    public Vector3 LocalPosition { get => transform.localPosition; set => transform.localPosition = value; }

    /// <summary>
    /// Getter/setter for the position of the spline point.
    /// </summary>
    public Vector3 Position { get => transform.position; set => transform.position = value; }

    /// <summary>
    /// Getter/setter for the local rotation of the spline point.
    /// </summary>
    public Quaternion LocalRotation { get => transform.localRotation; set => transform.localRotation = value; }

    /// <summary>
    /// Getter/setter for the rotation of the spline point.
    /// </summary>
    public Quaternion Rotation { get => transform.rotation; set => transform.rotation = value; }

    /// <summary>
    /// Getter/setter for the whether the curve between this point and the next point is powered.
    /// </summary>
    public bool PowerNext { get => powerNext; set => powerNext = value; }

    /// <summary>
    /// Getter/setter for the whether the curve between this point and the previous point is powered.
    /// </summary>
    public bool PowerPrevious { get => powerPrevious; set => powerPrevious = value; }

    /// <summary>
    /// Getter/setter for the target speed of the coaster by the end of the powered curve segment.
    /// </summary>
    public float Speed { get => speed; set => speed = value; }

    /// <summary>
    /// Getter for the transform of the spline point.
    /// </summary>
    public Transform Transform => transform;
    
    /// <summary>
    /// Getter for the game object of the spline point.
    /// </summary>
    public GameObject GameObject => transform.gameObject;

    /// <summary>
    /// Constructor for a spline point.
    /// </summary>
    /// <param name="transform">The transform of the spline point.</param>
    /// <param name="position">The position of the spline point.</param>
    /// <param name="powerSpline">Whether the curve between this point and the next point is powered.</param>
    public SplinePoint(Transform transform, Vector3 position, bool powerSpline)
    {
        this.transform = transform;
        this.transform.localPosition = position;
        powerNext = powerSpline;
        powerPrevious = powerSpline;
        speed = -1f;
    }

    /// <summary>
    /// Constructor for a spline point.
    /// </summary>
    /// <param name="transform">The transform of the spline point.</param>
    /// <param name="position">The position of the spline point.</param>
    /// <param name="rotation">The rotation of the spline point.</param>
    /// <param name="speed">The target speed of the coaster by the end of the powered curve segment.</param>
    public SplinePoint(Transform transform, Vector3 position, Quaternion rotation, float speed)
    {
        this.transform = transform;
        this.transform.localPosition = position;
        this.transform.rotation = rotation;
        this.powerNext = false;
        this.powerPrevious = false;
        this.speed = speed;
    }

    /// <summary>
    /// Constructor for a spline point.
    /// </summary>
    /// <param name="transform">The transform of the spline point.</param>
    /// <param name="position">The position of the spline point.</param>
    /// <param name="rotation">The rotation of the spline point.</param>
    /// <param name="powerSpline">Whether the curve between this point and the next point is powered.</param>
    /// <param name="speed">The target speed of the coaster by the end of the powered curve segment.</param>
    public SplinePoint(Transform transform, Vector3 position, Quaternion rotation, bool powerSpline, float speed)
    {
        this.transform = transform;
        this.transform.localPosition = position;
        this.transform.rotation = rotation;
        this.powerNext = false;
        this.powerPrevious = powerSpline;
        this.speed = speed;
    }
}