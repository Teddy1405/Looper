using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public struct Note
{
    public int lane;
    public float beatPos;
    public float start;
    public float stop;
}

// TODO: Save tracks to Resources

public class TrackParser : MonoBehaviour
{
    string tracksPath;

    BinaryFormatter bf = new BinaryFormatter();

    void Awake()
    {
        tracksPath = $"{Application.dataPath}/Tracks";
    }

    public void SaveTrack(Note[] notes, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogWarning("Track name cannot be blank");
            return;
        }

        string filePath = $"{tracksPath}/{fileName}.bytes";

        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        {
            bf.Serialize(fs, notes);
            Debug.Log($"{fileName}.bytes saved");
        }
    }

    public Note[] LoadTrack(string fileName)
    {
        string filePath = $"{tracksPath}/{fileName}.bytes";

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"File {fileName} not found");
            return null;
        }

        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        {
            Note[] notes = bf.Deserialize(fs) as Note[];
            Debug.Log($"{fileName}.bytes loaded");
            return notes;
        }
    }

    public Note[] ParseTrack(TextAsset trackFile)
    {
        using (MemoryStream ms = new MemoryStream(trackFile.bytes))
        {
            Note[] notes = bf.Deserialize(ms) as Note[];
            return notes;
        }
    }
}
