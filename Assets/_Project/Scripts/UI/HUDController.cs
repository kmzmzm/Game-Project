using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arcana.Player;
using Arcana.Systems;

namespace Arcana.UI
{
    /// <summary>
    /// 인게임 HUD 패널. HP·스태미나·재화·무기 슬롯 아이콘을 표시한다.
    /// PlayerStats, WeaponSlotSystem은 Inspector에서 연결하고
    /// GoldManager는 싱글톤으로 직접 접근한다.
    /// </summary>
    public class HUDController : UIBase
    {
        [Header("체력")]
        [SerializeField] Slider _hpSlider;
        [SerializeField] float  _hpLerpSpeed = 5f; // HP 보간 속도 (클수록 빠름)

        [Header("스태미나")]
        [SerializeField] Slider _staminaSlider;

        [Header("재화")]
        [SerializeField] TextMeshProUGUI _goldText;           // 현재 런 골드
        [SerializeField] TextMeshProUGUI _cursedFragmentText; // 잠식 파편 수량

        [Header("무기 슬롯")]
        [SerializeField] Image _weaponSlotAIcon; // 슬롯 A 아이콘 (주무기)
        [SerializeField] Image _weaponSlotBIcon; // 슬롯 B 아이콘 (보조무기)

        [Header("참조")]
        [SerializeField] PlayerStats      _playerStats;
        [SerializeField] WeaponSlotSystem _weaponSlotSystem;

        float _targetHpFill; // Lerp 목표값 (0~1) — Update에서 슬라이더에 반영

        protected override void Awake()
        {
            base.Awake();
        }

        void OnEnable()
        {
            if (_playerStats != null)
            {
                _playerStats.OnHpChanged      += HandleHpChanged;
                _playerStats.OnStaminaChanged += HandleStaminaChanged;
            }

            if (_weaponSlotSystem != null)
                _weaponSlotSystem.OnWeaponChanged += HandleWeaponChanged;

            if (GoldManager.Instance != null)
            {
                GoldManager.Instance.OnGoldChanged           += HandleGoldChanged;
                GoldManager.Instance.OnCursedFragmentChanged += HandleCursedFragmentChanged;
            }

            // 활성화 시점에 현재 값으로 즉시 초기화
            RefreshAll();
        }

        void OnDisable()
        {
            if (_playerStats != null)
            {
                _playerStats.OnHpChanged      -= HandleHpChanged;
                _playerStats.OnStaminaChanged -= HandleStaminaChanged;
            }

            if (_weaponSlotSystem != null)
                _weaponSlotSystem.OnWeaponChanged -= HandleWeaponChanged;

            if (GoldManager.Instance != null)
            {
                GoldManager.Instance.OnGoldChanged           -= HandleGoldChanged;
                GoldManager.Instance.OnCursedFragmentChanged -= HandleCursedFragmentChanged;
            }
        }

        void Update()
        {
            // HP 슬라이더 부드러운 보간 — 목표값을 향해 매 프레임 접근
            if (_hpSlider != null)
                _hpSlider.value = Mathf.Lerp(_hpSlider.value, _targetHpFill, Time.deltaTime * _hpLerpSpeed);
        }

        // HP 변경 이벤트 핸들러 — 목표 채움값만 갱신, 실제 보간은 Update에서 처리
        void HandleHpChanged(float current, float max)
        {
            _targetHpFill = max > 0f ? current / max : 0f;
        }

        // 스태미나 변경 이벤트 핸들러 — 즉시 반영
        void HandleStaminaChanged(float current, float max)
        {
            if (_staminaSlider != null)
                _staminaSlider.value = max > 0f ? current / max : 0f;
        }

        // 골드 변경 이벤트 핸들러
        void HandleGoldChanged(int amount)
        {
            if (_goldText != null)
                _goldText.text = amount.ToString("N0");
        }

        // 잠식 파편 변경 이벤트 핸들러
        void HandleCursedFragmentChanged(int amount)
        {
            if (_cursedFragmentText != null)
                _cursedFragmentText.text = amount.ToString();
        }

        // 무기 슬롯 변경 이벤트 핸들러 — 슬롯 A(0) / B(1) 아이콘 갱신
        void HandleWeaponChanged(WeaponData weapon, int slot)
        {
            Image target = slot == 0 ? _weaponSlotAIcon : _weaponSlotBIcon;
            if (target == null) return;

            if (weapon != null && weapon.Icon != null)
            {
                target.sprite  = weapon.Icon;
                target.enabled = true;
            }
            else
            {
                target.sprite  = null;
                target.enabled = false;
            }
        }

        // 활성화 시 현재 수치를 읽어 모든 UI 요소를 초기화한다
        void RefreshAll()
        {
            if (_playerStats != null)
            {
                float hpFill = _playerStats.CurrentMaxHp > 0f
                    ? _playerStats.CurrentHp / _playerStats.CurrentMaxHp : 0f;

                // 첫 표시이므로 보간 없이 즉시 설정
                _targetHpFill = hpFill;
                if (_hpSlider != null) _hpSlider.value = hpFill;

                HandleStaminaChanged(_playerStats.CurrentStamina, _playerStats.MaxStamina);
            }

            if (GoldManager.Instance != null)
            {
                HandleGoldChanged(GoldManager.Instance.CurrentGold);
                HandleCursedFragmentChanged(GoldManager.Instance.CursedFragment);
            }

            // 무기 슬롯 초기화
            if (_weaponSlotSystem != null)
            {
                HandleWeaponChanged(_weaponSlotSystem.SlotA, 0);
                HandleWeaponChanged(_weaponSlotSystem.SlotB, 1);
            }
        }
    }
}
