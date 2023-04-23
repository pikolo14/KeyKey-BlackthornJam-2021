using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;

    [Range(0f,1f)]
    public float volume = 1;
    [Range(.1f,3f)]
    public float  pitch = 1;
    public AudioClip clip;
    public bool loop;

    [HideInInspector]
    public AudioSource source;    
}
