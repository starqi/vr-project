using UnityEngine;
using System.Collections;

public class VizController : MonoBehaviour {

    public static Color[] intervalColors = {
        Color.white, // Octave
        Color.red, Color.Lerp(Color.red, Color.black, 0.5f), // Seconds
        Color.green, Color.Lerp(Color.green, Color.blue, 0.5f), // Thirds
        Color.Lerp(Color.yellow, Color.white, 0.8f), Color.Lerp(Color.blue, Color.white, 0.8f) // Fourth, tritone
    };

    public static Color semiToColor(int semitones)
    {
        int n = Mathf.Abs(semitones) % 12; // 0-11
        if (n > 6) // 0-6 is ascending, so up to and including the tritone
            n = 12 - n; // 7-11 comes back down descending
        return intervalColors[n];
    }

    public GameObject light;

    void Start () {

        // INCOMPLETE
        // Use directional light to light up the WHOLE scene additively, and only default spotlight when no keys are pressed

        Vector3 sum = Vector3.zero;
        foreach (Color color in intervalColors)
        {
            sum += new Vector3(color.r, color.g, color.b);
        }
        sum /= intervalColors.Length;
        light.GetComponent<Light>().color = new Color(sum.x, sum.y, sum.z);
    }
	
	void Update () {
	    
	}
}
