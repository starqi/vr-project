using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Text;
using System.Linq;

// Information about a single note
public class Note
{
    public AudioClip clip { get; set; }
    public AudioMixerGroup mixerGroup { get; set; }
    public int semitone { get; set; }
    public Color color { get; set; }
}

// The script for a single key game object
public class KeyController : MonoBehaviour
{
    public List<GameObject> dupes; // List of keys with same semitone as this one

    Material material;
    Color savedColor;
    AudioSource source; // Each key has its own audio source created by InstrumentController
    // Null means it is an unuseable key

    void Start()
    {
        source = GetComponent<AudioSource>();
        material = GetComponent<MeshRenderer>().material;
    }

    public void Play()
    {
        if (source == null) return;
        // Play sound and animate every duplicate key
        source.Play();
        savedColor = material.color;
        material.color = Color.red;
        dupes.ForEach(a => a.transform.Translate(0.0f, -InstrumentController.downShift, 0.0f, Space.Self));
    }

    public void Release()
    {
        if (source == null) return;
        material.color = savedColor;
        // Animate every duplicate key
        dupes.ForEach(a => a.transform.Translate(0.0f, InstrumentController.downShift, 0.0f, Space.Self));
    }
}

// Template for different kinds of instruments
public abstract class InstrumentController : MonoBehaviour
{
    public static int keyRows = 4, keyColumns = 10;
    public static string order = "1234567890qwertyuiopasdfghjkl;zxcvbnm,./";
    public static byte[] ascii = Encoding.ASCII.GetBytes(order);
    public static float keyWidth = 0.05f, rowShift = 0.018f, downShift = 0.01f, fillWidth = keyWidth + 0.002f, inactiveDrop = downShift * 3.0f;
    public static Color noNoteColor = Color.grey;

    // The instrument specific implementation
    public abstract Note GetNote(char c);

    Dictionary<KeyCode, Note> noteLookup = new Dictionary<KeyCode, Note>();
    Dictionary<KeyCode, GameObject> objLookup = new Dictionary<KeyCode, GameObject>();
    Dictionary<int, List<GameObject>> dupeLookup = new Dictionary<int, List<GameObject>>();

    void Start()
    {
        Debug.Assert(keyRows * keyColumns == order.Length);

        // Use the custom instrument implementation to get which sounds to play on each key
        for (int i = 0; i < order.Length; ++i)
        {
            noteLookup[(KeyCode)ascii[i]] = GetNote(order[i]);
        }
        
        // Create a virtual QWERTY keyboard
        int counter = 0;
        // For each key
        for (int i = 0; i < keyRows; ++i)
        {
            for (int j = 0; j < keyColumns; ++j)
            {
                KeyCode keyCode = (KeyCode)ascii[counter];

                // Find the note properties
                Note note = null;
                noteLookup.TryGetValue(keyCode, out note);

                // Make the 3D cube
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = gameObject.transform;
                cube.transform.localScale = new Vector3(keyWidth, keyWidth, keyWidth);
                cube.transform.localPosition = new Vector3(i * rowShift + j * fillWidth, note == null ? -inactiveDrop : 0.0f, -i * fillWidth);
                cube.GetComponent<MeshRenderer>().material.color = note == null ? noNoteColor : note.color;
                objLookup[keyCode] = cube;

                // Add a controller
                cube.AddComponent<KeyController>();

                if (note != null)
                {
                    // Group duplicates
                    List<GameObject> arr;
                    if (!dupeLookup.TryGetValue(note.semitone, out arr))
                    {
                        arr = dupeLookup[note.semitone] = new List<GameObject>();
                    }
                    arr.Add(cube);
                    var keyCtr = cube.GetComponent<KeyController>();
                    keyCtr.dupes = arr;

                    // Add an audio source, copying the note properties
                    cube.AddComponent<AudioSource>();
                    var keySrc = cube.GetComponent<AudioSource>();
                    keySrc.pitch = Mathf.Pow(2.0f, note.semitone / 12.0f);
                    keySrc.playOnAwake = false; // This is by default true
                    keySrc.clip = note.clip;
                    keySrc.outputAudioMixerGroup = note.mixerGroup;
                }

                counter++;
            }
        }
    }

    void Update()
    {
        GameObject cube;

        for (int i = 0; i < ascii.Length; ++i)
        {
            KeyCode kc = (KeyCode)ascii[i];
            if (Input.GetKeyDown(kc))
            {
                if (objLookup.TryGetValue(kc, out cube))
                {
                    var ctl = cube.GetComponent<KeyController>();
                    ctl.Play();
                }
            }
            else if (Input.GetKeyUp(kc))
            {
                if (objLookup.TryGetValue(kc, out cube))
                {
                    var ctl = cube.GetComponent<KeyController>();
                    ctl.Release();
                }
            }
        }
    }
}
