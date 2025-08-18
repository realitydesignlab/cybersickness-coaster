using UnityEngine;

/// <summary>
/// Manages the VR controller interactions.
/// </summary>
public class VRControllers : MonoBehaviour
{
    /// <summary>
    /// The input actions for the controllers.
    /// </summary>
    private XRIDefaultInputActions inputActions;

    /// <summary>
    /// Getter for the left hand's interact press.
    /// </summary>
    public bool InteractPressLeft => inputActions.XRILeftHandInteraction.UIPress.WasPressedThisFrame();
    /// <summary>
    /// Getter for the right hand's interact press.
    /// </summary>
    public bool InteractPressRight => inputActions.XRIRightHandInteraction.UIPress.WasPressedThisFrame();

    /// <summary>
    /// Enables the input actions.
    /// </summary>
    private void OnEnable()
    {
        inputActions = new XRIDefaultInputActions();
        inputActions.Enable();
    }

    /// <summary>
    /// Disables the input actions.
    /// </summary>
    private void OnDisable()
    {
        inputActions.Disable();
    }
}
