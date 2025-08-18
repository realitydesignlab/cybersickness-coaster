using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The final scene that the user sees before the experiment ends.
/// </summary>
public class ExitScene : MonoBehaviour
{
    [SerializeField] private float textDistanceFromHead = 2.0f;
    [SerializeField] private float textHeight = 1.5f;

    [SerializeField] private Transform exitText;

    /// <summary>
    /// Setup. Teleports the player to the exit position.
    /// </summary>
    private void Start()
    {
        Transform centeredPlayer = GameObject.Find("Player (Centered)")?.transform;
        if (centeredPlayer)
        {
            centeredPlayer.position = Vector3.zero;
            centeredPlayer.rotation = Quaternion.identity;

            Vector3 position = centeredPlayer.position + centeredPlayer.forward * textDistanceFromHead;
            position.y += textHeight;
            exitText.transform.position = position;
        }
    }

    /// <summary>
    /// Quits the application if the escape key is pressed.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// Quits the application.
    /// This is used for the exit button in the scene.
    /// </summary>
    public void Exit()
    {
        Application.Quit();
    }
}
