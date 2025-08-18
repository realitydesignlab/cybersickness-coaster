using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a VR survey system with a 4-point Likert scale for measuring user responses.
/// Handles survey state management, UI interactions, and result processing.
/// </summary>
public class Survey : MonoBehaviour
{
    /// <summary>
    /// Enum representing the different options on the Likert scale.
    /// </summary>
    public enum Scale
    {
        notAtAll,
        slightly,
        moderately,
        very
    }

    /// <summary>
    /// Represents a single survey question with its UI toggles.
    /// </summary>
    [Serializable]
    public class MatrixScale
    {
        [SerializeField] private string name;
        [SerializeField] private bool toggled;
        [SerializeField] private Toggle notAtAll;
        [SerializeField] private Toggle slightly;
        [SerializeField] private Toggle moderately;
        [SerializeField] private Toggle very;

        /// <summary>
        /// Getter for the field name of the survey question in the Qualtrics survey.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Getter/setter for whether an option on the scale has been selected.
        /// </summary>
        public bool Toggled { get => toggled; set => toggled = value; }

        /// <summary>
        /// Getter for the "Not at all" toggle.
        /// </summary>
        public Toggle NotAtAll => notAtAll;

        /// <summary>
        /// Getter for the "Slightly" toggle.
        /// </summary>
        public Toggle Slightly => slightly;

        /// <summary>
        /// Getter for the "Moderately" toggle.
        /// </summary>
        public Toggle Moderately => moderately;

        /// <summary>
        /// Getter for the "Very" toggle.
        /// </summary>
        public Toggle Very => very;
    }

    /// <summary>
    /// Enum representing internal state.
    /// </summary>
    private enum SurveyState
    {
        setup,
        waiting,
        end,
        quit
    }

    /// <summary>
    /// The current internal state.
    /// </summary>
    [SerializeField] private SurveyState currentSurveyState;

    [SerializeField] private GameObject text;
    [SerializeField] private MatrixScale[] scales;

    /// <summary>
    /// Getter for survey scales.
    /// </summary>
    public MatrixScale[] Scales => scales;
    private bool changedToggleThisTick;

    [SerializeField] private Button finishedSurveyButton;
    private bool finishedSurvey;

    [SerializeField] private float textDistanceFromHead = 2.0f;
    [SerializeField] private float textHeight = 1.5f;

    private Transform centeredPlayer;

    [SerializeField] private Transform surveyPosition;

    private VRPointers vrPointers;

    [SerializeField] private readonly int endExperimentThreshold;


    public Action<bool> SetUpAction;

    /// <summary>
    /// Survey component initialization.
    /// </summary>
    private void Start()
    {
        finishedSurvey = false;
        text.SetActive(false);
        currentSurveyState = SurveyState.setup;

        foreach (MatrixScale scale in scales)
        {
            scale.NotAtAll.onValueChanged.AddListener((value) =>
            {
                if (!changedToggleThisTick)
                {
                    changedToggleThisTick = true;
                    scale.Toggled = value;
                    scale.Slightly.isOn = false;
                    scale.Moderately.isOn = false;
                    scale.Very.isOn = false;
                    finishedSurveyButton.interactable = value && CheckSurveyFilled();
                }
            });
            scale.Slightly.onValueChanged.AddListener((value) =>
            {
                if (!changedToggleThisTick)
                {
                    changedToggleThisTick = true;
                    scale.Toggled = value;
                    scale.NotAtAll.isOn = false;
                    scale.Moderately.isOn = false;
                    scale.Very.isOn = false;
                    finishedSurveyButton.interactable = value && CheckSurveyFilled();
                }
            });
            scale.Moderately.onValueChanged.AddListener((value) =>
            {
                if (!changedToggleThisTick)
                {
                    changedToggleThisTick = true;
                    scale.Toggled = value;
                    scale.NotAtAll.isOn = false;
                    scale.Slightly.isOn = false;
                    scale.Very.isOn = false;
                    finishedSurveyButton.interactable = value && CheckSurveyFilled();
                }
            });
            scale.Very.onValueChanged.AddListener((value) =>
            {
                if (!changedToggleThisTick)
                {
                    changedToggleThisTick = true;
                    scale.Toggled = value;
                    scale.NotAtAll.isOn = false;
                    scale.Slightly.isOn = false;
                    scale.Moderately.isOn = false;
                    finishedSurveyButton.interactable = value && CheckSurveyFilled();
                }
            });
        }
    }

    /// <summary>
    /// Set the player objects.
    /// </summary>
    /// <param name="centeredPlayer">The player's transform.</param>
    public void SetPlayerObjects(Transform centeredPlayer)
    {
        this.centeredPlayer = centeredPlayer;
        vrPointers = centeredPlayer.GetComponentInChildren<VRPointers>();
    }

    /// <summary>
    /// Main update loop with state machine.
    /// </summary>
    /// <returns>The next experiment state.</returns>
    public ExperimentState Tick()
    {
        switch (currentSurveyState)
        {
            case SurveyState.setup:
                currentSurveyState = SetUp();
                break;
            case SurveyState.waiting:
                currentSurveyState = Wait();
                changedToggleThisTick = false;
                break;
            case SurveyState.end:
                End();
                return ExperimentState.send; // order of events?
            case SurveyState.quit:
                return ExperimentState.end;
        }
        return ExperimentState.survey; // CHECK THIS STATEMENT IF ISSUES WITH SURVEY NOT EXECUTING
    }

    /// <summary>
    /// Teleports the player to the survey position.
    /// </summary>
    public void Teleport()
    {
        centeredPlayer.position = surveyPosition.position;
        centeredPlayer.rotation = surveyPosition.rotation;
    }

    /// <summary>
    /// Sets up the survey.
    /// </summary>
    /// <returns>The next survey state.</returns>
    private SurveyState SetUp()
    {
        SetUpAction?.Invoke(false);
        text.SetActive(true);
        finishedSurvey = false;

        ClearSurvey();
        vrPointers.enabled = true;

        Teleport();
        Vector3 position = centeredPlayer.position + centeredPlayer.forward * textDistanceFromHead;
        position.y += textHeight;
        text.transform.position = position;
        return SurveyState.waiting;
    }

    /// <summary>
    /// Waits for the player to finish the survey.
    /// </summary>
    /// <returns>The next survey state.</returns>
    private SurveyState Wait()
    {
        if (finishedSurvey)
        {
            return SurveyState.end;
        }
        else
        {
            return SurveyState.waiting;
        }
    }

    /// <summary>
    /// Ends the survey.
    /// </summary>
    private void End()
    {
        text.SetActive(false);

        vrPointers.enabled = false;

        currentSurveyState = SurveyState.setup;
    }

    /// <summary>
    /// Clears the survey.
    /// </summary>
    private void ClearSurvey()
    {
        finishedSurveyButton.interactable = false;
        foreach (MatrixScale scale in scales)
        {
            scale.Toggled = false;
            scale.NotAtAll.isOn = false;
            scale.Slightly.isOn = false;
            scale.Moderately.isOn = false;
            scale.Very.isOn = false;
        }
    }

    /// <summary>
    /// Checks if the survey has been filled.
    /// </summary>
    /// <returns>True if the survey has been filled, false otherwise.</returns>
    private bool CheckSurveyFilled()
    {
        foreach (MatrixScale scale in scales)
        {
            if (!scale.Toggled)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Finishes the survey section.
    /// Run by the finish survey button.
    /// </summary>
    public void FinishSurvey()
    {
        finishedSurvey = true;

        int totalScores = 0;
        foreach (MatrixScale scale in scales)
        {
            totalScores += SurveyResultToScore(scale);
        }

        // If the average score is greater than the threshold, end the experiment. The user is feeling too uncomfortable.
        if (totalScores / scales.Length >= endExperimentThreshold)
        {
            currentSurveyState = SurveyState.quit;
        }
    }

    /// <summary>
    /// Converts the survey result to a score.
    /// </summary>
    /// <param name="scale">The scale to convert.</param>
    /// <returns>The score.</returns>
    private static int SurveyResultToScore(MatrixScale scale)
    {
        if (scale.NotAtAll.isOn)
        {
            return 1;
        }
        else if (scale.Slightly.isOn)
        {
            return 2;
        }
        else if (scale.Moderately.isOn)
        {
            return 3;
        }
        else if (scale.Very.isOn)
        {
            return 4;
        }
        return 0;
    }

    /// <summary>
    /// Converts the survey result to a string.
    /// </summary>
    /// <param name="scale">The scale to convert.</param>
    /// <returns>The string.</returns>
    public static string SurveyResultToString(MatrixScale scale)
    {
        if (scale.NotAtAll.isOn)
        {
            return "not at all";
        }
        else if (scale.Slightly.isOn)
        {
            return "slightly";
        }
        else if (scale.Moderately.isOn)
        {
            return "moderately";
        }
        else if (scale.Very.isOn)
        {
            return "very";
        }
        return null;
    }
}
