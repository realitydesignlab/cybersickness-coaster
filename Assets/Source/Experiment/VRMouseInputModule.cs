using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles VR input emulating mouse input for the Unity UI
/// </summary>
public class VRMouseInputModule : StandaloneInputModule
{
    /// <summary>
    /// The pointer event data.
    /// </summary>
    public PointerEventData Data { private set; get; }

    /// <summary>
    /// The current object being interacted with.
    /// </summary>
    public GameObject CurrentObject { get; set; }

    /// <summary>
    /// Whether the interact button is being pressed.
    public bool InteractDown { get; set; }

    /// <summary>
    /// Whether the interact button is being released.
    /// </summary>
    public bool InteractUp { get; set; }

    /// <summary>
    /// Initialization.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        Data = new PointerEventData(eventSystem);
        CurrentObject = null;
        InteractUp = false;
        InteractUp = false;
    }

    /// <summary>
    /// Main update loop.
    /// </summary>
    public override void Process()
    {
        if (Input.GetMouseButton(0))
        {
            base.Process();
        }
        else
        {
            Data.Reset();

            if (CurrentObject)
            {
                eventSystem.RaycastAll(Data, m_RaycastResultCache);
                Data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);

                m_RaycastResultCache.Clear();

                HandlePointerExitAndEnter(Data, CurrentObject);

                if (InteractDown)
                {
                    ProcessPress(Data);
                }

                if (InteractUp)
                {
                    ProcessRelease(Data);
                }
            }
        }
    }

    /// <summary>
    /// Processes a press event on the UI.
    /// </summary>
    /// <param name="data">The pointer event data.</param>
    private void ProcessPress(PointerEventData data)
    {
        data.pointerPressRaycast = data.pointerCurrentRaycast;

        GameObject newPointerPress = ExecuteEvents.ExecuteHierarchy(CurrentObject, data, ExecuteEvents.pointerDownHandler);

        if (!newPointerPress)
        {
            newPointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentObject);
        }

        data.pressPosition = data.position;
        data.pointerPress = newPointerPress;
        data.rawPointerPress = CurrentObject;
    }

    /// <summary>
    /// Processes a release event on the UI.
    /// </summary>
    /// <param name="data">The pointer event data.</param>
    private void ProcessRelease(PointerEventData data)
    {
        ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);

        GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentObject);

        if (data.pointerPress == pointerUpHandler)
        {
            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerClickHandler);
        }

        eventSystem.SetSelectedGameObject(null);

        data.pressPosition = Vector2.zero;
        data.pointerPress = null;
        data.rawPointerPress = null;
    }
}
