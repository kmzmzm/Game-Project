using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Arcana.Core;

namespace Arcana.Enemy
{
    /// <summary>
    /// GDD 9.3 슬라임 AI.
    /// Idle: NavMesh 위 랜덤 배회 / Chase: 플레이어 직선 추적
    /// Attack: 점프 후 내려찍기 (선딜 0.8초) / HP 30% 이하 격노(이동속도 1.5배)
    /// </summary>
    public class SlimeAI : EnemyBase
    {
        [Header("슬라임 - 배회")]
        [SerializeField] float wanderRadius   = 5f;
        [SerializeField] float wanderInterval = 3f;
        [SerializeField] float wanderSpeed    = 1.5f; // Idle 배회 속도 (moveSpeed보다 느리게)

        [Header("슬라임 - 공격")]
        [SerializeField] float attackWindup   = 0.8f; // 점프 선딜 (초)
        [SerializeField] float attackCooldown = 0.5f; // 공격 후 대기 (초)

        [Header("슬라임 - 드롭")]
        [SerializeField] int goldDrop = 5; // GDD 7.2 사망 시 골드 드롭량

        bool _isAttacking;
        bool _enraged; // HP 30% 이하 격노 상태 여부

        protected override void Update()
        {
            base.Update();

            // HP 30% 이하 최초 진입 시 이동속도 1.5배 적용
            if (!_enraged && currentHp > 0f && currentHp <= maxHp * 0.3f)
            {
                _enraged    = true;
                agent.speed = moveSpeed * 1.5f;
            }

            // Chase 중 매 프레임 플레이어 목적지 갱신
            if (currentState == EnemyState.Chase && playerTransform != null)
                agent.SetDestination(playerTransform.position);
        }

        protected override void OnIdle()
        {
            StopAllCoroutines();
            _isAttacking    = false;
            agent.isStopped = false;
            agent.speed     = _enraged ? moveSpeed * 1.5f : wanderSpeed;
            StartCoroutine(WanderRoutine());
        }

        protected override void OnChase()
        {
            StopAllCoroutines();
            _isAttacking    = false;
            agent.isStopped = false;
            agent.speed     = _enraged ? moveSpeed * 1.5f : moveSpeed;
        }

        protected override void OnAttack()
        {
            if (_isAttacking) return;
            StopAllCoroutines();
            StartCoroutine(JumpSlamRoutine());
        }

        protected override void OnDead()
        {
            StopAllCoroutines();
            agent.isStopped = true;
            // GDD 7.2 골드 드롭 — GoldManager 구현 후 연결
            Debug.Log($"[SlimeAI] 사망: 골드 {goldDrop} 드롭");
            Destroy(gameObject, 2f);
        }

        // Idle: NavMesh 위 랜덤 지점으로 느리게 배회
        IEnumerator WanderRoutine()
        {
            while (currentState == EnemyState.Idle)
            {
                Vector3 randomDir = Random.insideUnitSphere * wanderRadius + transform.position;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDir, out hit, wanderRadius, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);

                yield return new WaitForSeconds(wanderInterval);
            }
        }

        // Attack: 선딜 후 플레이어에게 내려찍기 데미지 적용
        IEnumerator JumpSlamRoutine()
        {
            _isAttacking    = true;
            agent.isStopped = true;

            // 점프 선딜 대기 (애니메이션 이벤트 연결 전 시간 기반 처리)
            yield return new WaitForSeconds(attackWindup);

            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= attackRange)
                {
                    if (playerTransform.TryGetComponent(out IDamageable target))
                        target.TakeDamage(attackDamage, playerTransform.position);
                }
            }

            agent.isStopped = false;
            yield return new WaitForSeconds(attackCooldown);

            _isAttacking = false;
            if (currentState != EnemyState.Dead)
                ChangeState(EnemyState.Idle);
        }

#if UNITY_EDITOR
        // Inspector에 컴포넌트 추가 시 GDD 9.3 기본값 자동 적용
        void Reset()
        {
            maxHp        = 30f;
            attackDamage = 8f;
            moveSpeed    = 3f;
            detectRange  = 8f;
            attackRange  = 1.5f;
        }
#endif
    }
}
