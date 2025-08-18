using System;
using UnityEngine;

/// <summary>
/// Component for spawning targets along the track for the user to look at.
/// Keeps user attention on the environment to ensure they are not distracted.
/// </summary>
public class LookAtTarget : MonoBehaviour
{
    /// <summary>
    /// Represents a target that the user can look at.
    /// </summary>
    private class Target
    {
        private Transform transform;
        private int targetCurveIndex;
        private float targetDistance;
        private int targetCurrentLoop;
        private float lookTime; // time the target has been looked at

        /// <summary>
        /// Getter for the target transform.
        /// </summary>
        public Transform Transform => transform;

        /// <summary>
        /// Getter for the target curve index.
        /// </summary>
        public int TargetCurveIndex => targetCurveIndex;

        /// <summary>
        /// Getter for the target distance along the curve index.
        /// </summary>
        public float TargetDistance => targetDistance;

        /// <summary>
        /// Getter for the target current loop.
        /// </summary>
        public int TargetCurrentLoop => targetCurrentLoop;

        /// <summary>
        /// Getter for the time the target has been looked at.
        /// </summary>
        public float LookTime => lookTime;

        /// <summary>
        /// Constructor for a target.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="targetCurveIndex">Target index of the curve along the track spline.</param>
        /// <param name="targetDistance">Target distance along the curve index.</param>
        /// <param name="targetCurrentLoop">Target current loop.</param>
        public Target(Transform transform, int targetCurveIndex, float targetDistance, int targetCurrentLoop)
        {
            this.transform = transform;
            this.targetCurveIndex = targetCurveIndex;
            this.targetDistance = targetDistance;
            this.targetCurrentLoop = targetCurrentLoop;
            lookTime = 0.0f;
        }

        /// <summary>
        /// Records the time the target has been looked at.
        /// </summary>
        public void IncreaseLookTime()
        {
            lookTime += Time.deltaTime;
        }
    }

    private TrackSpline spline;
    private AudioSource audioSource;
    [SerializeField] private GameObject targetPrefab;
    private float timeSincePreviousTarget; // target becomes more likely to spawn as time goes on
    
    /// <summary>
    /// Minimum possible time between target spawns.
    /// </summary>
    [SerializeField] private float minTimeTargetSpawn;
    
    /// <summary>
    /// Maximum possible time between target spawns.
    /// </summary>
    [SerializeField] private float maxTimeTargetSpawn;

    /// <summary>
    /// Minimum possible distance from the player to the target.
    /// </summary>
    [SerializeField] private float minTargetDistance;

    /// <summary>
    /// Multiplier for the target distance based on the player's current speed.
    /// </summary>
    [SerializeField] private float targetDistanceMultiplier;

    /// <summary>
    /// Maximum distance from the track that the target can spawn.
    /// </summary>
    [SerializeField] private float spawnCircleRadius;

    /// <summary>
    /// Time the player must look at a target to destroy it.
    /// </summary>
    [SerializeField] private float lookPeriod;

    /// <summary>
    /// Maximum angle the player can look at a target to destroy it.
    /// </summary>
    [SerializeField] private float maxLookAngle;

    /// <summary>
    /// Current target on the track. There is only one target at a time.
    /// </summary>
    private Target target;

    public Action OnDestroyTarget;
    public Action OnCreateTarget;

    /// <summary>
    /// Getter/setter for the VR camera transform.
    /// </summary>
    public Transform VRCamera { private get; set; }

    /// <summary>
    /// Initial setup on application start.
    /// </summary>
    private void Start()
    {
        spline = GetComponent<TrackSpline>();
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Set up the component. SetUp() runs each level.
    /// </summary>
    public void SetUp()
    {
        if (target != null)
        {
            Destroy(target.Transform.gameObject);
        }
        target = null;
        timeSincePreviousTarget = 0.0f;
    }

    /// <summary>
    /// Update loop for the component.
    /// </summary>
    /// <param name="player">The player on the rails.</param>
    public void Tick(CoasterTrain player)
    {
        if (target != null)
        {
            float angle = Vector3.Angle(VRCamera.forward, target.Transform.position - VRCamera.position);
            if (angle <= maxLookAngle) // player looking at target
            {
                target.IncreaseLookTime();
                if (target.LookTime >= lookPeriod) // player looked at target long enough to destroy it
                {
                    OnDestroyTarget?.Invoke();
                    Destroy(target.Transform.gameObject);
                    audioSource.Play();
                    target = null;
                    timeSincePreviousTarget = 0.0f;
                    return;
                }
            }

            if (target.TargetCurrentLoop <= player.CurrentLoop && target.TargetCurveIndex <= player.CurveIndex && 
                target.TargetDistance <= player.CurveDistance) // passed the target
            {
                Destroy(target.Transform.gameObject);
                target = null;
                timeSincePreviousTarget = 0.0f;
            }
        }
        else
        {
            timeSincePreviousTarget += Time.deltaTime;
            if (timeSincePreviousTarget > minTimeTargetSpawn)
            {
                float random = UnityEngine.Random.Range(Mathf.Clamp(timeSincePreviousTarget, 0, maxTimeTargetSpawn), maxTimeTargetSpawn); // becomes more likely to spawn
                if (random > maxTimeTargetSpawn - 0.01f)
                {
                    CreateTargetOnRails(player);
                    OnCreateTarget?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Creates a target near the rails in front of the player.
    /// </summary>
    /// <param name="player">The player on the rails.</param>
    private void CreateTargetOnRails(CoasterTrain player)
    {
        // choose curve in front of the player
        int targetCurveIndex = player.CurveIndex;

        float targetDistance = Mathf.Max(player.CurveDistance + minTargetDistance, player.CurveDistance + (player.Speed * targetDistanceMultiplier));

        int targetCurrentLoop = player.CurrentLoop;

        while (targetDistance > spline.CurveLookUpTables[targetCurveIndex].CurveLength)
        {
            if (targetCurveIndex + 1 >= spline.Points.Length / 3)
            {
                if (player.CurrentLoop + 1 < spline.loops && spline.isContinuousLoop)
                {
                    targetDistance -= spline.CurveLookUpTables[targetCurveIndex].CurveLength; // subtract the distance of the curve and go to the 0th curve
                    targetCurveIndex = 0;
                    targetCurrentLoop++;
                }
                else // Too close to the end of the track to make a new balloon.
                {
                    return;
                }
            }
            else
            {
                targetDistance -= spline.CurveLookUpTables[targetCurveIndex++].CurveLength; // subtract the distance of the curve and go to the next curve
            }
        }

        float tDistance = spline.CurveLookUpTables[targetCurveIndex].GetTFromDistance(targetDistance);
        Vector3 center = spline.GetPosition(targetCurveIndex * 3, tDistance);
        Vector3 direction = spline.GetDirection(targetCurveIndex * 3, tDistance);
        Quaternion rotation = spline.GetRotation(targetCurveIndex * 3, targetDistance / 
            spline.CurveLookUpTables[targetCurveIndex].CurveLength, direction);

        Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * spawnCircleRadius; // raycast in direction to not make go through objects.
        if (Physics.SphereCast(center, 0.35f, rotation * randomPoint, out RaycastHit hit, randomPoint.magnitude, 1 << 9))
        {
            target = new Target(Instantiate(targetPrefab, center + rotation * (randomPoint.normalized * 
                Mathf.Clamp(hit.distance - 1, 0, Mathf.Infinity)), Quaternion.identity, spline.transform).transform,
                targetCurveIndex, targetDistance, targetCurrentLoop);
        }
        else
        {
            target = new Target(Instantiate(targetPrefab, center + rotation * randomPoint, Quaternion.identity, 
                spline.transform).transform, targetCurveIndex, targetDistance, targetCurrentLoop);
        }

        target.Transform.name = "Target Balloon";
    }
}
