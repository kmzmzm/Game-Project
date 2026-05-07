using System;
using UnityEngine;
using Arcana.Core;

namespace Arcana.Systems
{
    /// <summary>
    /// 골드와 잠식 파편을 관리하는 싱글톤 매니저.
    /// CurrentGold — 현재 런에서 획득한 골드 (런 종료 시 TotalGold에 누적 후 초기화).
    /// TotalGold    — SaveData.gold에 영속 저장되는 누적 골드.
    /// CursedFragment — 런에 관계없이 영속되는 메타 재화.
    /// </summary>
    public class GoldManager : MonoBehaviour
    {
        public static GoldManager Instance { get; private set; }

        // 현재 런 중 획득한 골드 (런 종료 전까지는 SaveData에 반영되지 않음)
        public int CurrentGold     { get; private set; }

        // SaveData에 저장된 누적 골드
        public int TotalGold       { get; private set; }

        // SaveData에 저장된 잠식 파편 수량
        public int CursedFragment  { get; private set; }

        // 골드 변동 시 발행 — 현재 런 골드(CurrentGold) 전달
        public event Action<int> OnGoldChanged;

        // 잠식 파편 변동 시 발행 — 현재 파편 수량 전달
        public event Action<int> OnCursedFragmentChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromSave();
        }

        // SaveData에서 초기값 불러오기
        void LoadFromSave()
        {
            if (SaveManager.Instance == null) return;

            TotalGold      = SaveManager.Instance.Data.gold;
            CursedFragment = SaveManager.Instance.Data.cursedFragment;
            CurrentGold    = 0;
        }

        /// <summary>
        /// 현재 런 골드를 증가시킨다.
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            CurrentGold += amount;
            OnGoldChanged?.Invoke(CurrentGold);
        }

        /// <summary>
        /// 현재 런 골드를 소비한다. 잔액 부족 시 false 반환.
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0)        return true;
            if (CurrentGold < amount) return false;

            CurrentGold -= amount;
            OnGoldChanged?.Invoke(CurrentGold);
            return true;
        }

        /// <summary>
        /// 잠식 파편을 추가하고 SaveData에 즉시 저장한다.
        /// </summary>
        public void AddCursedFragment(int amount)
        {
            if (amount <= 0) return;

            CursedFragment += amount;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.cursedFragment = CursedFragment;
                SaveManager.Instance.Save();
            }

            OnCursedFragmentChanged?.Invoke(CursedFragment);
        }

        /// <summary>
        /// 런 시작 시 호출. CurrentGold를 초기화하고 SaveData를 다시 읽는다.
        /// </summary>
        public void InitRun()
        {
            LoadFromSave();
            OnGoldChanged?.Invoke(CurrentGold);
            OnCursedFragmentChanged?.Invoke(CursedFragment);
        }

        /// <summary>
        /// 런 종료 시 호출. CurrentGold를 TotalGold에 누적하고 SaveData에 저장한다.
        /// </summary>
        public void EndRun()
        {
            TotalGold += CurrentGold;
            CurrentGold = 0;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.gold = TotalGold;
                SaveManager.Instance.Save();
            }

            OnGoldChanged?.Invoke(CurrentGold);
        }
    }
}
