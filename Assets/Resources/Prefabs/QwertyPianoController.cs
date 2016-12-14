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

    public AudioClip clip; // The sound, eg. piano, guitar
    public AudioMixerGroup masterMixer; // Where to find the mixer groups
    public Material material = null; // Material of the keys
    public int baseSemiShift = -12; // For shifting higher pitched wav files down
    
    int octave = 0; // Current octave out of 3

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

    public override void NoteEvent(bool isDown, KeyCode note, bool isSpectator)
    {
        if (!isSpectator)
        {
            GameObject obj;
            if (!objLookup.TryGetValue(note, out obj)) return;
            if (obj.GetComponent<KeyController>().note == null) return;
            var keyCtl = obj.GetComponent<KeyController>();
            keyCtl.octave = octave;
            keyCtl.UpdatePitch();
        }
        base.NoteEvent(isDown, note, isSpectator);
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
