using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalManager : MonoBehaviour
{
    public static GlobalManager instance;

    static bool paused = false;

    SoundManager soundManager;
    InputManager inputManager;
    SongLibrary songLibrary;
    TrackParser trackParser;
    UserDataManager userDataManager;

    GameObject escMenu;

    public delegate void LaneInput(int lane);
    public LaneInput LanePressed = delegate { };
    public delegate void SideEffect();
    public SideEffect PauseEffects = delegate { };
    public SideEffect UnpauseEffects = delegate { };
    
    public float syncOffset = 0f;
    public float hitOffset = 0f;

    string fileName = "";
    Song currentSong = null;
    bool usingUserSong = false;

    CalibrationManager calibrationManager;
    SongSelectManager songSelectManager;
    EditorManager editorManager;
    PlayManager playManager;
    ResultsManager resultsManager;

    // HACK
    public string testSong;
    // end HACK

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        soundManager = GetComponentInChildren<SoundManager>();
        inputManager = GetComponentInChildren<InputManager>();
        songLibrary = GetComponentInChildren<SongLibrary>();
        trackParser = GetComponentInChildren<TrackParser>();
        userDataManager = GetComponentInChildren<UserDataManager>();
    }

    void Start()
    {
        SceneManager.sceneLoaded += SetupScenes;
        SceneManager.sceneUnloaded += TeardownScenes;

        if (PlayerPrefs.HasKey("SFX Volume"))
        {
            soundManager.SetSFXVolume(PlayerPrefs.GetFloat("SFX Volume"));
        }
        if (PlayerPrefs.HasKey("Music Volume"))
        {
            soundManager.SetSongVolume(PlayerPrefs.GetFloat("Music Volume"));
        }
        if (syncOffset != 0)
        {
            PlayerPrefs.SetFloat("Sync Offset", syncOffset);
        }
        else if (PlayerPrefs.HasKey("Sync Offset"))
        {
            syncOffset = PlayerPrefs.GetFloat("Sync Offset");
        }
        if (hitOffset != 0)
        {
            PlayerPrefs.SetFloat("Hit Offset", hitOffset);
        }
        else if (PlayerPrefs.HasKey("Hit Offset"))
        {
            hitOffset = PlayerPrefs.GetFloat("Hit Offset");
        }

        Debug.Log($"Sync: {syncOffset}");
        Debug.Log($"Hit: {hitOffset}");

        // HACK
        if (testSong != "")
            (fileName, currentSong, usingUserSong) = songLibrary.FindSongByTitle(testSong);
        // end HACK

        SetupScenes(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        HideEscMenu();
    }

    #region Helper
    public static void SetSFXVolume(float volume)
    {
        instance.soundManager.SetSFXVolume(volume);
        PlayerPrefs.SetFloat("SFX Volume", volume);
    }

    public static void SetMusicVolume(float volume)
    {
        instance.soundManager.SetSongVolume(volume);
        PlayerPrefs.SetFloat("Music Volume", volume);
    }
    
    public static void SetSyncOffset(float offset)
    {
        instance.syncOffset = offset;
        PlayerPrefs.SetFloat("Sync Offset", offset);
    }

    public static void SetHitOffset(float offset)
    {
        instance.hitOffset = offset;
        PlayerPrefs.SetFloat("Hit Offset", offset);
    }

    public void SelectSong(string fileName)
    {
        (Song s, bool uus) = songLibrary.FindSong(fileName);

        if (s != null)
        {
            this.fileName = fileName;
            currentSong = s;
            usingUserSong = uus;
            songSelectManager?.SetSong(s.title, uus ? 
                userDataManager.GetUserSongScore(fileName) : 
                userDataManager.GetSongScore(fileName));
        }
    }

    public void SaveTrack(Note[] notes, string fileName)
    {
        trackParser.SaveTrack(notes, fileName);
    }

    public void SaveSong(Song s, string fileName)
    {
        songLibrary.SaveSong(s, fileName);
    }

    public void DevSaveSong(Song s, string fileName)
    {
        songLibrary.SaveSongToResources(s, fileName);
    }

    public Note[] ParseTrack(TextAsset trackFile)
    {
        return trackParser.ParseTrack(trackFile);
    }

    public void SaveScore(int score)
    {
        if (usingUserSong)
        {
            userDataManager.SaveUserSongScore(fileName, score);
        }
        else
        {
            userDataManager.SaveSongScore(fileName, score);
        }
    }

    static void StopTime()
    {
        Time.timeScale = 0;
    }

    static void ResumeTime()
    {
        Time.timeScale = 1;
    }

    public static void Pause()
    {
        ShowEscMenu();
        instance.PauseEffects();
        paused = true;
    }

    public static void Unpause()
    {
        HideEscMenu();
        instance.UnpauseEffects();
        paused = false;
    }
    #endregion

    #region Input
    public Vector3 MousePosition()
    {
        return inputManager.mousePos;
    }

    public static void HandleEsc()
    {
        if (paused)
        {
            Unpause();
        }
        else
        {
            Pause();
        }
    }

    void DisableLaneInput()
    {
        // TODO: Implement this
    }

    void EnableLaneInput()
    {
        // TODO: Implement this
    }
    #endregion

    #region UI
    public static void ShowEscMenu()
    {
        instance.escMenu?.SetActive(true);
    }

    public static void HideEscMenu()
    {
        instance.escMenu?.SetActive(false);
    }
    #endregion

    #region Scenes
    public static void QuitGame()
    {
        Application.Quit();
    }

    public static void ChangeScene(string sceneName)
    {
        if (paused)
        {
            ResumeTime();
            instance.soundManager.StopSong();
            instance.EnableLaneInput();
            paused = false;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void SetupScenes(Scene scene, LoadSceneMode mode)
    {
        escMenu = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(obj => obj.name == "Esc Menu");

        switch (scene.name)
        {
            case "TitleScene": SetupTitleScene(); break;
            case "CalibrationScene": SetupCalibrationScene(); break;
            case "SongSelectScene": SetupSongSelectScene(); break;
            case "EditorScene": SetupEditorScene(); break;
            case "PlayScene": SetupPlayScene(); break;
            case "ResultsScene": SetupResultsScene(); break;
        }
    }

    public void TeardownScenes(Scene scene)
    {
        switch (scene.name)
        {
            case "CalibrationScene": TeardownCalibrationScene(); break;
            case "SongSelectScene": TeardownSongSelectScene(); break;
            case "EditorScene": TeardownEditorScene(); break;
            case "PlayScene": TeardownPlayScene(); break;
            case "ResultsScene": TeardownResultsScene(); break;
        }

        escMenu = null;
    }

    void SetupTitleScene()
    {
        if (PlayerPrefs.HasKey("SFX Volume"))
        {
            UnityEngine.UI.Slider sfxSlider = GameObject.Find("SFX Volume Slider").GetComponent<UnityEngine.UI.Slider>();
            sfxSlider.value = PlayerPrefs.GetFloat("SFX Volume");
        }
        if (PlayerPrefs.HasKey("Music Volume"))
        {
            UnityEngine.UI.Slider musicSlider = GameObject.Find("Music Volume Slider").GetComponent<UnityEngine.UI.Slider>();
            musicSlider.value = PlayerPrefs.GetFloat("Music Volume");
        }
    }

    void SetupCalibrationScene()
    {
        calibrationManager = FindObjectOfType<CalibrationManager>();
        LanePressed += calibrationManager.Hit;
    }

    void TeardownCalibrationScene()
    {
        LanePressed -= calibrationManager.Hit;
        calibrationManager = null;
    }

    void SetupSongSelectScene()
    {
        songSelectManager = FindObjectOfType<SongSelectManager>();

        var songs = songLibrary.GetSongs();
        songSelectManager.LoadLibrary(songs);
        SelectSong(songs[0].Item1);
    }

    void TeardownSongSelectScene()
    {
        songSelectManager = null;
    }

    void SetupEditorScene()
    {
        editorManager = FindObjectOfType<EditorManager>();

        if (currentSong != null)
            editorManager.LoadSong(currentSong, fileName);
        else
            editorManager.InitializeEmpty();
    }

    void TeardownEditorScene()
    {
        editorManager = null;
    }

    void SetupPlayScene()
    {
        playManager = FindObjectOfType<PlayManager>();
        LanePressed += playManager.CheckLane;
        PauseEffects += StopTime;
        PauseEffects += soundManager.PauseSong;
        PauseEffects += DisableLaneInput;
        UnpauseEffects += ResumeTime;
        UnpauseEffects += soundManager.UnpauseSong;
        UnpauseEffects += EnableLaneInput;

        if (currentSong != null)
        {
            soundManager.LoadSong(currentSong);
            playManager.LoadSong(currentSong);
        }
    }

    void TeardownPlayScene()
    {
        LanePressed -= playManager.CheckLane;
        PauseEffects -= StopTime;
        PauseEffects -= soundManager.PauseSong;
        PauseEffects -= DisableLaneInput;
        UnpauseEffects -= ResumeTime;
        UnpauseEffects -= soundManager.UnpauseSong;
        UnpauseEffects -= EnableLaneInput;
        playManager = null;
    }

    void SetupResultsScene()
    {
        resultsManager = FindObjectOfType<ResultsManager>();

        resultsManager.SetText(PlayManager.score, PlayManager.maxCombo);
    }

    void TeardownResultsScene()
    {
        resultsManager = null;

        soundManager.StopSong();
    }
    #endregion

    #region Sounds
    public void PlaySong()
    {
        soundManager.PlaySong();
    }

    public void StartMetronome()
    {
        soundManager.Play("metronome");
    }

    public void StopMetronome()
    {
        soundManager.Stop("metronome");
    }

    public void PlayHitSFX()
    {
        soundManager.Play("hit sfx");
    }
    #endregion
}
