using System.IO;
using UnityEngine;

/// <summary>
/// Load user data from a JSON file.
/// </summary>
[System.Serializable]
public class CybersickData
{
    private string pid;
    private int session;
    private static CybersickData instance;

    /// <summary>
    /// The participant ID.
    /// </summary>
    public static string Pid => instance.pid;

    /// <summary>
    /// The session number.
    /// </summary>
    public static int Session => instance.session;

    /// <summary>
    /// Load user data from a JSON file.
    /// </summary>
    public static void LoadData()
    {
        string path = Application.dataPath + "/cybersickness.json";

        if (File.Exists(path))
        {
            instance = JsonUtility.FromJson<CybersickData>(File.ReadAllText(path));
        }
        else
        {
            Debug.LogError($"Could not find save file. Should be in path {Application.dataPath}.");
        }
    }
}
