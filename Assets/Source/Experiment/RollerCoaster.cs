using UnityEngine;

/// <summary>
/// Component for a roller coaster that moves along a Track.
/// </summary>
[RequireComponent(typeof(TrackSpline))]
public class RollerCoaster : MonoBehaviour
{
    [SerializeField] private Track[] tracks;
    private TrackSpline spline;

    [SerializeField] private float coasterStartSpeed;

    /// <summary>
    /// Initial setup of the component.
    /// </summary>
    private void Start()
    {
        spline = GetComponent<TrackSpline>();
    }

    /// <summary>
    /// Set up the roller coaster.
    /// </summary>
    /// <param name="trackIndex">The index of the specific track to use.</param>
    public void SetUp(int trackIndex)
    {
        spline.track = tracks[trackIndex];
        spline.LoadTrack();
    }

    /// <summary>
    /// Start the coaster train.
    /// </summary>
    /// <param name="coaster">The coaster train.</param>
    public void StartCoaster(CoasterTrain coaster)
    {
        coaster.Speed = coasterStartSpeed;

        if (spline.Points[0].PowerNext)
        {
            MovePowered(coaster);
        }
        else
        {
            MoveGravity(coaster);
        }
    }

    /// <summary>
    /// Move the coaster along the track.
    /// </summary>
    /// <param name="coaster">The coaster train.</param>
    /// <returns>Returns true if the player has finished the track and false otherwise.</returns>
    public bool Move(CoasterTrain coaster)
    {
        if (!SetCurveAndDistance(coaster))
        {
            if (spline.Points[coaster.CurveIndex * 3].PowerNext)
            {
                if (coaster.IsInitialPowered)
                {
                    coaster.InitialPoweredSpeed = coaster.Speed;
                    coaster.InitialVelocity = coaster.Velocity;
                    coaster.InitialPoweredCurveIndex = coaster.CurveIndex;
                    coaster.IsInitialPowered = false;

                    int currCurveIndex = coaster.CurveIndex;
                    if (spline.Points[(currCurveIndex + 1) * 3].PowerNext) // do not power across loop
                    {
                        float distance = spline.CurveLookUpTables[currCurveIndex].CurveLength;
                        currCurveIndex++;
                        while (spline.Points[(currCurveIndex + 1) * 3].PowerNext)
                        {
                            distance += spline.CurveLookUpTables[currCurveIndex].CurveLength;
                            currCurveIndex++;
                        }
                        distance += spline.CurveLookUpTables[currCurveIndex].CurveLength;
                        float targetSpeed = spline.Points[(currCurveIndex + 1) * 3].Speed;
                        float acceleration = ((targetSpeed * targetSpeed) - (coaster.InitialPoweredSpeed * coaster.InitialPoweredSpeed)) /
                            (2 * distance);

                        while (currCurveIndex > coaster.CurveIndex)
                        {
                            distance -= spline.CurveLookUpTables[currCurveIndex].CurveLength;
                            spline.Points[currCurveIndex * 3].Speed = Mathf.Sqrt((coaster.InitialPoweredSpeed * coaster.InitialPoweredSpeed) +
                                (2 * acceleration * distance));
                            currCurveIndex--;
                        }
                    }
                }

                MovePowered(coaster);
            }
            else
            {
                MoveGravity(coaster);
            }
            
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Sets the curve that the player should currently be on and the distance they have traveled. 
    /// </summary>
    /// <param name="coaster">The coaster train.</param>
    /// <returns>Returns true if the player has finished the track and false otherwise.</returns>
    private bool SetCurveAndDistance(CoasterTrain coaster)
    {
        float deltaDistance = coaster.Speed * Time.deltaTime; // calculate how far it should travel

        coaster.CurveDistance += deltaDistance;

        while (coaster.CurveDistance > spline.CurveLookUpTables[coaster.CurveIndex].CurveLength)
        {
            coaster.IsInitialPowered = true;
            if (coaster.CurveIndex + 1 >= spline.Points.Length / 3) // coaster traveled an entire loop
            {
                if (coaster.CurrentLoop++ < spline.loops && spline.isContinuousLoop)
                {
                    coaster.CurveDistance -= spline.CurveLookUpTables[coaster.CurveIndex].CurveLength; // subtract the distance of the curve and go to the 0th curve
                    coaster.CurveIndex = 0;
                }
                else // Otherwise end the traversal
                {
                    SplinePoint point = spline.Points[spline.Points.Length - 1];
                    Vector3 position = point.Position;
                    coaster.MovingObject.position = position;
                    coaster.MovingObject.rotation = point.Rotation;
                    coaster.CurveIndex = spline.Points.Length / 3;
                    coaster.CurveDistance = 0.0f;
                    return true;
                }
            }
            else
            {
                coaster.CurveDistance -= spline.CurveLookUpTables[coaster.CurveIndex].CurveLength; // subtract the distance of the curve and go to the next curve
                coaster.CurveIndex++;
            }
        }
        return false;
    }


    /// <summary>
    /// Move the coaster along the track with powered rails. Lerps the speed between the initial powered speed and the target speed at the end of the
    /// powered curves section.
    /// </summary>
    /// <param name="coaster">The coaster train.</param>
    private void MovePowered(CoasterTrain coaster)
    {
        // find the value of t (not the t for position)
        float t = Mathf.Clamp01(coaster.CurveDistance / spline.CurveLookUpTables[coaster.CurveIndex].CurveLength);

        float tDistance = spline.CurveLookUpTables[coaster.CurveIndex].GetTFromDistance(coaster.CurveDistance);

        // find the position
        coaster.MovingObject.position = spline.GetPosition(coaster.CurveIndex * 3, tDistance);

        // Get current direction of travel
        Vector3 currentDirection = spline.GetDirection(coaster.CurveIndex * 3, tDistance);

        // Set the rotation of the coaster
        Quaternion rotation = spline.GetRotation(coaster.CurveIndex * 3, t, currentDirection);
        coaster.MovingObject.rotation = rotation;

        // Calculate speed interpolated between start speed and final target speed
        coaster.Speed = Mathf.Lerp(coaster.InitialPoweredSpeed, spline.Points[(coaster.CurveIndex + 1) * 3].Speed, t);

        // Calculate velocity
        coaster.Velocity = coaster.Speed * currentDirection;
    }

    /// <summary>
    /// Move the coaster along the track according to gravity.
    /// </summary>
    /// <param name="coaster">The coaster train.</param>
    private void MoveGravity(CoasterTrain coaster)
    {
        // find the value of t (not the t for position)
        float t = Mathf.Clamp01(coaster.CurveDistance / spline.CurveLookUpTables[coaster.CurveIndex].CurveLength);

        float tDistance = spline.CurveLookUpTables[coaster.CurveIndex].GetTFromDistance(coaster.CurveDistance);

        // find the position
        coaster.MovingObject.position = spline.GetPosition(coaster.CurveIndex * 3, tDistance);

        // Get current direction of travel
        Vector3 currentDirection = spline.GetDirection(coaster.CurveIndex * 3, tDistance);

        // Set the rotation of the coaster
        Quaternion rotation = spline.GetRotation(coaster.CurveIndex * 3, t, currentDirection);
        coaster.MovingObject.rotation = rotation;

        // Calculate a normal that has no local x component (upwards facing normal)
        Vector3 normal = (Vector3.up + Vector3.Project(Vector3.down, currentDirection)).normalized;

        // Calculate the added speed
        float addedSpeed = (currentDirection.y > 0 ? -1 : 1) * spline.gravity * Mathf.Sin(Vector3.Angle(normal, Vector3.up) * Mathf.Deg2Rad) *
            Time.deltaTime;

        coaster.Speed += addedSpeed;

        coaster.Velocity = coaster.Speed * currentDirection;
    }
}
