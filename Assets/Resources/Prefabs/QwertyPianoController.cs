using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System;

// Implements a piano on top of qwerty keyboard
public sealed class QwertyPianoController : InstrumentController<int>
{
    // Which keys are playable
    public static string usedKeys = "zxcvbnm,./asdfghjkl;wetyuop2356790";
    // Which note is being played
    public static byte[] semitones = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 1, 3, 6, 8, 10, 13, 15, 1, 3, 6, 8, 10, 13, 15 };
    public static string isBlack = "0000000000000000000011111111111111";

    // Keyboard letter to note
    static Dictionary<char, byte> semitoneLookup = new Dictionary<char, byte>();
    static Dictionary<char, bool> isWhiteLookup = new Dictionary<char, bool>();
    static QwertyPianoController()
    {
        Debug.Assert(usedKeys.Length == semitones.Length && usedKeys.Length == isBlack.Length);
        for (byte i = 0; i < usedKeys.Length; ++i)
        {
            semitoneLookup[usedKeys[i]] = semitones[i];
            isWhiteLookup[usedKeys[i]] = isBlack[i] == '0';
        }
    }

    // Number of groups taken by all prefabs, to see if we've ran out
    static int numGroupsTaken = 0;

    public AudioClip clip; // The sound, eg. piano, guitar
    public AudioMixerGroup masterMixer; // Where to find the mixer groups
    public string groupPrefix = "G"; // groupPrefix + {0, 1, 2, ...} = group name
    public int numGroups; // Total # of groups that exist, expect the same for each prefab
    public string pitchPrefix = "p"; // The prefix given to the pitch parameter of each mixer group
    public Material material = null; // Material of the keys
    public int baseSemiShift = -12; // For shifting higher pitched wav files down
    
    List<AudioMixerGroup> groups = new List<AudioMixerGroup>(); // Max 3 mixers, for 3 octave support
    int octave = 0; // Current octave out of 3

    // Save the next available mixer for use in this instrument
    int TakeMixer()
    {
        Debug.Assert(numGroupsTaken < numGroups);
        var number = numGroupsTaken;
        var group = masterMixer.audioMixer.FindMatchingGroups("Master/" + groupPrefix + number);
        Debug.Assert(group != null && group.Length == 1);
        groups.Add(group[0]);
        numGroupsTaken++;
        return number;
    }

    protected override void Start()
    {
        base.Start();
        for (int i = 0; i < 3; ++i) // Take 3 mixers for 3 octaves
        {
            var number = TakeMixer();
            groups[groups.Count - 1].audioMixer.SetFloat(pitchPrefix + number, Mathf.Pow(2.0f, (float)(i - 1)));
        }
    }

    public override void SetParameters(int octave)
    {
        this.octave = Mathf.Clamp(octave, -1, 1);
    }

    public override Note GetNote(char c)
    {
        byte semitone;
        if (semitoneLookup.TryGetValue(c, out semitone))
        {
            Note note = new Note();
            note.clip = clip;
            note.color = isWhiteLookup[c] ? Color.white : Color.Lerp(Color.black, Color.grey, 0.8f);
            note.semitone = semitone + baseSemiShift;
            note.material = material;
            return note;
        }
        else
        {
            return null;
        }
    }

    public override void NoteEvent(bool isDown, KeyCode note)
    {
        GameObject obj;
        if (!objLookup.TryGetValue(note, out obj)) return;
        if (obj.GetComponent<KeyController>().note == null) return;
        var keyCtl = obj.GetComponent<KeyController>();
        keyCtl.source.outputAudioMixerGroup = groups[octave + 1];
        keyCtl.octave = octave;
        base.NoteEvent(isDown, note);
    }

    protected override void Update()
    {
        base.Update();
        if (networkRd == null || !networkRd.isLocal) return;
        if (Input.GetKeyDown(KeyCode.LeftBracket))
            networkRd.SetParameters(octave - 1);
        else if (Input.GetKeyDown(KeyCode.RightBracket))
            networkRd.SetParameters(octave + 1);
    }
}
