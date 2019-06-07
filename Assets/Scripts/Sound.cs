﻿using UnityEngine.Audio;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume;
    [Range(.1f, 3)]
    public float pitch;
    [Range(0f, 1)]
    public float dimension;


    public bool playOnAwake;

    public bool loop;

    [HideInInspector]
    public AudioSource source;
}
