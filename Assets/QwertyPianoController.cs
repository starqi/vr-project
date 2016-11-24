using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

// Turns the QWERTY keyboard into a piano by overlaying it with AR key objects
public class QwertyPianoController : MonoBehaviour
{
    public AudioClip clip;
    public AudioMixerGroup mixerGroup;
    public Text text; // Text to display information to user
    public GameObject finger; // The finger used to find the positions of the keys

    static KeyCode setupKey = KeyCode.F1;
    // TODO: Implement for non-alphanumeric
    static string semitones = "q2w3er5t6y7ui9o0p";
    static string isWhite = "10101101010110101";

    AudioSource[] sources;
    List<GameObject> keys = new List<GameObject>(); // The 3D key objects
    bool isSetup = false;

    void Start()
    {
        // Make a sound source and a null object reference for each key
        for (int i = 0; i < semitones.Length; ++i)
        {
            gameObject.AddComponent<AudioSource>();
            keys.Add(null);
        }

        sources = GetComponents<AudioSource>();

        // Setup each key
        for (int i = 0; i < semitones.Length; ++i)
        {
            sources[i].pitch = Mathf.Pow(2.0f, i / 12.0f);
            sources[i].playOnAwake = false; // This is by default true
            sources[i].clip = clip;
            sources[i].outputAudioMixerGroup = mixerGroup;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(setupKey))
        {
            isSetup = !isSetup;
            if (isSetup)
            {
                text.text = "Setup mode";
            }
            else
            {
                text.text = "";
            }
        }
        

        for (int i = 0; i < semitones.Length; ++i)
        {
            if (Input.GetKeyDown(semitones[i].ToString()))
            {
                if (isSetup)
                {
                    sources[i].Play();
                    MakeKey(i);
                }
                else
                {
                    sources[i].Play();
                }

                // Animate the key press
                if (keys[i] != null)
                {
                    keys[i].transform.Translate(new Vector3(0.0f, -0.05f, 0.0f), Space.World);
                }
            }
            else if (Input.GetKeyUp(semitones[i].ToString()))
            {
                // Animate the key release
                if (keys[i] != null)
                {
                    keys[i].transform.Translate(new Vector3(0.0f, 0.05f, 0.0f), Space.World);
                }
            }
        }
    }

    void MakeKey(int semitone)
    {
        // Replace the existing key

        if (keys[semitone] != null)
        {
            GameObject.Destroy(keys[semitone]);
        }

        // Make the object

        var keyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        keyObj.name = "key" + semitone;
        keyObj.transform.position = finger.transform.position;
        keyObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        keys[semitone] = keyObj;

        if (isWhite[semitone] == '1')
        {
            keyObj.GetComponent<MeshRenderer>().material.color = Color.white;
        }
        else
        {
            keyObj.GetComponent<MeshRenderer>().material.color = Color.black;
        }
    }
}
