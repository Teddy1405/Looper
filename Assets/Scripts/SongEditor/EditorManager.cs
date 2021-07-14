using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ref<T> where T : struct
{
    public T Value { get; set; }
}

public class EditorManager : MonoBehaviour
{
    const int PHRASE_SCROLL_AMOUNT = 4;
    const float NEW_NOTE_BUFFER = 1f;
    const int NEW_NOTE_BARS = 4;

    Song currentSong;
    string currentTrackName;
    public static int currentBar;

    List<Ref<Note>> notes = new List<Ref<Note>>();

    [Serializable]
    public struct NoteButton
    {
        public int noteVal;
        public Button noteButton;
    }
    NoteButton currentNoteVal;
    bool usingTriplet;

    struct SelectedNote
    {
        public Ref<Note> noteRef;
        public EditNoteHandler enh;
    }
    SelectedNote selectedNote;
    List<EditNoteHandler> barNotes = new List<EditNoteHandler>();
    
    LoopDisplayHandler ldm;
    SongDisplayManager sdm;

    InputField offsetField;
    Slider offsetSlider;
    NoteInfoHandler nih;
    List<PlaceholderHandler> placeholders = new List<PlaceholderHandler>();

    public GameObject[] placeholderPrefabs = new GameObject[LoopDisplayHandler.LANE_COUNT];
    public GameObject[] editNotePrefabs = new GameObject[LoopDisplayHandler.LANE_COUNT];
    public NoteButton[] noteValButtons;

    void Awake()
    {
        ldm = FindObjectOfType<LoopDisplayHandler>();
        sdm = FindObjectOfType<SongDisplayManager>();
        offsetField = GameObject.Find("Offset Field").transform.GetComponent<InputField>();
        offsetSlider = GameObject.Find("Offset Slider").transform.GetComponent<Slider>();
        nih = FindObjectOfType<NoteInfoHandler>();
    }

    void Start()
    {
        ldm.HideMetronome();
        nih.Hide();
        SelectNoteVal(4);
    }

    int CurrentBar
    {
        get => currentBar;
        set
        {
            if (value < 0) currentBar = 0;
            else currentBar = value;

            if (selectedNote.noteRef != null &&
                !NoteInBar(selectedNote.noteRef.Value.start,
                           selectedNote.noteRef.Value.stop,
                           selectedNote.noteRef.Value.beatPos))
                DeselectNote();

            SpawnEditNotes();
            sdm.DisplayBar(notes);
        }
    }

    public void LoadSong(Song s)
    {
        currentSong = s;
        currentSong.beatsPerBar = s.beatsPerBar;
        currentSong.beatUnit = s.beatUnit;
        currentTrackName = s.TrackName;
        
        foreach (Note n in s.Track)
        {
            Ref<Note> newNoteRef = new Ref<Note>();
            newNoteRef.Value = n;
            notes.Add(newNoteRef);
        }
        
        ldm.Initialize(s.beatsPerBar);
        sdm.Initialize(s);

        GameObject.Find("Title Field").transform.GetComponent<InputField>().text = s.title;
        offsetField.text = s.offset.ToString();
        offsetSlider.value = s.offset;
        GameObject.Find("Track Name Field").transform.GetComponent<InputField>().text = currentTrackName;

        CurrentBar = 0;
    }

    public void SaveSong()
    {
        Note[] track = new Note[notes.Count];

        for (int i = 0; i < notes.Count; i++)
            track[i] = notes[i].Value;

        GlobalManager.instance.SaveTrack(track, currentTrackName);
    }

    public void DisplayNextBar()
    {
        CurrentBar++;
    }

    public void DisplayPrevBar()
    {
        CurrentBar--;
    }

    public void DisplayNextPhrase()
    {
        CurrentBar += PHRASE_SCROLL_AMOUNT;
    }

    public void DisplayPrevPhrase()
    {
        CurrentBar -= PHRASE_SCROLL_AMOUNT; 
    }

    // TODO: Set beatsPerBar, beatUnit, tempo

    public void SetTitle(string title)
    {
        currentSong.title = title;
    }

    public void SetOffset(string textOffset)
    {
        float newOffset;
        if (float.TryParse(textOffset, out newOffset))
        {
            currentSong.offset = newOffset;
            offsetSlider.value = newOffset;
            sdm.SetOffset(newOffset);
        }
    }

    public void SetOffset(float newOffset)
    {
        currentSong.offset = newOffset;
        offsetField.text = newOffset.ToString();
        sdm.SetOffset(newOffset);
    }

    public void SetTrackName(string trackName)
    {
        currentTrackName = trackName;
    }

    bool NoteInBar(float start, float stop, float beatPos)
    {
        float barBeat = currentBar * currentSong.beatsPerBar;
        float rangeStart = start - barBeat;
        float rangeEnd = stop - barBeat;
        return rangeStart <= beatPos && beatPos <= rangeEnd;
    }

    void SpawnEditNotes()
    {
        for (int i = barNotes.Count - 1; i >= 0; i--)
        {
            Destroy(barNotes[i].gameObject);
            barNotes.RemoveAt(i);
        }

        foreach (Ref<Note> n in notes)
        {
            if (NoteInBar(n.Value.start, n.Value.stop, n.Value.beatPos))
            {
                GameObject newEditNote = Instantiate(editNotePrefabs[n.Value.lane]);
                newEditNote.transform.position = ldm.CalcNotePosition(n.Value.lane, n.Value.beatPos);
                newEditNote.transform.localScale = ldm.CalcNoteScale();

                EditNoteHandler enh = newEditNote.GetComponent<EditNoteHandler>();
                enh.em = this;
                enh.info = n;

                barNotes.Add(enh);
            }
        }
    }

    void UpdateNoteDisplay()
    {
        SpawnEditNotes();
        sdm.SpawnNoteLines(notes);
    }

    // TODO: Try to save when switch note

    public void SelectNote(EditNoteHandler enh)
    {
        if (selectedNote.enh != null) selectedNote.enh.Deselect();
        enh.Select();
        selectedNote.noteRef = enh.info;
        selectedNote.enh = enh;

        nih.SetStart(enh.info.Value.start);
        nih.SetStop(enh.info.Value.stop);

        nih.Show();
    }

    public void DeselectNote()
    {
        nih.Hide();

        if (selectedNote.enh != null) selectedNote.enh.Deselect();

        selectedNote.noteRef = null;
        selectedNote.enh = null;
    }

    public void AddNote(int lane, float beatPos)
    {
        foreach (Ref<Note> n in notes)
        {
            if (lane == n.Value.lane &&
                Mathf.Approximately(beatPos, n.Value.beatPos) &&
                NoteInBar(n.Value.start, n.Value.stop, n.Value.beatPos))
                return;
        }

        Note newNote = new Note();
        newNote.lane = lane;
        newNote.beatPos = beatPos;
        float barBeat = currentBar * currentSong.beatsPerBar;
        float noteBeat = barBeat + beatPos;
        newNote.start = barBeat;
        newNote.stop = noteBeat;

        Ref<Note> noteRef = new Ref<Note>();
        noteRef.Value = newNote;

        notes.Add(noteRef);

        UpdateNoteDisplay();
        SelectNote(barNotes.Find(enh => enh.info == noteRef));
    }

    bool BeatPosInRange(float start, float end, float beatPos)
    {
        float offset = (start + currentSong.beatsPerBar) % currentSong.beatsPerBar;
        float adjustedBeatPos = (beatPos + currentSong.beatsPerBar - offset) % currentSong.beatsPerBar;
        float rangeStart = 0f; // NOTE: start - start
        float rangeEnd = end - start;
        return rangeStart <= adjustedBeatPos && adjustedBeatPos <= rangeEnd;
    }

    bool StartValid(Note n, float start)
    {
        float firstPreZeroBeat = n.beatPos - currentSong.beatsPerBar;
        return start > firstPreZeroBeat && start <= n.stop && BeatPosInRange(start, n.stop, n.beatPos);
    }

    // TODO: Prevent note overlap (not same lane, beatPos, and range)

    public void SetSelectedNoteStart(string startText)
    {
        float start;
        if (float.TryParse(startText, out start))
        {
            Note info = selectedNote.noteRef.Value;
            
            if (!StartValid(info, start))
            {
                nih.SetStart(info.start);
                return;
            }

            info.start = start;
            selectedNote.noteRef.Value = info;

            if (!NoteInBar(info.start, info.stop, info.beatPos))
                DeselectNote();

            UpdateNoteDisplay();
        }
    }

    public void AdjustSelectedNoteStart(string adjustText)
    {
        float adjust;
        if (float.TryParse(adjustText, out adjust))
        {
            Note info = selectedNote.noteRef.Value;
            float newStart = info.start + adjust;

            if (!StartValid(info, newStart))
                return;

            info.start = newStart;
            selectedNote.noteRef.Value = info;

            nih.SetStart(newStart);
            nih.ClearStartAdjust();

            if (!NoteInBar(info.start, info.stop, info.beatPos))
                DeselectNote();

            UpdateNoteDisplay();
        }
    }

    bool StopValid(Note n, float stop)
    {
        return stop >= 0 && stop >= n.start && BeatPosInRange(n.start, stop, n.beatPos);
    }

    public void SetSelectedNoteStop(string stopText)
    {
        float stop;
        if (float.TryParse(stopText, out stop))
        {
            Note info = selectedNote.noteRef.Value;
            
            if (!StopValid(info, stop))
            {
                nih.SetStop(info.stop);
                return;
            }

            info.stop = stop;
            selectedNote.noteRef.Value = info;

            if (!NoteInBar(info.start, info.stop, info.beatPos))
                DeselectNote();

            UpdateNoteDisplay();
        }
    }

    public void AdjustSelectedNoteStop(string adjustText)
    {
        float adjust;
        if (float.TryParse(adjustText, out adjust))
        {
            Note info = selectedNote.noteRef.Value;
            float newStop = info.stop + adjust;

            if (!StopValid(info, newStop))
                return;

            info.stop = newStop;
            selectedNote.noteRef.Value = info;

            nih.SetStop(newStop);
            nih.ClearStopAdjust();

            if (!NoteInBar(info.start, info.stop, info.beatPos))
                DeselectNote();

            UpdateNoteDisplay();
        }
    }

    public void RemoveSelectedNote()
    {
        nih.Hide();

        notes.Remove(selectedNote.noteRef);

        selectedNote.noteRef = null;
        selectedNote.enh = null;

        UpdateNoteDisplay();
    }

    void SpawnPlaceholders()
    {
        for (int i = placeholders.Count - 1; i >= 0; i--)
        {
            Destroy(placeholders[i].gameObject);
            placeholders.RemoveAt(i);
        }

        if (currentNoteVal.noteVal == 0 || currentNoteVal.noteButton == null)
            return;

        float noteLength = (float)currentSong.beatUnit / currentNoteVal.noteVal;
        if (usingTriplet) noteLength *= 2/3f;

        for(int lane = 0; lane < LoopDisplayHandler.LANE_COUNT; lane++)
            for (float beat = 0; beat < currentSong.beatsPerBar; beat += noteLength)
            {
                GameObject newPlaceholder = Instantiate(placeholderPrefabs[lane]);
                newPlaceholder.transform.position = ldm.CalcNotePosition(lane, beat);
                newPlaceholder.transform.localScale = ldm.CalcNoteScale();

                PlaceholderHandler ph = newPlaceholder.GetComponent<PlaceholderHandler>();
                ph.em = this;
                ph.lane = lane;
                ph.beatPos = beat;

                placeholders.Add(ph);
            }
    }

    public void SelectNoteVal(int noteVal)
    {
        NoteButton selectedNote = Array.Find(noteValButtons, noteButton => noteButton.noteVal == noteVal);
        if (selectedNote.noteVal == 0 || selectedNote.noteButton == null)
        {
            Debug.LogWarning($"Note button {noteVal} not found");
            return;
        }
        if (currentNoteVal.noteButton != null) currentNoteVal.noteButton.interactable = true;
        selectedNote.noteButton.interactable = false;
        currentNoteVal = selectedNote;
        SpawnPlaceholders();
    }

    public void SelectTriplet(bool turnOn)
    {
        usingTriplet = turnOn;
        SpawnPlaceholders();
    }
}
