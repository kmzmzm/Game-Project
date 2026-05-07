using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Arcana.Player;

namespace Arcana.Systems
{
    /// <summary>
    /// 현재 장착 무기의 스킬 풀에서 2개를 랜덤 확정하고 발동·쿨다운·스태미나를 관리한다.
    /// WeaponSlotSystem의 슬롯 A(0)가 바뀔 때마다 스킬을 자동으로 재룰한다.
    /// PlayerInput 컴포넌트와 동일한 GameObject에 부착해야 한다.
    /// </summary>
    [RequireComponent(typeof(WeaponSlotSystem))]
    [RequireComponent(typeof(PlayerStats))]
    public class SkillSystem : MonoBehaviour
    {
        WeaponSlotSystem _weaponSlotSystem;
        PlayerStats      _stats;

        readonly SkillData[] _activeSkills    = new SkillData[2]; // 활성 스킬 (슬롯 0 = 1번 키, 슬롯 1 = 2번 키)
        readonly float[]     _cooldownTimers  = new float[2];     // 슬롯별 남은 쿨다운 (초)

        // 스킬 발동 시 발행 — (스킬 데이터, 슬롯 인덱스)
        public event Action<SkillData, int>      OnSkillActivated;

        // 스킬 풀 재룰 시 발행 — (슬롯0 스킬, 슬롯1 스킬)
        public event Action<SkillData, SkillData> OnSkillsChanged;

        // UI 바인딩용 — 읽기 전용 노출
        public IReadOnlyList<SkillData> ActiveSkills                    => _activeSkills;
        public float                    GetCooldownRemaining(int slot)  => _cooldownTimers[slot];
        public bool                     IsReady(int slot)               => _cooldownTimers[slot] <= 0f;

        void Awake()
        {
            _weaponSlotSystem = GetComponent<WeaponSlotSystem>();
            _stats            = GetComponent<PlayerStats>();
        }

        void OnEnable()
        {
            _weaponSlotSystem.OnWeaponChanged += HandleWeaponChanged;
        }

        void OnDisable()
        {
            _weaponSlotSystem.OnWeaponChanged -= HandleWeaponChanged;
        }

        void Update()
        {
            // 슬롯별 쿨다운 감소
            for (int i = 0; i < _cooldownTimers.Length; i++)
            {
                if (_cooldownTimers[i] > 0f)
                    _cooldownTimers[i] = Mathf.Max(_cooldownTimers[i] - Time.deltaTime, 0f);
            }
        }

        /// <summary>
        /// 지정 슬롯의 스킬을 발동한다.
        /// 패시브 스킬·쿨다운 중·스태미나 부족 시 무시된다.
        /// </summary>
        public void ActivateSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _activeSkills.Length) return;

            SkillData skill = _activeSkills[slotIndex];
            if (skill == null)                            return;
            if (skill.SkillType == SkillType.Passive)    return; // 패시브는 수동 발동 불가
            if (_cooldownTimers[slotIndex] > 0f)         return; // 쿨다운 중
            if (_stats.CurrentStamina < skill.StaminaCost) return; // 스태미나 부족

            _stats.UseStamina(skill.StaminaCost);
            _cooldownTimers[slotIndex] = skill.Cooldown;

            OnSkillActivated?.Invoke(skill, slotIndex);
        }

        // 무기 변경 시 슬롯 A(0)만 감지해 스킬 재룰
        void HandleWeaponChanged(WeaponData weapon, int slot)
        {
            if (slot != 0) return;
            RollSkills(weapon);
        }

        // 무기 스킬 풀을 셔플한 뒤 앞에서 2개 확정
        void RollSkills(WeaponData weapon)
        {
            _activeSkills[0]   = null;
            _activeSkills[1]   = null;
            _cooldownTimers[0] = 0f;
            _cooldownTimers[1] = 0f;

            if (weapon == null || weapon.SkillPool == null || weapon.SkillPool.Count == 0)
            {
                OnSkillsChanged?.Invoke(null, null);
                return;
            }

            // 풀을 복사해 셔플 — 원본 ScriptableObject는 건드리지 않음
            List<SkillData> pool = new List<SkillData>(weapon.SkillPool);
            Shuffle(pool);

            _activeSkills[0] = pool.Count > 0 ? pool[0] : null;
            _activeSkills[1] = pool.Count > 1 ? pool[1] : null;

            OnSkillsChanged?.Invoke(_activeSkills[0], _activeSkills[1]);
        }

        // Fisher-Yates 셔플
        void Shuffle(List<SkillData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Send Messages — InputActions 에셋에 "Skill1" 액션(1키) 추가 필요
        void OnSkill1(InputValue value)
        {
            if (value.isPressed) ActivateSkill(0);
        }

        // Send Messages — InputActions 에셋에 "Skill2" 액션(2키) 추가 필요
        void OnSkill2(InputValue value)
        {
            if (value.isPressed) ActivateSkill(1);
        }
    }
}
