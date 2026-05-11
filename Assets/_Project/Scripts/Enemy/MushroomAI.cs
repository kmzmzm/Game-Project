using System.Collections;
using UnityEngine;
using Arcana.Core;
using Arcana.Systems;

namespace Arcana.Enemy
{
    /// <summary>
    /// 버섯 몬스터 AI (GDD 9.3)
    /// Idle: 제자리 고정 / Chase: 5m 거리 유지 후퇴 / Attack: 포자 3방향 투척
    /// 특수: 사망 시 포자 폭발 (범위 2m, 데미지 10)
    /// </summary>
    public class MushroomAI : EnemyBase
    {
        [Header("버섯 - 유지 거리")]
        [SerializeField] float keepDistance = 5f;      // 플레이어와 유지할 후퇴 기준 거리

        [Header("버섯 - 포자 공격")]
        [SerializeField] float sporeAngle    = 30f;    // 3방향 포자 좌우 각도 간격
        [SerializeField] float sporeHitRadius = 0.5f;  // 포자 착탄 판정 반경 (OverlapSphere)
        [SerializeField] float attackCooldown = 2f;    // 포자 공격 간격 (초)

        [Header("버섯 - 사망 포자 폭발")]
        [SerializeField] float deathSporeRange  = 2f;  // 사망 시 포자 폭발 범위
        [SerializeField] float deathSporeDamage = 10f; // 사망 포자 데미지

        [Header("버섯 - 드롭")]
        [SerializeField] int goldDrop = 10;            // GDD 9.3 사망 시 골드 드롭

        bool isAttacking;

        // Idle: 제자리 고정
        protected override void OnIdle()
        {
            StopAllCoroutines();
            isAttacking = false;
            agent.isStopped = true;
        }

        // Chase: 후퇴 이동 로직은 Update에서 매 프레임 처리
        protected override void OnChase()
        {
            StopAllCoroutines();
            isAttacking = false;
            agent.isStopped = false;
        }

        // Attack: 포자 공격 코루틴 시작 (중복 실행 방지)
        protected override void OnAttack()
        {
            if (isAttacking) return;
            StartCoroutine(SporeAttackRoutine());
        }

        // Dead: 포자 폭발 → 골드 드롭 → 오브젝트 제거
        protected override void OnDead()
        {
            StopAllCoroutines();
            agent.isStopped = true;

            DeathSporeExplosion();
            GoldManager.Instance?.AddGold(goldDrop);

            Destroy(gameObject, 2f);
        }

        protected override void Update()
        {
            base.Update();

            // Chase / Attack 공통: keepDistance보다 가까우면 플레이어 반대 방향으로 후퇴
            if ((currentState == EnemyState.Chase || currentState == EnemyState.Attack)
                && playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);

                if (dist < keepDistance)
                {
                    // 플레이어 반대 방향으로 keepDistance + 버퍼 지점을 목적지로 설정
                    Vector3 retreatDest = playerTransform.position
                        + (transform.position - playerTransform.position).normalized
                        * (keepDistance + 0.5f);
                    agent.isStopped = false;
                    agent.SetDestination(retreatDest);
                }
                else
                {
                    // 적정 거리 유지 중이면 이동 정지
                    agent.isStopped = true;
                }
            }
        }

        // 포자 3방향 투척 코루틴 — 2초 간격으로 반복
        IEnumerator SporeAttackRoutine()
        {
            isAttacking = true;

            while (currentState == EnemyState.Attack)
            {
                LaunchSpores();
                yield return new WaitForSeconds(attackCooldown);
            }

            isAttacking = false;
        }

        // 플레이어 방향 기준 3방향 포자 투척 (임시 Physics.OverlapSphere 방식)
        // TODO: 나중에 투사체 프리팹 방식으로 교체
        void LaunchSpores()
        {
            if (playerTransform == null) return;

            Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
            float[] angles = { -sporeAngle, 0f, sporeAngle };

            foreach (float angle in angles)
            {
                Vector3 dir      = Quaternion.AngleAxis(angle, Vector3.up) * toPlayer;
                Vector3 hitCenter = transform.position + dir * attackRange;

                Collider[] hits = Physics.OverlapSphere(hitCenter, sporeHitRadius, playerLayer);
                foreach (var col in hits)
                {
                    if (col.TryGetComponent<IDamageable>(out var target))
                        target.TakeDamage(attackDamage, hitCenter);
                }
            }
        }

        // 사망 시 자신 위치 중심으로 포자 폭발
        void DeathSporeExplosion()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, deathSporeRange, playerLayer);
            foreach (var col in hits)
            {
                if (col.TryGetComponent<IDamageable>(out var target))
                    target.TakeDamage(deathSporeDamage, transform.position);
            }
        }

        // Inspector에서 컴포넌트 추가 시 GDD 9.3 기본값 자동 설정
        void Reset()
        {
            maxHp        = 55f;
            attackDamage = 14f;
            moveSpeed    = 2f;
            detectRange  = 10f;
            attackRange  = 6f;
        }
    }
}
