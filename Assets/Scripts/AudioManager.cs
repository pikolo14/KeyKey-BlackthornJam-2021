using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System;

public class AudioManager : MonoBehaviour
{
    //Array de sonidos del juego
    public Sound[] sounds;
    //Referencia estática al audio manager para mayor comodidad
    public static AudioManager am;

    void Awake()
    {
        //Usamos el patrón singleton para que no se generen varios audio manager
        if(am == null)
        {
            am = this;
            //Mantenemos el sonido entre escenas
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            return;
        }

        foreach(Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    public void Play (string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Play();
    }

    public void Stop (string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Stop();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
