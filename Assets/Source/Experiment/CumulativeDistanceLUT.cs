using UnityEngine;

/// <summary>
/// Look up table data structure for finding the "t" value along a Bézier curve given a target distance. There is no formula to get the length of a
/// Bézier curve given a value "t".
/// 
/// Credit: Freya Holmer 
/// The Beauty of Bezier Curves - https://youtu.be/aVwxzDHniEw?si=hJ-wBKQBCPfJn5Rq
/// </summary>
[System.Serializable]
public class CumulativeDistanceLUT
{
    private const float IntervalT = 0.01f;
    private const float IntervalTargetDistance = 0.01f;
    [SerializeField] private float intervalDistance;

    public float[] lut;

    /// <summary>
    /// Getter for the length of the curve.
    /// </summary>
    public float CurveLength => (lut.Length - 1) * intervalDistance;

    /// <summary>
    /// Constructor for the cumulative distance look up table.
    /// </summary>
    /// <param name="spline">Roller coaster track spline.</param>
    /// <param name="startIndex">Index of the first point of the curve along the track spline.</param>
    public CumulativeDistanceLUT(TrackSpline spline, int startIndex)
    {
        // Each float in this array is the cumulative distance from t = 0 to t = 1.
        // The length of the array is ceil(1.0f / IntervalT) + 1.
        // UseDistanceIntervals() is used to convert this array to look up by t value to get the cumulative distance.
        int arrayLength = Mathf.CeilToInt(1.0f / IntervalT);

        lut = new float[arrayLength + 1];

        Vector3 previousPosition = spline.GetPosition(startIndex, 0.0f);

        lut[0] = 0.0f;

        for (int i = 1; i < arrayLength; i++)
        {
            float t = IntervalT * i;
            Vector3 currentPosition = spline.GetPosition(startIndex, t);
            float distance = Vector3.Distance(previousPosition, currentPosition);
            lut[i] = lut[i - 1] + distance;
            previousPosition = currentPosition;
        }

        lut[arrayLength] = lut[arrayLength - 1] + Vector3.Distance(previousPosition, spline.GetPosition(startIndex, 1.0f)); // last point must be t = 1.0f

        lut = UseDistanceIntervals(lut);
    }

    /// <summary>
    /// Convert a look up table that looks up by t value to get the cumulative distance to a look up table that looks up by distance to get the t 
    /// value.
    /// </summary>
    /// <param name="lut">Look up table that looks up by t value to get the cumulative distance. To use this look up table, use your desired t value
    /// divided by IntervalT to get the index of the look up table, which gets the cumulative distance of that t value.</param>
    /// <returns>Look up table that looks up by distance to get the t value. To use this look up table, use your desired distance divided by
    /// IntervalTargetDistance to get the index of the look up table, which gets the t value of that distance. If your distance lies between, it will
    /// interpolate between the two t values that the distance lies between.</returns>
    private float[] UseDistanceIntervals(float[] lut)
    {
        float arcLength = lut[^1];

        int divisions = Mathf.CeilToInt(arcLength / IntervalTargetDistance);
        intervalDistance = arcLength / divisions;

        float[] tByDistanceLut = new float[divisions + 1];

        tByDistanceLut[0] = 0.0f;

        int j = 1;
        for (int i = 1; i < divisions; i++)
        {
            float distance = intervalDistance * i; // distance trying to find a t for

            for (; distance > lut[j]; j++); // keep incrementing j until it distance is less than or equal to the value in the Look up table

            float lerpBetweenTs = Mathf.InverseLerp(lut[j - 1], lut[j], distance);
            tByDistanceLut[i] = Mathf.Lerp((j - 1) * IntervalT, j * IntervalT, lerpBetweenTs);
        }

        tByDistanceLut[^1] = 1.0f;

        return tByDistanceLut;
    }

    /// <summary>
    /// Get the "t" value along the curve given a target distance.
    /// </summary>
    /// <param name="distance">Target distance along the curve.</param>
    /// <returns>The "t" value along the curve.</returns>
    public float GetTFromDistance(float distance)
    {
        float arcLength = (lut.Length - 1) * intervalDistance;

        if (distance >= arcLength)
        {
            return 1.0f;
        }
        else
        {
            // find the lower and upper index that the distance fits into the lut
            int lowerIndex = (int)(distance / intervalDistance);
            int upperIndex = lowerIndex + 1;

            float lerpBetweenDistances = Mathf.InverseLerp(intervalDistance * lowerIndex, intervalDistance * upperIndex, distance); // t value between two distances

            return Mathf.Lerp(lut[lowerIndex], lut[upperIndex], lerpBetweenDistances);
        }
    }
}
