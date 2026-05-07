using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcana.Player
{
    /// <summary>
    /// 아이소메트릭 카메라 기준 이동·구르기·스태미나 자동 회복을 처리한다.
    /// New Input System의 Send Messages 방식으로 입력을 수신한다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed             =  5f;  // 기본 이동 속도
        [SerializeField] float rollSpeed             = 12f;  // 구르기 이동 속도
        [SerializeField] float rollInvincibleDuration = 0.3f; // 구르기 무적 시간(초)
        [SerializeField] float rollStaminaCost       = 25f;  // 구르기 스태미나 소모량
        [SerializeField] float staminaRecoveryRate   = 20f;  // 초당 스태미나 회복량
        [SerializeField] float staminaRecoveryDelay  =  0.5f; // 행동 후 회복 시작까지 딜레이

        CharacterController _cc;
        PlayerStats         _stats;

        Vector2  _moveInput;
        float    _staminaDelayTimer;
        Coroutine _rollCoroutine;

        // 대화·컷씬 등 외부에서 입력을 막을 때 true로 설정
        public bool InputBlocked { get; set; }
        public bool IsRolling    { get; private set; }
        public bool IsInvincible { get; private set; }

        void Awake()
        {
            _cc    = GetComponent<CharacterController>();
            _stats = GetComponent<PlayerStats>();
        }

        void Update()
        {
            if (!IsRolling)
                HandleMovement();

            HandleStaminaRecovery();
        }

        // Send Messages — 이동 입력
        void OnMove(InputValue value)
        {
            _moveInput = InputBlocked ? Vector2.zero : value.Get<Vector2>();
        }

        // Send Messages — 구르기 입력
        void OnRoll(InputValue value)
        {
            if (InputBlocked || IsRolling) return;
            if (_stats.CurrentStamina < rollStaminaCost) return;

            if (_rollCoroutine != null)
                StopCoroutine(_rollCoroutine);

            _rollCoroutine = StartCoroutine(RollRoutine());
        }

        void HandleMovement()
        {
            if (_moveInput.sqrMagnitude < 0.01f) return;

            Vector3 worldDir = ToIsoDirection(_moveInput);
            _cc.SimpleMove(worldDir * moveSpeed);
            transform.rotation = Quaternion.LookRotation(worldDir);
        }

        void HandleStaminaRecovery()
        {
            if (_staminaDelayTimer > 0f)
            {
                _staminaDelayTimer -= Time.deltaTime;
                return;
            }

            if (_stats.CurrentStamina < _stats.MaxStamina)
                _stats.RecoverStamina(staminaRecoveryRate * Time.deltaTime);
        }

        IEnumerator RollRoutine()
        {
            _stats.UseStamina(rollStaminaCost);
            ResetStaminaDelay();

            IsRolling    = true;
            IsInvincible = true;

            // 입력이 없으면 현재 바라보는 방향으로 구르기
            Vector3 rollDir = _moveInput.sqrMagnitude > 0.01f
                ? ToIsoDirection(_moveInput)
                : transform.forward;

            float elapsed = 0f;
            while (elapsed < rollInvincibleDuration)
            {
                _cc.Move(rollDir * rollSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            IsInvincible   = false;
            IsRolling      = false;
            _rollCoroutine = null;
        }

        /// <summary>
        /// 스태미나 회복 딜레이를 리셋한다. 공격 등 외부 시스템에서도 호출한다.
        /// </summary>
        public void ResetStaminaDelay()
        {
            _staminaDelayTimer = staminaRecoveryDelay;
        }

        // 아이소메트릭 카메라 forward/right를 수평면으로 투영해 입력 방향을 월드 방향으로 변환
        Vector3 ToIsoDirection(Vector2 input)
        {
            Transform cam = Camera.main.transform;

            Vector3 camForward = cam.forward;
            Vector3 camRight   = cam.right;
            camForward.y = 0f;
            camRight.y   = 0f;
            camForward.Normalize();
            camRight.Normalize();

            return (camForward * input.y + camRight * input.x).normalized;
        }
    }
}
