using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Simple Audio Player and Subtitle Component.
/// </summary>
public class AudioAndSubtitles : MonoBehaviour
{
    /// <summary>
    /// Object for containing the text and time queue for a subtitle.
    /// </summary>
    [Serializable]
    public class Subtitle
    {
        [TextArea(1, 10)] [SerializeField] private string text;
        [SerializeField] private float endTime;

        /// <summary>
        /// Getter/setter for the text for the subtitle.
        /// </summary>
        public string Text { get => text; set => text = value; }
        
        /// <summary>
        /// Getter/setter for the time, relative to the start of the audio file, to stop displaying the subtitle.
        /// </summary>
        public float EndTime { get => endTime; set => endTime = value; }
    }

    [SerializeField] private AudioClip audioClip;
    [Range(0f, 1f)] [SerializeField] private float volume;
    [SerializeField] private AudioSource source;

    [SerializeField] private Subtitle[] subtitles;
    [SerializeField] private TextMeshProUGUI text;

    [SerializeField] private bool isSkippable;
    private bool continued;
    [SerializeField] private bool isReplayable;
    private bool replayed;

    public Action OnContinue;

    /// <summary>
    /// Prompts to skip the audio clip playback while the PlayAudio coroutine is running.
    /// Note: The clip can only be skipped if isSkippable is true.
    /// </summary>
    public void PromptContinue()
    {
        continued = true;
    }

    /// <summary>
    /// Prompts replay of the audio clip while the PlayAudio coroutine is running. Note: The clip can only be replayed if isReplayable is true.
    /// </summary>
    public void PromptReplay()
    {
        replayed = true;
    }
    
    /// <summary>
    /// Begin playing the audio.
    /// </summary>
    public void Play()
    {
        StartCoroutine(PlayAudio());
    }

    /// <summary>
    /// Coroutine to begin playing the audio and managing showing subtitles.
    /// </summary>
    /// <returns>Coroutine</returns>
    public IEnumerator PlayAudio()
    {
        source.clip = audioClip;
        source.volume = volume;
        if (audioClip)
        {
            source?.Play();
        }

        float timer = 0f;
        for (int i = 0; i < subtitles.Length; i++)
        {
            text.text = subtitles[i].Text;

            do
            {
                // Don't need to check every frame, just picked an arbitrary time.
                yield return new WaitForSeconds(0.05f);
                timer += 0.05f;

                if (continued && isSkippable)
                {
                    continued = false;
                    Continue();
                    OnContinue?.Invoke();
                }
                else if (replayed && isReplayable) // replay the audio and subtitles
                {
                    replayed = false;
                    source?.Stop();
                    Play();
                    yield break;
                }
            } while (timer < subtitles[i].EndTime);
        }

        continued = false;
        replayed = false;
        do 
        {
            yield return new WaitForSeconds(0.1f);
            if (replayed)
            {
                replayed = false;
                Play();
                yield break;
            }
        } while (!continued);
        
        Continue();
        OnContinue?.Invoke();
    }

    /// <summary>
    /// Skips the audio playback.
    /// </summary>
    private void Continue()
    {
        text.gameObject.SetActive(false);
        source?.Stop();
    }
}
