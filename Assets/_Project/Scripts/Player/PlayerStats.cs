using System;
using UnityEngine;
using Arcana.Core;

namespace Arcana.Player
{
    /// <summary>
    /// 플레이어의 HP·스태미나·방어력 수치를 관리하고 변경 이벤트를 발행한다.
    /// </summary>
    public class PlayerStats : MonoBehaviour, IDamageable, IHealable
    {
        [SerializeField] float baseMaxHp          = 100f; // 기본 최대 HP
        [SerializeField] float armorMaxHpBonus    =  50f; // 방어구로 늘릴 수 있는 최대 HP 보너스
        [SerializeField] float maxStamina         = 100f;
        [SerializeField] float maxArmorReduction  =  0.3f; // 방어력이 줄일 수 있는 최대 데미지 비율

        float _currentMaxHp;
        float _currentHp;
        float _currentStamina;
        float _currentArmorReduction; // 0 ~ maxArmorReduction

        public float CurrentHp       => _currentHp;
        public float CurrentMaxHp    => _currentMaxHp;
        public float CurrentStamina  => _currentStamina;
        public float MaxStamina      => maxStamina;
        public bool  IsDead          => _currentHp <= 0f;

        // (현재 값, 최대 값) 순서로 전달
        public event Action<float, float> OnHpChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action               OnDied;

        void Awake()
        {
            ResetForRun();
        }

        // IDamageable 구현 — 방어력 감소 적용 후 HP 차감
        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (IsDead) return;

            float reduced = damage * (1f - _currentArmorReduction);
            _currentHp = Mathf.Max(_currentHp - reduced, 0f);
            OnHpChanged?.Invoke(_currentHp, _currentMaxHp);

            if (_currentHp <= 0f)
                OnDied?.Invoke();
        }

        // IHealable 구현
        public void Heal(float amount) => HealHp(amount);

        public void HealHp(float amount)
        {
            if (IsDead) return;
            _currentHp = Mathf.Min(_currentHp + amount, _currentMaxHp);
            OnHpChanged?.Invoke(_currentHp, _currentMaxHp);
        }

        public void UseStamina(float amount)
        {
            _currentStamina = Mathf.Max(_currentStamina - amount, 0f);
            OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
        }

        public void RecoverStamina(float amount)
        {
            _currentStamina = Mathf.Min(_currentStamina + amount, maxStamina);
            OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
        }

        /// <summary>
        /// 방어구 장착 시 호출. 최대 HP와 방어율을 올린다.
        /// </summary>
        /// <param name="hpBonus">추가할 최대 HP (armorMaxHpBonus 범위 내로 제한됨)</param>
        /// <param name="armorReduction">추가할 데미지 감소율 (0~1)</param>
        public void ApplyArmorBonus(float hpBonus, float armorReduction)
        {
            float allowedBonus = Mathf.Min(hpBonus, baseMaxHp + armorMaxHpBonus - _currentMaxHp);
            _currentMaxHp += allowedBonus;
            _currentHp = Mathf.Min(_currentHp + allowedBonus, _currentMaxHp);

            _currentArmorReduction = Mathf.Clamp(_currentArmorReduction + armorReduction, 0f, maxArmorReduction);
            OnHpChanged?.Invoke(_currentHp, _currentMaxHp);
        }

        /// <summary>
        /// 방어구 해제 시 호출. 최대 HP와 방어율을 내린다.
        /// </summary>
        public void RemoveArmorBonus(float hpBonus, float armorReduction)
        {
            _currentMaxHp = Mathf.Max(_currentMaxHp - hpBonus, baseMaxHp);
            _currentHp = Mathf.Min(_currentHp, _currentMaxHp);

            _currentArmorReduction = Mathf.Clamp(_currentArmorReduction - armorReduction, 0f, maxArmorReduction);
            OnHpChanged?.Invoke(_currentHp, _currentMaxHp);
        }

        /// <summary>
        /// 런 시작 시 모든 스탯을 초기값으로 되돌린다.
        /// </summary>
        public void ResetForRun()
        {
            _currentMaxHp          = baseMaxHp;
            _currentHp             = baseMaxHp;
            _currentStamina        = maxStamina;
            _currentArmorReduction = 0f;
        }
    }
}
