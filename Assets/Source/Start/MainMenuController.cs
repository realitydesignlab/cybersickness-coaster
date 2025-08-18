using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the starting menu.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameObject mainMenuGroup;
    [SerializeField] GameObject settingsGroup;

    [SerializeField] InputField snapRotationInput;

    public void Play()
    {
        if (XRSettings.isDeviceActive)
        {
            SceneManager.LoadScene("Demo_Scene");
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Settings()
    {
        settingsGroup.SetActive(true);
        mainMenuGroup.SetActive(false);
    }

    public void Back()
    {
        settingsGroup.SetActive(false);
        mainMenuGroup.SetActive(true);
    }
}
