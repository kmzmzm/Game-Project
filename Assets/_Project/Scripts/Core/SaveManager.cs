using System;
using System.IO;
using UnityEngine;

namespace Arcana.Core
{
    /// <summary>
    /// 영구 저장 데이터 컨테이너. JSON 직렬화를 위해 [Serializable] 필수.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int baseWeaponLevel;    // 기본 무기 강화 단계 (0~5)
        public int gold;               // 보유 골드
        public int cursedFragment;     // 저주받은 파편 수량
        public int erosionWaveLevel;   // 침식 웨이브 단계 (0~4)
        public int totalRunCount;      // 총 런 횟수
        public int bestClearRoomCount; // 단일 런 최대 클리어 방 수

        public float masterVolume = 1f; // 마스터 볼륨 (0~1)
        public float bgmVolume    = 1f; // 배경음악 볼륨 (0~1)
        public float sfxVolume    = 1f; // 효과음 볼륨 (0~1)
    }

    /// <summary>
    /// 세이브 데이터의 저장·불러오기·초기화를 담당하는 매니저. JSON 파일로 영속 저장한다.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public SaveData Data { get; private set; } = new();

        // 플랫폼별 쓰기 허용 경로에 파일 저장
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

        /// <summary>
        /// 현재 Data를 JSON 파일로 저장한다. 범위를 벗어난 값은 클램프 후 저장.
        /// </summary>
        public void Save()
        {
            Data.baseWeaponLevel  = Mathf.Clamp(Data.baseWeaponLevel,  0, 5);
            Data.erosionWaveLevel = Mathf.Clamp(Data.erosionWaveLevel, 0, 4);

            File.WriteAllText(SavePath, JsonUtility.ToJson(Data, true));
        }

        /// <summary>
        /// JSON 파일에서 데이터를 읽어 Data에 반영한다. 파일이 없으면 기본값으로 초기화.
        /// </summary>
        public void Load()
        {
            if (File.Exists(SavePath))
            {
                Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

                // 파일이 외부에서 조작되었을 경우를 대비해 범위 재검증
                Data.baseWeaponLevel  = Mathf.Clamp(Data.baseWeaponLevel,  0, 5);
                Data.erosionWaveLevel = Mathf.Clamp(Data.erosionWaveLevel, 0, 4);
            }
            else
            {
                Data = new();
            }
        }

        /// <summary>
        /// 저장 파일을 삭제하고 Data를 기본값으로 초기화한다.
        /// </summary>
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            Data = new();
        }
    }
}
