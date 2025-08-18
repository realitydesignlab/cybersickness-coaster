using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// State machine component for measuring user balance after a roller coaster test. 
/// </summary>
public class MeasureBalance : MonoBehaviour
{
    /// <summary>
    /// Enum representing internal state.
    /// </summary>
    private enum MeasureBalanceState
    {
        setup,
        waiting,
        initialIgnoring,
        testing,
        finalIgnoring,
        end
    }
    
    /// <summary>
    /// The current internal state.
    /// </summary>
    [SerializeField] private MeasureBalanceState currentState;

    private float waitingTime;

    /// <summary>
    /// How long to inform the user that their balance is going to be recorded.
    /// </summary>
    [SerializeField] private float waitPeriod;
    
    private float testingTime;

    /// <summary>
    /// How long to ignore balance data before starting to record.
    /// </summary>
    [SerializeField] private float initialIgnoreDataPeriod;
    
    /// <summary>
    /// How long to record balance data.
    /// </summary>
    [SerializeField] private float testPeriod;
    
    /// <summary>
    /// How long to ignore balance data after finishing recording.
    /// </summary>
    [SerializeField] private float finalIgnoreDataPeriod;

    /// <summary>
    /// The frequency of which to record data. TODO convert this to fixed interval
    /// </summary>
    [SerializeField] private int captureFrame;

    private int currentFrame;
    
    private List<Vector3> positions;

    private Transform centeredPlayer;
    private Transform vrCamera;

    /// <summary>
    /// Text output gameobject.
    /// </summary>
    [SerializeField] private GameObject text;

    /// <summary>
    /// The timer text.
    /// </summary>
    [SerializeField] private TMPro.TextMeshProUGUI timer;

    /// <summary>
    /// The distance from the user's head you would like the measuring balance text to be displayed.
    /// </summary>
    [SerializeField] private float textDistanceFromHead;
    
    /// <summary>
    /// The height of the measuring balance text off of the ground.
    /// </summary>
    [SerializeField] private float textHeight;

    /// <summary>
    /// The position you would like to move the user to to begin measuring.
    /// </summary>
    [SerializeField] private Transform measurementPosition;

    public Action<bool> SetUpAction;

    /// <summary>
    /// Initial setup of the component.
    /// </summary>
    private void Start()
    {
        positions = new List<Vector3>();

        currentFrame = 0;

        text.SetActive(false);
        currentState = MeasureBalanceState.setup;
    }

    /// <summary>
    /// Set the player objects.
    /// </summary>
    /// <param name="centeredPlayer">The player's transform.</param>
    /// <param name="vrCamera">The camera's transform.</param>
    public void SetPlayerObjects(Transform centeredPlayer, Transform vrCamera)
    {
        this.centeredPlayer = centeredPlayer;
        this.vrCamera = vrCamera;
    }

    /// <summary>
    /// Main update loop with state machine.
    /// </summary>
    /// <returns>The next experiment state.</returns>
    public ExperimentState Tick()
    {
        switch (currentState)
        {
            case MeasureBalanceState.setup:
                currentState = SetUp();
                break;
            case MeasureBalanceState.waiting:
                currentState = Waiting();
                break;
            case MeasureBalanceState.initialIgnoring:
                currentState = InitialIgnore();
                break;
            case MeasureBalanceState.testing:
                currentState = Test();
                break;
            case MeasureBalanceState.finalIgnoring:
                currentState = FinalIgnore();
                break;
            case MeasureBalanceState.end:
                End();
                return ExperimentState.survey;
        }
        return ExperimentState.balanceTest;
    }

    /// <summary>
    /// Teleport the player to the measurement position.
    /// </summary>
    public void Teleport()
    {
        centeredPlayer.position = measurementPosition.position;
        centeredPlayer.rotation = measurementPosition.rotation;
    }

    /// <summary>
    /// Set up the balance test.
    /// </summary>
    /// <returns>The next measure balance state.</returns>
    private MeasureBalanceState SetUp()
    {
        SetUpAction?.Invoke(false);
        text.SetActive(true);
        positions.Clear();
        Teleport();
        Vector3 position = centeredPlayer.position + centeredPlayer.forward * textDistanceFromHead;
        position.y += textHeight;
        text.transform.position = position;

        waitingTime = waitPeriod;
        testingTime = initialIgnoreDataPeriod + testPeriod + finalIgnoreDataPeriod;

        timer.text = string.Format("Stand up! Measuring balance in: {0:00}", waitingTime);

        currentFrame = 0;
        return MeasureBalanceState.waiting;
    }

    /// <summary>
    /// Initial wait informing the user that their balance is going to be recorded.
    /// </summary>
    /// <returns>The next measure balance state.</returns>
    private MeasureBalanceState Waiting()
    {
        if (waitingTime <= 0.0f)
        {
            Debug.Log("Starting initial ignore of balance data.");
            return MeasureBalanceState.initialIgnoring;
        }
        else
        {
            waitingTime -= Time.deltaTime;
            timer.text = string.Format("Stand up! Measuring balance in: {0:00}", waitingTime);
            return MeasureBalanceState.waiting;
        }
    }

    /// <summary>
    /// Ignore initial balance data for a period of time.
    /// </summary>
    /// <returns>The next measure balance state.</returns>
    private MeasureBalanceState InitialIgnore()
    {
        if (testingTime <= testPeriod + finalIgnoreDataPeriod)
        {
            Debug.Log("Measuring balance values.");
            return MeasureBalanceState.testing;
        }
        else
        {
            testingTime -= Time.deltaTime;
            timer.text = string.Format("Stand still! Measuring balance: {0:00}", testingTime);
            return MeasureBalanceState.initialIgnoring;
        }
    }

    /// <summary>
    /// Record balance data.
    /// </summary>
    /// <returns>The next measure balance state.</returns>
    private MeasureBalanceState Test()
    {
        if (testingTime <= finalIgnoreDataPeriod)
        {
            currentFrame = 0;
            Debug.Log("Finished measuring balance values.");
            return MeasureBalanceState.finalIgnoring;
        }
        else
        {
            testingTime -= Time.deltaTime;
            timer.text = string.Format("Stand still! Measuring balance: {0:00}", testingTime);

            if (currentFrame % captureFrame == 0)
            {
                positions.Add(vrCamera.position);
            }
            currentFrame++;
            return MeasureBalanceState.testing;
        }
    }

    /// <summary>
    /// Ignore final balance data for a period of time.
    /// </summary>
    /// <returns>The next measure balance state.</returns>
    private MeasureBalanceState FinalIgnore()
    {
        if (testingTime <= 0.0f)
        {
            testingTime = 0.0f;
            timer.text = string.Format("Stand still! Measuring balance: {0:00}", 0);
            return MeasureBalanceState.end;
        }
        else
        {
            testingTime -= Time.deltaTime;
            timer.text = string.Format("Stand still! Measuring balance: {0:00}", testingTime);
            return MeasureBalanceState.finalIgnoring;
        }
    }

    /// <summary>
    /// End the balance test.
    /// </summary>
    private void End()
    {
        text.SetActive(false);
        currentState = MeasureBalanceState.setup;
    }

    /// <summary>
    /// Get the moment of inertia results.
    /// </summary>
    /// <returns>The moment of inertia results.</returns>
    public double GetMomentOfInertiaResults()
    {
        double sum = 0;
        Vector3 averagePosition = CalculateAverage();
        foreach (Vector3 point in positions)
        {
            sum += Mathf.Pow(Vector3.Distance(point, averagePosition), 2);
        }
        return sum;
    }

    /// <summary>
    /// Calculate the average position.
    /// </summary>
    /// <returns>The average position.</returns>
    private Vector3 CalculateAverage()
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 point in positions)
        {
            sum += point;
        }
        return sum / positions.Count;
    }
}
