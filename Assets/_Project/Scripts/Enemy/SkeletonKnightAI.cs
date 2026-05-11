using System.Collections;
using UnityEngine;
using Arcana.Core;
using Arcana.Systems;

namespace Arcana.Enemy
{
    /// <summary>
    /// 해골 기사 AI (GDD 9.3)
    /// Idle: 웨이포인트 왕복 순찰 / Chase: 빠른 추적
    /// Attack: 3타 콤보 (22/22/33) 또는 돌진 공격 (35, 넉백)
    /// 특수: HP 50% 이하 → 방패 발동 (정면 피해 50% 감소 + 패링 윈도우)
    ///
    /// 주의: EnemyBase.TakeDamage가 virtual로 선언되어 있어야 방패 오버라이드가 동작함
    /// </summary>
    public class SkeletonKnightAI : EnemyBase
    {
        [Header("해골 기사 - 순찰")]
        [SerializeField] Transform[] _waypoints;            // 왕복 순찰 포인트 (2개 권장)
        [SerializeField] float _patrolSpeed      = 2f;      // 순찰 이동 속도
        [SerializeField] float _waypointStopDist = 0.5f;    // 웨이포인트 도착 판정 거리

        [Header("해골 기사 - 추적")]
        [SerializeField] float _chaseSpeed = 5f;            // 추적 이동 속도

        [Header("해골 기사 - 3타 콤보")]
        [SerializeField] float _comboDamage1   = 22f;       // 1타 데미지
        [SerializeField] float _comboDamage2   = 22f;       // 2타 데미지
        [SerializeField] float _comboDamage3   = 33f;       // 3타 피니셔 데미지
        [SerializeField] float _comboInterval  = 0.3f;      // 콤보 타격 사이 딜레이 (초)
        [SerializeField] float _attackCooldown = 1.5f;      // 콤보 완료 후 대기 (초)

        [Header("해골 기사 - 방패")]
        [SerializeField] float _shieldHpThreshold  = 0.5f;  // 방패 발동 HP 비율 (0~1)
        [SerializeField] float _shieldDamageReduce = 0.5f;  // 정면 피해 감소율
        [SerializeField] float _parryWindow        = 0.15f; // 방패 발동 직후 패링 가능 윈도우 (초)

        [Header("해골 기사 - 돌진")]
        [SerializeField] float _chargeMinDist   = 4f;       // 돌진 발동 최소 거리
        [SerializeField] float _chargeMaxDist   = 8f;       // 돌진 발동 최대 거리
        [SerializeField] float _chargeWindup    = 1f;       // 돌진 선딜 (초)
        [SerializeField] float _chargeDamage    = 35f;      // 돌진 데미지
        [SerializeField] float _chargeKnockback = 8f;       // 돌진 넉백 강도
        [SerializeField] float _chargeSpeed     = 12f;      // 돌진 이동 속도
        [SerializeField] float _chargeDuration  = 0.35f;    // 돌진 지속 시간 (초)

        [Header("해골 기사 - 드롭")]
        [SerializeField] int _goldDrop = 20;                // GDD 9.3 사망 시 골드 드롭

        // 방패를 든 상태 (외부에서 PlayerCombat 패링 판정에 활용)
        public bool IsShielding   { get; private set; }
        // 방패 발동 직후 패링 가능 윈도우 활성 여부
        public bool IsParryWindow { get; private set; }

        bool _shieldActivated;  // 방패 최초 1회 트리거 여부
        bool _isAttacking;
        bool _isCharging;
        int  _waypointIndex;    // 현재 목표 웨이포인트 인덱스

        // ── 상태 진입 ────────────────────────────────────────────────────

        // Idle: 웨이포인트 순찰. 미설정 시 제자리 고정
        protected override void OnIdle()
        {
            StopAllCoroutines();
            _isAttacking = false;
            _isCharging  = false;

            if (_waypoints != null && _waypoints.Length >= 2)
            {
                agent.speed = _patrolSpeed;
                StartCoroutine(PatrolRoutine());
            }
            else
            {
                agent.isStopped = true;
            }
        }

        // Chase: 빠른 추적 (목적지 갱신은 Update에서 처리)
        protected override void OnChase()
        {
            StopAllCoroutines();
            _isAttacking    = false;
            _isCharging     = false;
            agent.speed     = _chaseSpeed;
            agent.isStopped = false;
        }

        // Attack: 거리에 따라 돌진 or 3타 콤보 선택
        protected override void OnAttack()
        {
            if (_isAttacking || _isCharging) return;
            StartCoroutine(AttackDecisionRoutine());
        }

        // Dead: 골드 드롭 → 2초 후 제거
        protected override void OnDead()
        {
            StopAllCoroutines();
            agent.isStopped = true;
            IsShielding     = false;
            IsParryWindow   = false;

            GoldManager.Instance?.AddGold(_goldDrop);
            Destroy(gameObject, 2f);
        }

        // ── 피해 처리 ────────────────────────────────────────────────────

        // 방패 상태일 때 정면 피해 50% 감소 후 base로 위임
        public override void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (IsShielding)
            {
                // hitPoint → 자신 방향 벡터와 forward 내적이 양수 = 정면 공격
                Vector3 toAttacker = (hitPoint - transform.position).normalized;
                if (Vector3.Dot(transform.forward, toAttacker) > 0f)
                    damage *= 1f - _shieldDamageReduce;
            }

            base.TakeDamage(damage, hitPoint);
        }

        // ── 업데이트 ─────────────────────────────────────────────────────

        protected override void Update()
        {
            base.Update();

            // HP 50% 이하 최초 1회 방패 발동
            if (!_shieldActivated && currentHp <= maxHp * _shieldHpThreshold)
            {
                _shieldActivated = true;
                StartCoroutine(ActivateShieldRoutine());
            }

            // Chase: 목적지 매 프레임 갱신 + 돌진 사거리 진입 시 돌진
            if (currentState == EnemyState.Chase && playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);

                if (!_isCharging && dist >= _chargeMinDist && dist <= _chargeMaxDist)
                    StartCoroutine(ChargeRoutine());
                else if (!_isCharging)
                    agent.SetDestination(playerTransform.position);
            }
        }

        // ── 코루틴 ───────────────────────────────────────────────────────

        // 웨이포인트 0 ↔ 1 왕복 순찰
        IEnumerator PatrolRoutine()
        {
            agent.isStopped = false;

            while (currentState == EnemyState.Idle)
            {
                agent.SetDestination(_waypoints[_waypointIndex].position);

                // 경로 계산 완료 + 도착 거리 기준 대기
                yield return new WaitUntil(() =>
                    !agent.pathPending &&
                    agent.remainingDistance <= _waypointStopDist);

                _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
            }
        }

        // 거리에 따라 돌진 or 3타 콤보 결정
        IEnumerator AttackDecisionRoutine()
        {
            if (playerTransform == null) yield break;

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (!_isCharging && dist >= _chargeMinDist && dist <= _chargeMaxDist)
                yield return StartCoroutine(ChargeRoutine());
            else
                yield return StartCoroutine(ComboRoutine());
        }

        // 3타 콤보: 22 / 22 / 33 데미지, 0.3초 간격
        IEnumerator ComboRoutine()
        {
            _isAttacking    = true;
            agent.isStopped = true;

            float[] damages = { _comboDamage1, _comboDamage2, _comboDamage3 };

            foreach (float dmg in damages)
            {
                if (playerTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, playerTransform.position);
                    if (dist <= attackRange && playerTransform.TryGetComponent<IDamageable>(out var target))
                        target.TakeDamage(dmg, transform.position);
                }

                yield return new WaitForSeconds(_comboInterval);
            }

            yield return new WaitForSeconds(_attackCooldown);

            _isAttacking    = false;
            agent.isStopped = false;

            if (currentState != EnemyState.Dead)
                ChangeState(EnemyState.Idle);
        }

        // 돌진: 선딜 1초 → NavMesh 위 고속 질주 → 범위 데미지 + 넉백
        IEnumerator ChargeRoutine()
        {
            if (playerTransform == null) yield break;

            _isCharging     = true;
            agent.isStopped = true;
            agent.ResetPath();

            // 선딜: 플레이어 방향 응시하며 대기
            float elapsed = 0f;
            while (elapsed < _chargeWindup)
            {
                if (playerTransform != null)
                {
                    Vector3 lookDir = playerTransform.position - transform.position;
                    lookDir.y = 0f;
                    if (lookDir != Vector3.zero)
                        transform.rotation = Quaternion.LookRotation(lookDir);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 돌진: agent.velocity 직접 설정으로 NavMesh 위 고속 이동
            Vector3 chargeDir = transform.forward;
            agent.isStopped   = false;

            float chargeTimer = 0f;
            while (chargeTimer < _chargeDuration)
            {
                agent.velocity = chargeDir * _chargeSpeed;
                chargeTimer   += Time.deltaTime;
                yield return null;
            }

            agent.velocity  = Vector3.zero;
            agent.isStopped = true;

            // 착탄 판정: 공격 범위 1.5배 이내 플레이어에게 데미지 + 넉백
            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange * 1.5f, playerLayer);
            foreach (var col in hits)
            {
                if (col.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(_chargeDamage, transform.position);

                // 넉백: CharacterController를 즉시 밀기
                // TODO: PlayerController에 ApplyKnockback() 추가 후 교체 권장
                if (col.TryGetComponent<CharacterController>(out var cc))
                {
                    Vector3 knockDir = (col.transform.position - transform.position).normalized;
                    cc.Move(knockDir * _chargeKnockback);
                }
            }

            yield return new WaitForSeconds(0.5f); // 돌진 후 경직

            agent.speed = _chaseSpeed;
            _isCharging = false;

            if (currentState != EnemyState.Dead)
                ChangeState(EnemyState.Idle);
        }

        // 방패 발동: 패링 윈도우 열기 → 닫기 → IsShielding 유지
        IEnumerator ActivateShieldRoutine()
        {
            IsShielding   = true;
            IsParryWindow = true;

            yield return new WaitForSeconds(_parryWindow);

            // 패링 윈도우 종료. IsShielding은 사망 전까지 유지
            IsParryWindow = false;
        }

        // Inspector에서 컴포넌트 추가 시 GDD 9.3 기본값 자동 설정
        void Reset()
        {
            maxHp        = 90f;
            attackDamage = 22f;
            moveSpeed    = 3f;
            detectRange  = 10f;
            attackRange  = 2f;
        }
    }
}
