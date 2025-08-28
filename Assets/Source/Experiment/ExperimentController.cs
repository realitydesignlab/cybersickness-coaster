using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// State machine component for managing the experiment.
/// </summary>
public class ExperimentController : MonoBehaviour
{
    /// <summary>
    /// Enum representing the current state of the experiment.
    /// </summary>
    private ExperimentState currentExperimentState = ExperimentState.start;

    /// <summary>
    /// Controller object for running the roller coaster test.
    /// </summary>
    [SerializeField] private RollerCoasterTest rollerCoasterTest;

    /// <summary>
    /// Controller object for measuring balance data.
    /// </summary>
    [SerializeField] private MeasureBalance balanceTest;

    /// <summary>
    /// Controller object for gathering survey data.
    /// </summary>
    [SerializeField] private Survey survey;

    /// <summary>
    /// The number of times to repeat the experiment, per session.
    /// </summary>
    [SerializeField] private int levels;
    private int currentLevel;

    [SerializeField] private GameObject environment;

    [SerializeField] private string startSceneName;
    private bool isLoadingScene;

    /// <summary>
    /// Survey URL to send data to.
    /// We used Qualtrics, but any API that can accept the data as URL parameters will work.
    /// </summary>
    [TextArea(1, 20)][SerializeField] private string surveyURL;

    /// <summary>
    /// Getter/setter property for the current state of the experiment.
    /// </summary>
    public ExperimentState CurrentDemoState { get => currentExperimentState; set => currentExperimentState = value; }

    /// <summary>
    /// Set the environment active or inactive.
    /// </summary>
    /// <param name="value">True to set the environment active, false to set it inactive.</param>
    public void SetEnvironmentActive(bool value)
    {
        environment.SetActive(value);
    }

    /// <summary>
    /// Experiment initialization.
    /// </summary>
    private async void Start()
    {
        Transform centeredPlayer = GameObject.Find("Player (Centered)")?.transform;
        Transform vrCamera = GameObject.Find("Camera")?.transform;
        
        // If cannot find the VR player, load the start scene
        if (!centeredPlayer)
        {
            Debug.LogError("VR is not active!");
            LoadScene(startSceneName);
            return;
        }
        
        Debug.Log("Loading experiment.");
        
        currentExperimentState = ExperimentState.start;
        currentLevel = 0;
        CybersickData.LoadData();
        
        rollerCoasterTest.SetPlayerObjects(centeredPlayer, vrCamera);
        balanceTest.SetPlayerObjects(centeredPlayer, vrCamera);
        survey.SetPlayerObjects(centeredPlayer);

        survey.Teleport();
        SetEnvironmentActive(false);

        rollerCoasterTest.SetUpAction += SetEnvironmentActive;
        balanceTest.SetUpAction += SetEnvironmentActive;
        survey.SetUpAction += SetEnvironmentActive;

        await Task.Delay(1);

        currentExperimentState = ExperimentState.survey;
        Debug.Log("Finished loading.");
        isLoadingScene = false;
    }

    /// <summary>
    /// Main update loop with state machine. Manages running the roller coaster test, balance measuring, survey, and sends data to Qualtrics.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            currentExperimentState = ExperimentState.survey;
            currentLevel = levels; //ensure it will quit after sending the data
            Debug.Log("Running time was " + rollerCoasterTest.GetRunTime());
        }

        // Quit the application when the escape key is pressed.
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
        }
        switch (currentExperimentState)
        {
            case ExperimentState.start:
                break;
            case ExperimentState.rollerCoasterTest:
                currentExperimentState = rollerCoasterTest.Tick();         
                break;
            case ExperimentState.balanceTest:
                currentExperimentState = balanceTest.Tick();
                break;
            case ExperimentState.survey:
                currentExperimentState = survey.Tick();
                break;
            case ExperimentState.send:
                if (currentLevel < levels)
                {
                    if (surveyURL.Length > 0)
                    {
                        StartCoroutine(SendLevelToQualtrics(currentLevel,
                            rollerCoasterTest.GetTargetsDestroyedRatio(), rollerCoasterTest.GetRunTime(),
                            balanceTest.GetMomentOfInertiaResults(), survey.Scales));
                    }
                    else
                    {
                        Debug.LogError("No survey URL set. Please add a survey URL if you wish to record data.");
                    }
                    
                    currentLevel++;
                    currentExperimentState = ExperimentState.rollerCoasterTest;
                }
                else
                {
                    currentExperimentState = ExperimentState.end;
                }
                break;
            case ExperimentState.end:
                if (!isLoadingScene)
                {
                    if (surveyURL.Length > 0)
                    {
                        StartCoroutine(SendLevelToQualtrics(currentLevel,
                            rollerCoasterTest.GetTargetsDestroyedRatio(), rollerCoasterTest.GetRunTime(),
                            balanceTest.GetMomentOfInertiaResults(), survey.Scales));
                    }
                    else
                    {
                        Debug.LogError("No survey URL set. Please add a survey URL if you wish to record data.");
                    }
                    
                    isLoadingScene = true;
                    LoadScene("Exit_Scene");
                }
                break;
        }
    }

    /// <summary>
    /// Send level data to the Qualtrics survey API coroutine.
    /// </summary>
    /// <param name="level">The level number.</param>
    /// <param name="balloonsPoppedRatio">The ratio of balloons popped to total balloons.</param>
    /// <param name="runTime">The time the user spent on the level.</param>
    /// <param name="moi">The moment of inertia of the user.</param>
    private IEnumerator SendLevelToQualtrics(int level, float balloonsPoppedRatio, float runTime, double moi, Survey.MatrixScale[] scales)
    {
        // Form containing data
        WWWForm form = new WWWForm();

        form.AddField("pid", CybersickData.Pid);
        form.AddField("session", CybersickData.Session);
        form.AddField("level", level);

        form.AddField("balloon popped ratio", balloonsPoppedRatio.ToString());
        form.AddField("moment of inertia", moi.ToString());

        foreach (Survey.MatrixScale scale in scales)
        {
            form.AddField(scale.Name, Survey.SurveyResultToString(scale));
        }

        form.AddField("running time", runTime.ToString());

        // Input the data into the site
        using (UnityWebRequest request = UnityWebRequest.Post(surveyURL, form))
        {
            yield return request.SendWebRequest();
        }
    }

    /// <summary>
    /// Load a scene asynchronously.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    private async void LoadScene(string sceneName)
    {
        AsyncOperation scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = false;

        do
        {
            await Task.Delay(100);
        } while (scene.progress < 0.9f);

        await Task.Delay(1000);

        scene.allowSceneActivation = true;
    }
}