using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcana.Systems
{
    /// <summary>
    /// GDD 8.4 스테이지 구성을 관리하는 싱글톤 매니저.
    /// 고정 룸(Start·Boss·보스직전Shop)과 랜덤 풀에서 뽑은 룸으로
    /// 총 7칸 스테이지를 구성한다: Start→Battle→Battle→Shop→Elite→Shop→Boss
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [Header("고정 룸 (GDD 8.4)")]
        [SerializeField] RoomData _startRoom; // 스테이지 시작 룸 (인덱스 0 고정)
        [SerializeField] RoomData _bossRoom;  // 보스 룸 (인덱스 6 고정)
        [SerializeField] RoomData _preBossShopRoom; // 보스 직전 Shop (인덱스 5 고정)

        [Header("랜덤 룸 풀")]
        [SerializeField] List<RoomData> _battleRoomPool; // Battle 슬롯(인덱스 1·2)에서 랜덤 선택
        [SerializeField] List<RoomData> _eliteRoomPool;  // Elite 슬롯(인덱스 4)에서 랜덤 선택
        [SerializeField] List<RoomData> _shopRoomPool;   // 중간 Shop 슬롯(인덱스 3)에서 랜덤 선택

        // 현재 스테이지에서 순서대로 나열된 룸 목록 (GenerateStage 호출 시 구성)
        readonly List<RoomData> _stageRooms = new();

        // 현재 룸 인덱스 (읽기 전용)
        public int CurrentRoomIndex { get; private set; }

        // 룸 클리어 시 발행 — 다음 룸 로드 전에 구독자에게 알림
        public event Action OnRoomCleared;

        GameObject _currentRoomInstance;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// GDD 8.4 기준으로 스테이지 룸 순서를 생성한다.
        /// Start(0)→Battle(1)→Battle(2)→Shop(3)→Elite(4)→Shop(5)→Boss(6)
        /// 각 Battle·Elite·Shop 슬롯은 해당 풀에서 랜덤으로 선택한다.
        /// </summary>
        public void GenerateStage()
        {
            _stageRooms.Clear();
            _stageRooms.Add(_startRoom);                        // 0: Start  (고정)
            _stageRooms.Add(GetRandom(_battleRoomPool));        // 1: Battle (랜덤)
            _stageRooms.Add(GetRandom(_battleRoomPool));        // 2: Battle (랜덤)
            _stageRooms.Add(GetRandom(_shopRoomPool));          // 3: Shop   (랜덤)
            _stageRooms.Add(GetRandom(_eliteRoomPool));         // 4: Elite  (랜덤)
            _stageRooms.Add(_preBossShopRoom);                  // 5: Shop   (보스 직전 고정)
            _stageRooms.Add(_bossRoom);                         // 6: Boss   (고정)
        }

        /// <summary>
        /// 지정한 인덱스의 룸을 로드한다.
        /// 기존 룸 인스턴스를 파괴하고 새 룸 프리팹을 인스턴스화한다.
        /// </summary>
        public void LoadRoom(int index)
        {
            if (_stageRooms.Count == 0)
            {
                Debug.LogWarning("[RoomManager] 스테이지가 생성되지 않았습니다. GenerateStage()를 먼저 호출하세요.", this);
                return;
            }

            if (index < 0 || index >= _stageRooms.Count)
            {
                Debug.LogWarning($"[RoomManager] 유효하지 않은 룸 인덱스: {index}", this);
                return;
            }

            RoomData data = _stageRooms[index];
            if (data == null || data.RoomPrefab == null)
            {
                Debug.LogWarning($"[RoomManager] 인덱스 {index}의 RoomData 또는 프리팹이 없습니다.", this);
                return;
            }

            // 기존 룸 파괴 후 새 룸 생성
            if (_currentRoomInstance != null)
                Destroy(_currentRoomInstance);

            CurrentRoomIndex     = index;
            _currentRoomInstance = Instantiate(data.RoomPrefab);
        }

        /// <summary>
        /// 현재 룸 클리어 처리. OnRoomCleared를 발행하고 다음 룸으로 이동한다.
        /// 마지막 룸(Boss) 클리어 시에는 OnRoomCleared만 발행한다.
        /// </summary>
        public void ClearCurrentRoom()
        {
            OnRoomCleared?.Invoke();

            int next = CurrentRoomIndex + 1;
            if (next < _stageRooms.Count)
                LoadRoom(next);
        }

        // 풀에서 룸 데이터를 무작위로 반환한다. 풀이 비어 있으면 null 반환.
        RoomData GetRandom(List<RoomData> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("[RoomManager] 룸 풀이 비어 있습니다.", this);
                return null;
            }
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }
    }
}
