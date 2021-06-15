using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalManager : MonoBehaviour
{
    public static GlobalManager instance;

    SoundManager sm;
    SongLibraryManager slm;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        sm = GetComponentInChildren<SoundManager>();
        slm = GetComponentInChildren<SongLibraryManager>();
    }

    void Start()
    {
        Song s = slm.FindSong("test track");
        FindObjectOfType<SongManager>().LoadSong(s.file, s.beatsPerBar, s.tempo);
    }
}