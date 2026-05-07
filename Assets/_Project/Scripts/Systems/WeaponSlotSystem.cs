using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Arcana.Core;
using Arcana.Player;

namespace Arcana.Systems
{
    /// <summary>
    /// 슬롯 A(주무기)와 슬롯 B(보조무기) 2슬롯 무기 장착 시스템.
    /// PlayerInput의 Send Messages 방식으로 Q키 입력(SwapWeapon 액션)을 수신한다.
    /// PlayerInput 컴포넌트와 동일한 GameObject에 부착해야 한다.
    /// </summary>
    [RequireComponent(typeof(PlayerCombat))]
    public class WeaponSlotSystem : MonoBehaviour
    {
        // SaveData.baseWeaponLevel(0~5) 인덱스에 대응하는 기본 무기 목록
        [SerializeField] WeaponData[] _baseWeaponProgression;

        WeaponData   _slotA;  // 슬롯 A — 런 시작 시 SaveData 기준으로 복구
        WeaponData   _slotB;  // 슬롯 B — 런 중 획득 무기, 런 종료 시 초기화
        PlayerCombat _combat;

        public WeaponData SlotA => _slotA;
        public WeaponData SlotB => _slotB;

        // 무기 장착·교환 시 발행 — (무기 데이터, 슬롯 인덱스: 0=A / 1=B)
        public event Action<WeaponData, int> OnWeaponChanged;

        void Awake()
        {
            _combat = GetComponent<PlayerCombat>();
        }

        /// <summary>
        /// 런 시작 시 호출. SaveData.baseWeaponLevel 기준으로 슬롯 A를 초기화한다.
        /// </summary>
        public void InitRun()
        {
            if (_baseWeaponProgression == null || _baseWeaponProgression.Length == 0)
            {
                Debug.LogWarning("[WeaponSlotSystem] baseWeaponProgression이 비어 있습니다.", this);
                return;
            }

            int level = SaveManager.Instance != null
                ? SaveManager.Instance.Data.baseWeaponLevel
                : 0;

            int idx = Mathf.Clamp(level, 0, _baseWeaponProgression.Length - 1);
            EquipWeapon(_baseWeaponProgression[idx], 0);
        }

        /// <summary>
        /// 런 종료 시 호출. 슬롯 B를 초기화하고 슬롯 A를 SaveData 기준으로 복구한다.
        /// </summary>
        public void EndRun()
        {
            // 슬롯 B 초기화 — 런 중 획득 무기는 영속 보관하지 않음
            _slotB = null;
            OnWeaponChanged?.Invoke(null, 1);

            InitRun();
        }

        /// <summary>
        /// 지정 슬롯에 무기를 장착한다.
        /// </summary>
        /// <param name="weapon">장착할 무기 데이터</param>
        /// <param name="slot">0 = 슬롯 A, 1 = 슬롯 B</param>
        public void EquipWeapon(WeaponData weapon, int slot)
        {
            if (slot == 0)
                _slotA = weapon;
            else
                _slotB = weapon;

            // 슬롯 A가 교체된 경우 PlayerCombat 공격력 즉시 갱신
            if (slot == 0 && weapon != null)
                _combat.SetAttackDamage(weapon.AttackDamage);

            OnWeaponChanged?.Invoke(weapon, slot);
        }

        /// <summary>
        /// 슬롯 A↔B를 교환한다. 슬롯 B가 비어 있으면 교환하지 않는다.
        /// </summary>
        public void SwapWeapon()
        {
            if (_slotB == null) return;

            (_slotA, _slotB) = (_slotB, _slotA);

            // 교환 후 활성 무기(A)로 PlayerCombat 공격력 갱신
            _combat.SetAttackDamage(_slotA.AttackDamage);

            OnWeaponChanged?.Invoke(_slotA, 0);
            OnWeaponChanged?.Invoke(_slotB, 1);
        }

        // Send Messages — InputActions 에셋에 "SwapWeapon" 액션(Q키) 추가 필요
        void OnSwapWeapon(InputValue value)
        {
            SwapWeapon();
        }
    }
}
