using System;
using UnityEngine;

/// <summary>
/// State machine component for managing roller coaster test sessions.
/// </summary>
public class RollerCoasterTest : MonoBehaviour
{
    /// <summary>
    /// Enum representing internal state.
    /// </summary>
    private enum RollerCoasterTestState
    {
        setup,
        waiting,
        startMoving,
        moving,
        end
    }
    
    /// <summary>
    /// The current internal state.
    /// </summary>
    [SerializeField] private RollerCoasterTestState currRollerCoasterTestState;
    
    /// <summary>
    /// Accumulated time for the current test phase.
    /// </summary>
    private float time;
    
    /// <summary>
    /// How long to wait before starting the roller coaster movement.
    /// </summary>
    [SerializeField] private float waitPeriod;

    private int currentLevel;
    
    /// <summary>
    /// Component for spawning targets for the user to look at.
    /// </summary>
    [SerializeField] private LookAtTarget lookAtTarget;

    /// <summary>
    /// Coaster train object storing coaster speed and other data.
    /// </summary>
    [SerializeField] private CoasterTrain playerCoaster;

    /// <summary>
    /// Roller coaster object responsible for moving the coaster train along the track.
    /// </summary>
    [SerializeField] private RollerCoaster rollerCoaster;

    public Action<bool> SetUpAction;

    private float targetsDestroyed;
    private float totalTargets;

    /// <summary>
    /// Initial setup of the component.
    /// </summary>
    private void Start()
    {
        currentLevel = 0;
        currRollerCoasterTestState = RollerCoasterTestState.setup;
        lookAtTarget.OnDestroyTarget += DestroyedTarget;
        lookAtTarget.OnCreateTarget += CreateTarget;
    }

    /// <summary>
    /// Set the player objects.
    /// </summary>
    /// <param name="centeredPlayer">The player's transform.</param>
    /// <param name="vrCamera">The camera's transform.</param>
    public void SetPlayerObjects(Transform centeredPlayer, Transform vrCamera)
    {
        Debug.Log($"center coaster named {centeredPlayer}");
        playerCoaster = new CoasterTrain(centeredPlayer);
        lookAtTarget.VRCamera = vrCamera;
    }

    /// <summary>
    /// Main update loop with state machine.
    /// </summary>
    /// <returns>The next experiment state.</returns>
    public ExperimentState Tick()
    {
        switch (currRollerCoasterTestState)
        {
            case RollerCoasterTestState.setup:
                currRollerCoasterTestState = SetUp();
                break;
            case RollerCoasterTestState.waiting:
                currRollerCoasterTestState = Wait();
                break;
            case RollerCoasterTestState.startMoving:
                currRollerCoasterTestState = StartMoving();
                break;
            case RollerCoasterTestState.moving:
                currRollerCoasterTestState = Move();
                break;
            case RollerCoasterTestState.end:
                End();
                return ExperimentState.balanceTest;
        }
        time += Time.deltaTime;
        return ExperimentState.rollerCoasterTest;
    }

    /// <summary>
    /// Set up the roller coaster test.
    /// </summary>
    /// <returns>The next roller coaster test state.</returns>
    private RollerCoasterTestState SetUp()
    {

        SetUpAction?.Invoke(true);

        time = 0.0f;
        targetsDestroyed = 0;
        totalTargets = 0;

        rollerCoaster.SetUp(currentLevel);
        rollerCoaster.StartCoaster(playerCoaster);
        return RollerCoasterTestState.waiting;
    }

    /// <summary>
    /// Handles the waiting period before the roller coaster starts moving.
    /// </summary>
    /// <returns>The next roller coaster test state.</returns>
    private RollerCoasterTestState Wait()
    {
        if (time > waitPeriod)
        {
            time = 0.0f;
            return RollerCoasterTestState.startMoving;
        }
        else
        {
            return RollerCoasterTestState.waiting;
        }
    }

    /// <summary>
    /// Start the movement phase. Initialize spawning targets.
    /// </summary>
    /// <returns>The next roller coaster test state.</returns>
    private RollerCoasterTestState StartMoving()
    {
        lookAtTarget.SetUp();
        return RollerCoasterTestState.moving;
    }

    /// <summary>
    /// Handles the active movement phase with target tracking.
    /// </summary>
    /// <returns>The next roller coaster test state.</returns>
    private RollerCoasterTestState Move()
    {
        if (rollerCoaster.Move(playerCoaster))
        {
            return RollerCoasterTestState.end;
        }
        else
        {
            lookAtTarget.Tick(playerCoaster); // run the look at object program
            return RollerCoasterTestState.moving;
        }
    }

    /// <summary>
    /// Callback for when a new target is created during the test.
    /// </summary>
    public void CreateTarget()
    {
        totalTargets++;
    }

    /// <summary>
    /// Callback for when a target is successfully destroyed by the user.
    /// </summary>
    public void DestroyedTarget()
    {
        targetsDestroyed++;
    }

    /// <summary>
    /// Get the ratio of targets destroyed to total targets created.
    /// </summary>
    /// <returns>The ratio of targets destroyed to total targets created.</returns>
    public float GetTargetsDestroyedRatio()
    {
        return targetsDestroyed / totalTargets;
    }

    /// <summary>
    /// Gets the total runtime of the current test session.
    /// </summary>
    /// <returns>Time elapsed in seconds</returns>
    public float GetRunTime()
    {
        Debug.Log("Time "+ time);
        return time;
    }

    /// <summary>
    /// Handles test completion and prepares for the next level.
    /// </summary>
    private void End()
    {
        currentLevel++;
        playerCoaster.Reset();
        currRollerCoasterTestState = RollerCoasterTestState.setup;
    }
}
