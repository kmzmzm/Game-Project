using System.IO;
using UnityEngine;

namespace Arcana.Core
{
    /// <summary>
    /// 영구 저장 데이터 컨테이너. JSON 직렬화를 위해 [Serializable] 필수.
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        public int BaseWeaponLevel    = 0; // 기본 무기 강화 단계 (0~5)
        public int Gold               = 0; // 보유 골드
        public int CursedFragment     = 0; // 저주받은 파편 수량
        public int ErosionWaveLevel   = 0; // 침식 웨이브 단계 (0~4)
        public int TotalRunCount      = 0; // 총 런 횟수
        public int BestClearRoomCount = 0; // 단일 런 최대 클리어 방 수

        public float MasterVolume = 1f; // 마스터 볼륨 (0~1)
        public float BGMVolume    = 1f; // 배경음악 볼륨 (0~1)
        public float SFXVolume    = 1f; // 효과음 볼륨 (0~1)
    }

    /// <summary>
    /// 세이브 데이터의 저장·불러오기·초기화를 담당하는 매니저. JSON 파일로 영속 저장한다.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        // 어디서든 접근 가능한 단일 인스턴스
        public static SaveManager Instance { get; private set; }

        // 현재 세션에서 사용 중인 세이브 데이터
        public SaveData Data { get; private set; } = new SaveData();

        // 플랫폼별 쓰기 허용 경로에 파일 저장
        string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        void Awake()
        {
            // 이미 인스턴스가 존재하면 중복 오브젝트 제거
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // 씬이 전환되어도 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);
            // 앱 시작 시 저장 파일 즉시 로드
            Load();
        }

        /// <summary>
        /// 현재 Data를 JSON 파일로 저장한다. 범위를 벗어난 값은 클램프 후 저장.
        /// </summary>
        public void Save()
        {
            // 저장 전 유효 범위 강제 적용
            Data.BaseWeaponLevel  = Mathf.Clamp(Data.BaseWeaponLevel,  0, 5);
            Data.ErosionWaveLevel = Mathf.Clamp(Data.ErosionWaveLevel, 0, 4);

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
                Data.BaseWeaponLevel  = Mathf.Clamp(Data.BaseWeaponLevel,  0, 5);
                Data.ErosionWaveLevel = Mathf.Clamp(Data.ErosionWaveLevel, 0, 4);
            }
            else
            {
                Data = new SaveData();
            }
        }

        /// <summary>
        /// 저장 파일을 삭제하고 Data를 기본값으로 초기화한다.
        /// </summary>
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            Data = new SaveData();
        }
    }
}
