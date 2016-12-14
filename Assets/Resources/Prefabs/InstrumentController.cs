using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;

// Information about a single note
public class Note
{
    public AudioClip clip { get; set; }
    public int semitone { get; set; }
    public Color color { get; set; }
    public Material material { get; set; }
}

// The script for a single key game object
public class KeyController : MonoBehaviour
{
    public List<GameObject> dupes; // List of keys with same semitone as this one
    public Note note; // The original note
    public AudioSource source; // Each key has its own audio source created by InstrumentController, null means it is an unuseable key
    public int octave;
    public Vector3 upPos, downPos;

    public void UpdatePitch()
    {
        source.pitch = Mathf.Pow(2.0f, (note.semitone + octave * 12.0f) / 12.0f);
    }

    public void Play(bool spectating = false)
    {
        Debug.Assert(note != null);
        
        if (spectating)
        {
            // Don't play sound because it's already being played by another instrument
            dupes.ForEach(a => a.GetComponent<MeshRenderer>().material.color = Color.red);
        }
        else
        {
            // Play sound and animate every duplicate key
            source.Play();
            dupes.ForEach(a => a.transform.position = a.GetComponent<KeyController>().downPos);
        }
    }

    public void Release(bool highlight = false)
    {
        Debug.Assert(note != null);
        if (highlight)
        {
            dupes.ForEach(a => a.GetComponent<MeshRenderer>().material.color = note.color);
        }
        else
        {
            dupes.ForEach(a => a.transform.position = a.GetComponent<KeyController>().upPos);
        }
    }
}

// Template for different kinds of qwerty instruments
public abstract class InstrumentController<P> : MonoBehaviour, NetworkSupport<KeyCode, P>
{
    public static int keyRows = 4, keyColumns = 10;
    public static string order = "1234567890qwertyuiopasdfghjkl;zxcvbnm,./";
    public static byte[] ascii = Encoding.ASCII.GetBytes(order);
    public static float keyWidth = 0.25f, rowShift = 0.115f, downShift = 0.05f, fillWidth = keyWidth + 0.01f, inactiveDrop = downShift;
    public static Color noNoteColor = new Color(0.05f, 0.01f, 0.01f);
    public static byte numLastNotes = 5;

    // The instrument specific implementation
    public abstract Note GetNote(char c);
    public abstract void SetParameters(P parameters);

    public List<Note> recentNotes { get; private set; }
    public Action<Note, GameObject> noteDown; // Subscribe to get notifications

    protected Dictionary<KeyCode, Note> noteLookup = new Dictionary<KeyCode, Note>();
    protected Dictionary<KeyCode, GameObject> objLookup = new Dictionary<KeyCode, GameObject>();
    protected Dictionary<int, List<GameObject>> dupeLookup = new Dictionary<int, List<GameObject>>();

    // Network
    protected NetworkRedirector<KeyCode, P> networkRd = null;
    public bool isNetworkSetup() { return networkRd != null; }

    public void SetupNetwork(int ownerID)
    {
        if (isNetworkSetup()) throw new Exception("Network already set up");
        networkRd = new NetworkRedirector<KeyCode, P>(this, ownerID);
    }

    public void ChangeSpec(int? id)
    {
        networkRd.specID = id;
    }

    protected virtual void Start()
    {
        recentNotes = new List<Note>();
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
                var meshRend = cube.GetComponent<MeshRenderer>();
                if (note != null && note.material != null) meshRend.material = note.material;
                meshRend.material.color = note == null ? noNoteColor : note.color;
                objLookup[keyCode] = cube;

                // Add a controller
                var keyCtr = cube.AddComponent<KeyController>();

                if (note != null)
                {
                    // Group duplicates
                    List<GameObject> arr;
                    if (!dupeLookup.TryGetValue(note.semitone, out arr))
                    {
                        arr = dupeLookup[note.semitone] = new List<GameObject>();
                    }
                    arr.Add(cube);
                    keyCtr.dupes = arr;
                    keyCtr.note = note;
                    keyCtr.upPos = cube.transform.position;
                    keyCtr.downPos = keyCtr.upPos + Vector3.down * downShift;

                    // Add an audio source, copying the note properties
                    var keySrc = cube.AddComponent<AudioSource>();
                    keySrc.playOnAwake = false; // This is by default true
                    keySrc.clip = note.clip;
                    keySrc.spatialize = true;
                    keySrc.spatialBlend = 1.0f;

                    keyCtr.source = keySrc;
                    keyCtr.UpdatePitch();
                }

                counter++;
            }
        }
    }

    public virtual void NoteEvent(bool isDown, KeyCode note, bool isSpectate)
    {
        GameObject cube;

        if (isDown)
        {   
            if (objLookup.TryGetValue(note, out cube)) // Find the key object
            {
                var ctl = cube.GetComponent<KeyController>();   
                if (ctl.note != null) // If it is a playable key
                {
                    // Play it and keep the last few played notes
                    ctl.Play(isSpectate);
                    if (!isSpectate)
                    {
                        if (recentNotes.Count >= numLastNotes) recentNotes.RemoveAt(0);
                        recentNotes.Add(ctl.note);
                        noteDown(ctl.note, cube);
                    }
                }
            }
        }
        else
        {   
            if (objLookup.TryGetValue(note, out cube)) // Find the key object
            {
                // Release the note
                var ctl = cube.GetComponent<KeyController>();
                if (ctl.note != null)
                {
                    ctl.Release(isSpectate);
                }
            }
        }
    }

    protected virtual void Update()
    {
        // Not set up or is a network instrument => Cannot play it with keys
        if (networkRd == null || !networkRd.isLocal) return;

        for (int i = 0; i < ascii.Length; ++i)
        {
            KeyCode keyCode = (KeyCode)ascii[i];
            if (Input.GetKeyDown(keyCode))
                networkRd.Play(true, keyCode);
            else if (Input.GetKeyUp(keyCode))
                networkRd.Play(false, keyCode);
        }
    }
}
