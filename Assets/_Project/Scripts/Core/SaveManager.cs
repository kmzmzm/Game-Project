using System.IO;
using UnityEngine;

namespace Arcana.Core
{
    [System.Serializable]
    public class SaveData
    {
        // 0~5
        public int BaseWeaponLevel    = 0;
        public int Gold               = 0;
        public int CursedFragment     = 0;
        // 0~4
        public int ErosionWaveLevel   = 0;
        public int TotalRunCount      = 0;
        public int BestClearRoomCount = 0;

        public float MasterVolume = 1f;
        public float BGMVolume    = 1f;
        public float SFXVolume    = 1f;
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
            Data.BaseWeaponLevel  = Mathf.Clamp(Data.BaseWeaponLevel,  0, 5);
            Data.ErosionWaveLevel = Mathf.Clamp(Data.ErosionWaveLevel, 0, 4);
            File.WriteAllText(SavePath, JsonUtility.ToJson(Data, true));
        }

        public void Load()
        {
            if (File.Exists(SavePath))
            {
                Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                Data.BaseWeaponLevel  = Mathf.Clamp(Data.BaseWeaponLevel,  0, 5);
                Data.ErosionWaveLevel = Mathf.Clamp(Data.ErosionWaveLevel, 0, 4);
            }
            else
            {
                Data = new SaveData();
            }
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            Data = new SaveData();
        }
    }
}
