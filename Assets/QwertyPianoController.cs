
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

// TODO: Apparently, have to use multiple sources same WAV file...

public class QwertyPianoController : MonoBehaviour {

    public AudioClip clip;
    public AudioMixerGroup mixerGroup;

    static string semitones = "q2w3er5t6y7ui9o0p";
    AudioSource[] sources;

    void Start()
    {
        for (int i = 0; i < semitones.Length; ++i)
        {
            gameObject.AddComponent<AudioSource>();
        }

        sources = GetComponents<AudioSource>();

        for (int i = 0; i < semitones.Length; ++i)
        {
            sources[i].pitch = Mathf.Pow(2.0f, i / 12.0f);
            sources[i].playOnAwake = false;
            sources[i].clip = clip;
            sources[i].outputAudioMixerGroup = mixerGroup;
        }
    }

	void Update ()
    {
	    for (int i = 0; i < semitones.Length; ++i)
        {
            if (Input.GetKeyDown(semitones[i].ToString()))
            {
                sources[i].Play();
            }
        }
	}
}
