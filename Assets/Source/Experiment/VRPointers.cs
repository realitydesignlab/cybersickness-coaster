using UnityEngine;

/// <summary>
/// Manages switching between the left and right VRPointers.
/// </summary>
public class VRPointers : MonoBehaviour
{
    private VRControllers vrControllers;
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;

    /// <summary>
    /// Start method.
    /// </summary>
    private void Start()
    {
        vrControllers = GetComponent<VRControllers>();
    }

    /// <summary>
    /// Sets the right hand to active by default.
    /// </summary>
    private void OnEnable()
    {
        leftHand.SetActive(false);
        rightHand.SetActive(true);
    }

    /// <summary>
    /// Disables the VRPointers.
    /// </summary>
    private void OnDisable()
    {
        leftHand.SetActive(false);
        rightHand.SetActive(false);
    }

    /// <summary>
    /// Switches between the left and right VRPointers based on which controler is being clicked.
    /// </summary>
    private void Update()
    {
        if (leftHand.activeSelf && vrControllers.InteractPressRight) // switch to right hand
        {
            leftHand.gameObject.SetActive(false);
            rightHand.gameObject.SetActive(true);
        }
        else if (rightHand.activeSelf && vrControllers.InteractPressLeft) // switch to left hand
        {
            leftHand.gameObject.SetActive(true);
            rightHand.gameObject.SetActive(false);
        }
    }
}
