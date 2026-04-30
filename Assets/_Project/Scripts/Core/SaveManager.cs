using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int   BaseWeaponLevel    = 1;
    public int   Gold               = 0;
    public int   CursedFragment     = 0;
    public int   ErosionWaveLevel   = 1;
    public int   TotalRunCount      = 0;
    public int   BestClearRoomCount = 0;
    public float MasterVolume       = 1f;
    public float BGMVolume          = 1f;
    public float SFXVolume          = 1f;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public SaveData Data { get; private set; } = new SaveData();

    string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void Save()
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(Data, true));
    }

    public void Load()
    {
        Data = File.Exists(SavePath)
            ? JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath))
            : new SaveData();
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        Data = new SaveData();
    }
}
