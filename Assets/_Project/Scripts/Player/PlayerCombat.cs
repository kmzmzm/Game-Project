using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Arcana.Core;

namespace Arcana.Player
{
    /// <summary>
    /// 기본 공격 콤보(0~2타)와 패링을 처리한다.
    /// 히트박스 판정은 애니메이션 이벤트(OnAttackHit)로 호출하고,
    /// 패링 판정은 적 공격 코드에서 TryProcessParry()를 호출해 확인한다.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("공격")]
        [SerializeField] float attackStaminaCost = 10f;
        [SerializeField] float hitstopDuration   =  0.08f; // 히트 성공 시 히트스탑 길이

        [Header("패링")]
        [SerializeField] float parryStaminaCost =  15f;
        [SerializeField] float parryWindow      =  0.3f; // 패링 성공 판정 유지 시간
        [SerializeField] float parryCooldown    =  0.8f; // 패링 재사용 대기시간

        [Header("히트박스")]
        [SerializeField] Transform hitboxCenter; // 공격 판정 중심 Transform
        [SerializeField] float     hitboxRadius = 0.8f;
        [SerializeField] LayerMask enemyLayer;

        PlayerStats      _stats;
        PlayerController _controller;
        Animator         _animator;

        int   _comboIndex;       // 현재 콤보 단계 (0~2)
        bool  _isAttacking;
        bool  _isParrying;
        float _parryCooldownTimer;
        float _attackDamage = 10f; // SetAttackDamage로 갱신

        // 히트 성공 시 발행 — float: 히트스탑 지속 시간
        public event Action<float> OnHitstopRequested;
        // 패링 성공 시 발행
        public event Action        OnParrySuccess;

        void Awake()
        {
            _stats      = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _animator   = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            if (HitstopManager.Instance != null)
                OnHitstopRequested += HitstopManager.Instance.DoHitstop;
        }

        void OnDestroy()
        {
            if (HitstopManager.Instance != null)
                OnHitstopRequested -= HitstopManager.Instance.DoHitstop;
        }

        void Update()
        {
            if (_parryCooldownTimer > 0f)
                _parryCooldownTimer -= Time.deltaTime;
        }

        // Send Messages — 기본 공격
        void OnAttack(InputValue value)
        {
            if (_controller.InputBlocked || _isAttacking) return;
            if (_stats.CurrentStamina < attackStaminaCost) return;

            _stats.UseStamina(attackStaminaCost);
            _controller.ResetStaminaDelay();

            _animator.SetInteger("ComboIndex", _comboIndex);
            _animator.SetTrigger("Attack");
            _isAttacking = true;
        }

        // Send Messages — 패링
        void OnParry(InputValue value)
        {
            if (_controller.InputBlocked) return;
            if (_parryCooldownTimer > 0f) return;
            if (_stats.CurrentStamina < parryStaminaCost) return;

            _stats.UseStamina(parryStaminaCost);
            _controller.ResetStaminaDelay();
            _parryCooldownTimer = parryCooldown;

            StartCoroutine(ParryWindowRoutine());
        }

        // 패링 윈도우는 실제 시간 기준 — hitstop(timeScale=0) 중에도 소진되어야 함
        IEnumerator ParryWindowRoutine()
        {
            _isParrying = true;
            yield return new WaitForSecondsRealtime(parryWindow);
            _isParrying = false;
        }

        // 애니메이션 이벤트 — 공격 판정 프레임에 호출
        public void OnAttackHit()
        {
            Collider[] hits = Physics.OverlapSphere(
                hitboxCenter.position, hitboxRadius, enemyLayer);

            foreach (Collider col in hits)
            {
                if (col.TryGetComponent(out IDamageable target))
                {
                    target.TakeDamage(_attackDamage, col.ClosestPoint(hitboxCenter.position));
                    OnHitstopRequested?.Invoke(hitstopDuration);
                }
            }
        }

        // 애니메이션 이벤트 — 공격 애니메이션 종료 시 호출
        public void OnAttackEnd()
        {
            _comboIndex  = (_comboIndex + 1) % 3; // 0 → 1 → 2 → 0 순환
            _isAttacking = false;
        }

        /// <summary>
        /// 적이 공격을 시도할 때 호출한다.
        /// 패링 윈도우 내에 있으면 패링 성공(true)을 반환하고 이벤트를 발행한다.
        /// </summary>
        public bool TryProcessParry()
        {
            if (!_isParrying) return false;

            _isParrying = false;
            OnParrySuccess?.Invoke();
            return true;
        }

        /// <summary>
        /// 무기 장착·해제 시 호출해 공격 데미지를 갱신한다.
        /// </summary>
        public void SetAttackDamage(float damage)
        {
            _attackDamage = damage;
        }

#if UNITY_EDITOR
        // 히트박스 반경을 씬 뷰에서 시각화
        void OnDrawGizmosSelected()
        {
            if (hitboxCenter == null) return;
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawSphere(hitboxCenter.position, hitboxRadius);
        }
#endif
    }
}
