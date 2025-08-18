using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Represents a spline consisting of connected Bezier curves. A Bezier curve is made of four spline point objects. Two anchor points at the start and end of the curve, and two control points that determine the shape of the curve.
/// Responsible for creating and editing the spline, and for calculating the position, direction, and rotation of the spline at a given point.
/// 
/// Credit: Freya Holmer 
/// The Beauty of Bezier Curves - https://youtu.be/aVwxzDHniEw?si=hJ-wBKQBCPfJn5Rq
/// </summary>
[Serializable]
[ExecuteAlways]
public class TrackSpline : MonoBehaviour
{
    public Track track;
    
    // The below hidden public variables are used by the custom editor.
    [HideInInspector] public int selectedIndex = 0;
    [HideInInspector] public Vector3 selectedPosition;
    [HideInInspector] public float selectedRailRotation;
    [HideInInspector] public bool selectedPowerNext;
    [HideInInspector] public bool selectedPowerPrevious;
    [HideInInspector] public float selectedSpeed;
    [HideInInspector] public float gravity;
    [HideInInspector] public bool isContinuousLoop;
    [HideInInspector] public int loops;
    
    private SplinePoint[] points;
    /// <summary>
    /// Getter for the points of the spline.
    /// </summary>
    public SplinePoint[] Points => points;
    
    private CumulativeDistanceLUT[] curveLookUpTables;

    /// <summary>
    /// Getter for the look up tables for the curves of the spline.
    /// </summary>
    public CumulativeDistanceLUT[] CurveLookUpTables => curveLookUpTables;

    /// <summary>
    /// Resets the spline to four points.
    /// </summary>
    public void Reset()
    {
        selectedIndex = 0;
        GameObject[] children = new GameObject[transform.childCount];
        int i = 0;
        foreach (Transform child in transform)
        {
            children[i++] = child.gameObject;
        }
        foreach (GameObject child in children)
        {
            DestroyImmediate(child.gameObject);
        }

        Transform transformA = new GameObject("Point 0").transform;
        transformA.SetParent(transform);

        Transform transformB = new GameObject("Point 1").transform;
        transformB.SetParent(transform);

        Transform transformC = new GameObject("Point 2").transform;
        transformC.SetParent(transform);

        Transform transformD = new GameObject("Point 3").transform;
        transformD.SetParent(transform);

        points = new SplinePoint[]
        {
            new SplinePoint(transformA, new Vector3(1, 0, 0), Quaternion.identity, false, -1f),
            new SplinePoint(transformB, new Vector3(2, 0, 0), false),
            new SplinePoint(transformC, new Vector3(3, 0, 0), false),
            new SplinePoint(transformD, new Vector3(4, 0, 0), Quaternion.identity, false, -1f),
        };

        Vector3 direction = (points[1].Position - points[0].Position).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        points[3].Rotation = rotation;
        points[0].Rotation = rotation;
        UpdateSelected(0);
    }

    /// <summary>
    /// Adds a new curve to the spline. Adds three new points and uses the previous end point to determine the curve.
    /// </summary>
    public void AddCurve()
    {
        Array.Resize(ref points, points.Length + 3);
        Vector3 previousLastPosition = points[^4].LocalPosition;
        Vector3 previous2ToLastPosition = points[^5].LocalPosition;
        Vector3 direction = (previousLastPosition - previous2ToLastPosition).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        Transform transformA = new GameObject("Point " + (points.Length - 3)).transform;
        transformA.SetParent(transform);

        Transform transformB = new GameObject("Point " + (points.Length - 2)).transform;
        transformB.SetParent(transform);

        Transform transformC = new GameObject("Point " + (points.Length - 1)).transform;
        transformC.SetParent(transform);

        bool powerPrevious = points[^4].PowerPrevious;
        SplinePoint endPoint = points[^4];
        endPoint.PowerNext = powerPrevious;
        points[^3] = new SplinePoint(transformA, previousLastPosition + (direction), powerPrevious);
        points[^2] = new SplinePoint(transformB, previousLastPosition + (direction * 2), powerPrevious);
        points[^1] = new SplinePoint(transformC, previousLastPosition + (direction * 3), rotation, powerPrevious, endPoint.Speed);
    }

    /// <summary>
    /// Adds a curve of three points after a certain point in the spline.
    /// </summary>
    /// <param name="startIndex">The index along the track</param>
    public void AddCurve(int startIndex)
    {
        Array.Resize(ref points, points.Length + 3);
        Vector3 midDirection = GetDirection(startIndex, 0.5f);
        Quaternion rotation = GetRotation(startIndex, 0.5f);

        Vector3 midPoint = transform.InverseTransformPoint(GetPosition(startIndex, 0.5f));
        float distanceBetween = Vector3.Distance(points[startIndex + 3].LocalPosition, points[startIndex].LocalPosition);

        for (int i = points.Length - 4; i > startIndex + 1; i--)
        {
            points[i].GameObject.name = "Point " + (i + 3);
            points[i + 3] = points[i];
        }

        Transform transformA = new GameObject("Point " + (startIndex + 2)).transform;
        transformA.SetParent(transform);

        Transform transformB = new GameObject("Point " + (startIndex + 3)).transform;
        transformB.SetParent(transform);

        Transform transformC = new GameObject("Point " + (startIndex + 4)).transform;
        transformC.SetParent(transform);

        bool powerSpline = points[startIndex].PowerNext;
        points[startIndex + 2] = new SplinePoint(transformA, midPoint + (midDirection * distanceBetween * -0.2f), powerSpline);
        points[startIndex + 3] = new SplinePoint(transformC, midPoint, rotation, powerSpline, 
            Mathf.Lerp(points[startIndex].Speed, points[startIndex + 3].Speed, 0.5f));
        points[startIndex + 3].PowerNext = powerSpline;
        points[startIndex + 4] = new SplinePoint(transformB, midPoint + (midDirection * distanceBetween * 0.2f), powerSpline);
    }

    /// <summary>
    /// Removes the three-point curve at the end of the spline.
    /// </summary>
    public void RemoveCurve()
    {
        DestroyImmediate(points[^3].GameObject);
        DestroyImmediate(points[^2].GameObject);
        DestroyImmediate(points[^1].GameObject);
        Array.Resize(ref points, points.Length - 3);
    }

    /// <summary>
    /// Removes a three-point Bézier curve a certain point given a start index value. Used for removing curves in the
    /// middle of a spline.
    /// </summary>
    /// <param name="startIndex">The start index along a spline of the Bézier curve you are trying to remove.</param>
    public void RemoveCurve(int startIndex)
    {
        DestroyImmediate(points[startIndex + 1].GameObject);
        DestroyImmediate(points[startIndex].GameObject);
        DestroyImmediate(points[startIndex - 1].GameObject);

        for (int i = startIndex + 2; i < points.Length; i++)
        {
            points[i].GameObject.name = $"Point {i - 3}";
            points[i - 3] = points[i];
        }
        Array.Resize(ref points, points.Length - 3);
    }

    /// <summary>
    /// Updates the values displayed in the inspector GUI to the actual values of the SplinePoint.
    /// </summary>
    /// <param name="index">Index of the SplinePoint to update.</param>
    public void UpdateSelected(int index)
    {
        SplinePoint point = points[index];
        selectedPosition = point.LocalPosition;

        selectedRailRotation = point.LocalRotation.eulerAngles.z;

        selectedPowerNext = point.PowerNext;

        if (index == 0 && isContinuousLoop)
        {
            selectedPowerPrevious = points[^1].PowerPrevious;
        }
        else
        {
            selectedPowerPrevious = point.PowerPrevious;
        }

        selectedSpeed = point.Speed;
    }

    /// <summary>
    /// Updates the actual values of the SplinePoint to the values displayed in the inspector GUI.
    /// </summary>
    /// <param name="index">Index of the SplinePoint to edit.</param>
    public void EditSelected(int index)
    {
        SplinePoint point = points[index];
        point.LocalPosition = selectedPosition;

        if (index % 3 == 0)
        {
            Vector3 localEuler = point.LocalRotation.eulerAngles;
            localEuler.z = selectedRailRotation;
            point.LocalRotation = Quaternion.Euler(localEuler);

            if (point.PowerNext != selectedPowerNext)
            {
                point.PowerNext = selectedPowerNext;
                points[index + 1].PowerNext = selectedPowerNext;
                points[index + 1].PowerPrevious = selectedPowerNext;
                points[index + 2].PowerNext = selectedPowerNext;
                points[index + 2].PowerPrevious = selectedPowerNext;
                points[index + 3].PowerPrevious = selectedPowerNext;
            }

            int lastIndex = points.Length - 1;
            if (index == 0)
            {
                if (points[lastIndex].PowerPrevious != selectedPowerPrevious)
                {
                    points[lastIndex].PowerPrevious = selectedPowerPrevious;
                    points[lastIndex - 1].PowerNext = selectedPowerPrevious;
                    points[lastIndex - 1].PowerPrevious = selectedPowerPrevious;
                    points[lastIndex - 2].PowerNext = selectedPowerPrevious;
                    points[lastIndex - 2].PowerPrevious = selectedPowerPrevious;
                    points[lastIndex - 3].PowerNext = selectedPowerPrevious;
                }
            }
            else if (point.PowerPrevious != selectedPowerPrevious)
            {
                point.PowerPrevious = selectedPowerPrevious;
                points[index - 1].PowerNext = selectedPowerPrevious;
                points[index - 1].PowerPrevious = selectedPowerPrevious;
                points[index - 2].PowerNext = selectedPowerPrevious;
                points[index - 2].PowerPrevious = selectedPowerPrevious;
                points[index - 3].PowerNext = selectedPowerPrevious;
            }
            point.Speed = selectedSpeed;
        }
        else // Otherwise not one of the end points
        {
            if (point.PowerNext != selectedPowerNext)
            {
                point.PowerNext = selectedPowerNext;
                point.PowerPrevious = selectedPowerNext;

                if (index % 3 == 1) // The first control point
                {
                    points[index - 1].PowerNext = selectedPowerNext;
                    points[index + 1].PowerNext = selectedPowerNext;
                    points[index + 1].PowerPrevious = selectedPowerNext;
                    points[index + 2].PowerPrevious = selectedPowerNext;
                }
                else // The second control point
                {
                    points[index - 2].PowerNext = selectedPowerNext;
                    points[index - 1].PowerNext = selectedPowerNext;
                    points[index - 1].PowerPrevious = selectedPowerNext;
                    points[index + 1].PowerPrevious = selectedPowerNext;
                }
            }
        }
    }

    /// <summary>
    /// Changes the position and rotation of the transform of a point on the spline.
    /// </summary>
    /// <param name="index">Index of the SplinePoint transform to edit.</param>
    public void EditTransform(int index)
    {
        SplinePoint point = points[index];
        point.LocalPosition = selectedPosition;

        if (index % 3 == 0)
        {
            Vector3 localEuler = point.LocalRotation.eulerAngles;
            localEuler.z = selectedRailRotation;
            point.LocalRotation = Quaternion.Euler(localEuler);

            if (index == 0 && isContinuousLoop)
            {
                points[^1].LocalRotation = point.LocalRotation;
            }
        }
    }

    /// <summary>
    /// Changes the spline power values for an end point on the spline.
    /// </summary>
    /// <param name="index">Index of the SplinePoint power to edit.</param>
    public void EditPowerEndPoint(int index)
    {
        SplinePoint point = points[index];

        if (point.PowerNext != selectedPowerNext) // Power Next
        {
            point.PowerNext = selectedPowerNext;
            points[index + 1].PowerNext = selectedPowerNext;
            points[index + 1].PowerPrevious = selectedPowerNext;
            points[index + 2].PowerNext = selectedPowerNext;
            points[index + 2].PowerPrevious = selectedPowerNext;
            points[index + 3].PowerPrevious = selectedPowerNext;

            if (!isContinuousLoop) return;

            if (index == 0)
            {
                points[^1].PowerNext = selectedPowerNext;
            }
            else if (index == points.Length - 4)
            {
                points[0].PowerPrevious = selectedPowerNext;
            }
        }
        else if (point.PowerPrevious != selectedPowerPrevious)
        {
            if (index == 0) // Power previous is only visible on 0 if continuous loop
            {
                points[0].PowerPrevious = selectedPowerPrevious;
                points[^1].PowerPrevious = selectedPowerPrevious;
                points[^2].PowerNext = selectedPowerPrevious;
                points[^2].PowerPrevious = selectedPowerPrevious;
                points[^3].PowerNext = selectedPowerPrevious;
                points[^3].PowerPrevious = selectedPowerPrevious;
                points[^4].PowerNext = selectedPowerPrevious;
            }
            else
            {
                point.PowerPrevious = selectedPowerPrevious; // point ~3
                points[index - 1].PowerNext = selectedPowerPrevious; // point ~2
                points[index - 1].PowerPrevious = selectedPowerPrevious; // point ~2
                points[index - 2].PowerNext = selectedPowerPrevious; // point ~1
                points[index - 2].PowerPrevious = selectedPowerPrevious; // point ~1
                points[index - 3].PowerNext = selectedPowerPrevious; // point ~0

                if (index == 3 && isContinuousLoop)
                {
                    points[^1].PowerNext = selectedPowerPrevious;
                }
            }

        }
    }

    /// <summary>
    /// Changes the power values for a control point on the spline.
    /// </summary>
    /// <param name="index">Index of the SplinePoint power to edit.</param>
    public void EditPowerControlPoint(int index)
    {
        SplinePoint point = points[index];

        if (point.PowerNext != selectedPowerNext)
        {
            point.PowerNext = selectedPowerNext;
            point.PowerPrevious = selectedPowerNext;

            if (index % 3 == 1) // The first control point
            {
                points[index - 1].PowerNext = selectedPowerNext;
                points[index + 1].PowerNext = selectedPowerNext;
                points[index + 1].PowerPrevious = selectedPowerNext;
                points[index + 2].PowerPrevious = selectedPowerNext;
            }
            else // The second control point
            {
                points[index - 2].PowerNext = selectedPowerNext;
                points[index - 1].PowerNext = selectedPowerNext;
                points[index - 1].PowerPrevious = selectedPowerNext;
                points[index + 1].PowerPrevious = selectedPowerNext;
            }
        }
    }

    /// <summary>
    /// Changes the speed values for a point on the spline.
    /// </summary>
    /// <param name="index">Index of the SplinePoint speed to edit.</param>
    public void EditSpeed(int index)
    {
        points[index].Speed = selectedSpeed;

        if (index == 0 && isContinuousLoop)
        {
            points[^1].Speed = selectedSpeed;
        }
    }

    /// <summary>
    /// Gets the position of a point on the bezier curve given a 0 - 1 decimal value t.
    /// </summary>
    /// <param name="startIndex">The index of the spline point a the start of the curve.</param>
    /// <param name="t">The "t" value along the curve.</param>
    /// <returns>A point on the bezier curve.</returns>
    public Vector3 GetPosition(int startIndex, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * oneMinusT * points[startIndex].Position + 3f * oneMinusT * oneMinusT * t * points[startIndex + 1].Position +
            3f * oneMinusT * t * t * points[startIndex + 2].Position + t * t * t * points[startIndex + 3].Position;
    }

    /// <summary>
    /// Gets the direction of the curve at a certain point given a 0 - 1 decimal value t.
    /// </summary>
    /// <param name="startIndex">The index of the spline point a the start of the curve.</param>
    /// <param name="t">The "t" value along the curve.</param>
    /// <returns>A direction on the bezier curve.</returns>
    public Vector3 GetDirection(int startIndex, float t)
    {
        float oneMinusT = 1f - t;
        return (3f * oneMinusT * oneMinusT * (points[startIndex + 1].Position - points[startIndex].Position) +
            6f * oneMinusT * t * (points[startIndex + 2].Position - points[startIndex + 1].Position) +
            3f * t * t * (points[startIndex + 3].Position - points[startIndex + 2].Position)).normalized;
    }

    /// <summary>
    /// Gets on the rotation of the curve at a certain point given a 0 - 1 decimal position t.
    /// </summary>
    /// <param name="startIndex">The index of the spline point a the start of the curve.</param>
    /// <param name="t">The "t" value along the curve.</param>
    /// <returns>A rotation on the bezier curve.</returns>
    public Quaternion GetRotation(int startIndex, float t)
    {
        return Quaternion.Slerp(points[startIndex].Rotation, points[startIndex + 3].Rotation, t);
    }

    /// <summary>
    /// Gets on the rotation of the curve at a certain point given a 0 - 1 decimal position t and a direction to rotate around.
    /// </summary>
    /// <param name="startIndex">The index of the spline point a the start of the curve.</param>
    /// <param name="t">The "t" value along the curve.</param>
    /// <param name="currentCurveDirection">The direction the coaster is facing along the current curve.</param>
    /// <returns>A rotation on the bezier curve.</returns>
    public Quaternion GetRotation(int startIndex, float t, Vector3 currentCurveDirection)
    {
        Vector3 localEuler = Quaternion.LookRotation(currentCurveDirection).eulerAngles;
        localEuler.z = Mathf.LerpAngle(points[startIndex].Rotation.eulerAngles.z, points[startIndex + 3].Rotation.eulerAngles.z, 
            Mathf.SmoothStep(0, 1, t));

        return Quaternion.Euler(localEuler);
    }

    /// <summary>
    /// Saves the path and settings of a track to the Track scriptable object.
    /// </summary>
    public void SaveTrack()
    {
        track.IsContinuousLoop = isContinuousLoop;
        track.Loops = loops;

        SplinePoint.StoredSplinePoint[] storedPoints = new SplinePoint.StoredSplinePoint[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            SplinePoint point = points[i];
            SplinePoint.StoredSplinePoint storedPoint = new SplinePoint.StoredSplinePoint(point.LocalPosition, point.Rotation,
                point.PowerNext, point.PowerPrevious, point.Speed);
            storedPoints[i] = storedPoint;
        }
        track.Points = storedPoints;

        CumulativeDistanceLUT[] curveLUTs = new CumulativeDistanceLUT[points.Length / 3];
        for (int i = 0; i < points.Length / 3; i++)
        {
            curveLUTs[i] = new CumulativeDistanceLUT(this, i * 3);
        }
        track.CurveLookUpTables = curveLUTs;
        curveLookUpTables = curveLUTs;

#if UNITY_EDITOR
        EditorUtility.SetDirty(track);
#endif
    }

    /// <summary>
    /// Loads track path and settings from the current scriptable object Track.
    /// </summary>
    public void LoadTrack()
    {
        foreach (SplinePoint point in points)
        {
            DestroyImmediate(point.GameObject);
        }

        gravity = track.Gravity;
        isContinuousLoop = track.IsContinuousLoop;
        loops = track.Loops;

        points = new SplinePoint[track.Points.Length];
        for (int i = 0; i < track.Points.Length; i++)
        {
            Transform splinePointTransform = new GameObject("Point " + i).transform;
            splinePointTransform.SetParent(transform);
            SplinePoint.StoredSplinePoint point = track.Points[i];
            points[i] = new SplinePoint(splinePointTransform, point.position, point.rotation, point.speed);
            points[i].PowerNext = point.powerNext;
            points[i].PowerPrevious = point.powerPrevious;
        }

        curveLookUpTables = track.CurveLookUpTables;

        selectedIndex = 0;
        UpdateSelected(0);
    }

    /// <summary>
    /// Display the track spline lines in the editor scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (points != null)
        {
            for (int i = 0; i < (points.Length / 3); i++)
            {
                DisplayCurve(i);
            }

            for (int i = 0; i < points.Length; i += 3)
            {
                Gizmos.color = Color.black;
                SplinePoint endPoint = points[i];
                Gizmos.DrawLine(endPoint.Position, endPoint.Position + (endPoint.Transform.up * 0.5f));
            }
        }
    }

    /// <summary>
    /// Displays a curve.
    /// </summary>
    /// <param name="curveNum">The index of the curve to display. Not to be confused with the index of the spline point.</param>
    private void DisplayCurve(int curveNum)
    {
        int startIndex = curveNum * 3;
        Vector3[] transformedPositions = new Vector3[]
        {
            transform.TransformPoint(points[startIndex].LocalPosition),
            transform.TransformPoint(points[startIndex + 1].LocalPosition),
            transform.TransformPoint(points[startIndex + 2].LocalPosition),
            transform.TransformPoint(points[startIndex + 3].LocalPosition)
        };

#if UNITY_EDITOR
        if (points[startIndex].PowerNext) // The current spline is powered
        {
            Handles.DrawBezier(transformedPositions[0], transformedPositions[3], transformedPositions[1], transformedPositions[2], Color.red, null, 2);
        }
        else
        {
            Handles.DrawBezier(transformedPositions[0], transformedPositions[3], transformedPositions[1], transformedPositions[2], Color.white, null, 2);
        }
#endif
    }
}
