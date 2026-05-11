using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Arcana.Core;

namespace Arcana.Player
{
    /// <summary>
    /// 기본 공격 콤보(0~2타)와 패링을 처리한다.
    /// 히트박스 판정은 자식 오브젝트의 SphereCollider 트리거(AttackHitboxTrigger)로 처리한다.
    /// 변경 이유: Physics.OverlapSphere는 호출 시점 한 프레임만 체크하지만,
    ///           트리거 방식은 콜라이더 활성화 구간 동안 진입한 적을 모두 감지할 수 있고
    ///           Inspector에서 크기·위치를 직접 조정 가능해 애니메이션 연동에 유리하다.
    /// 패링 판정은 적 공격 코드에서 TryProcessParry()를 호출해 확인한다.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("공격")]
        [SerializeField] float attackStaminaCost = 10f;
        [SerializeField] float hitstopDuration   = 0.08f; // 히트 성공 시 히트스탑 길이

        [Header("패링")]
        [SerializeField] float parryStaminaCost = 15f;
        [SerializeField] float parryWindow      = 0.3f;   // 패링 성공 판정 유지 시간
        [SerializeField] float parryCooldown    = 0.8f;   // 패링 재사용 대기시간

        [Header("히트박스")]
        // 자식 오브젝트(AttackHitbox)에 AttackHitboxTrigger + SphereCollider 부착 후 연결
        [SerializeField] AttackHitboxTrigger attackHitbox;
        [SerializeField] float               hitboxActiveDuration = 0.2f; // 활성화 유지 시간 (초)
        [SerializeField] LayerMask           enemyLayer;

        PlayerStats      _stats;
        PlayerController _controller;
        Animator         _animator;

        int   _comboIndex;
        bool  _isAttacking;
        bool  _isParrying;
        float _parryCooldownTimer;
        float _attackDamage = 10f; // SetAttackDamage로 갱신

        // 한 스윙에서 동일 적 중복 히트 방지 — 콤보 새 타격 시 초기화
        readonly HashSet<Collider> _hitEnemies = new HashSet<Collider>();

        // 히트 성공 시 발행 — float: 히트스탑 지속 시간
        public event Action<float> OnHitstopRequested;
        // 패링 성공 시 발행
        public event Action        OnParrySuccess;

        void Awake()
        {
            _stats      = GetComponent<PlayerStats>();
            _controller = GetComponent<PlayerController>();
            _animator   = GetComponentInChildren<Animator>();

            if (_stats == null)
                Debug.LogWarning("[PlayerCombat] PlayerStats 컴포넌트를 찾을 수 없습니다.", this);
            if (_controller == null)
                Debug.LogWarning("[PlayerCombat] PlayerController 컴포넌트를 찾을 수 없습니다.", this);
            if (_animator == null)
                Debug.LogWarning("[PlayerCombat] Animator 컴포넌트를 찾을 수 없습니다.", this);
            if (attackHitbox == null)
                Debug.LogWarning("[PlayerCombat] AttackHitboxTrigger가 연결되지 않았습니다.", this);

            // 자식 트리거에서 발생하는 히트 이벤트 구독
            if (attackHitbox != null)
                attackHitbox.OnHit += HandleHit;
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

            if (attackHitbox != null)
                attackHitbox.OnHit -= HandleHit;
        }

        void Update()
        {
            if (_parryCooldownTimer > 0f)
                _parryCooldownTimer -= Time.deltaTime;
        }

        // Send Messages — 기본 공격
        void OnAttack(InputValue value)
        {
            if (_controller == null) return;
            if (_controller.InputBlocked || _isAttacking) return;
            if (_stats.CurrentStamina < attackStaminaCost) return;

            _stats.UseStamina(attackStaminaCost);
            _controller.ResetStaminaDelay();

            _isAttacking = true;

            if (_animator != null)
            {
                // Animator가 있으면 기존대로 이벤트(OnAttackHit, OnAttackEnd)에서 판정 처리
                _animator.SetInteger("ComboIndex", _comboIndex);
                _animator.SetTrigger("Attack");
            }
            else
            {
                // 임시 처리 - 애니메이션 없을 때: 즉시 판정 호출
                // OnAttackEnd는 히트박스 활성 구간 종료 후 지연 호출
                // (즉시 호출 시 _isAttacking=false → HandleHit에서 데미지 처리 안 됨)
                OnAttackHit();
                StartCoroutine(DelayedAttackEnd());
            }
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

        // 애니메이션 이벤트 또는 임시 즉시 호출 — 히트박스 SphereCollider 활성화
        public void OnAttackHit()
        {
            if (attackHitbox == null) return;
            StartCoroutine(HitboxActiveRoutine());
        }

        // 콤보 새 타격 시작: HashSet 초기화 → 히트박스 활성화 → 0.2초 후 비활성화
        IEnumerator HitboxActiveRoutine()
        {
            _hitEnemies.Clear(); // 콤보 새 타격마다 초기화
            attackHitbox.SetActive(true);

            yield return new WaitForSeconds(hitboxActiveDuration);

            attackHitbox.SetActive(false);
        }

        // 임시 처리 - 애니메이션 없을 때: 히트박스 활성 구간 종료 후 OnAttackEnd 호출
        IEnumerator DelayedAttackEnd()
        {
            yield return new WaitForSeconds(hitboxActiveDuration);
            OnAttackEnd();
        }

        // AttackHitboxTrigger.OnHit 콜백 — enemyLayer 필터링 후 데미지 처리
        void HandleHit(Collider other)
        {
            if (!_isAttacking) return;
            if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;
            if (!_hitEnemies.Add(other)) return; // 스윙당 동일 적은 한 번만 히트

            if (other.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(_attackDamage, other.ClosestPoint(transform.position));
                OnHitstopRequested?.Invoke(hitstopDuration);
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
        // 히트박스 SphereCollider 범위를 씬 뷰에서 시각화
        void OnDrawGizmosSelected()
        {
            if (attackHitbox == null) return;
            var col = attackHitbox.GetComponent<SphereCollider>();
            if (col == null) return;
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Vector3 center = attackHitbox.transform.TransformPoint(col.center);
            Gizmos.DrawWireSphere(center, col.radius * attackHitbox.transform.lossyScale.x);
        }
#endif
    }
}
