using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Arcana.Core;

namespace Arcana.Enemy
{
    public enum EnemyState { Idle, Chase, Attack, Hurt, Dead }

    /// <summary>
    /// 모든 적 AI의 추상 베이스 클래스.
    /// 상태머신(Idle/Chase/Attack/Hurt/Dead), 피격, 스턴을 공통 처리한다.
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("스탯")]
        [SerializeField] protected float maxHp        = 100f;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float moveSpeed    = 3f;
        [SerializeField] protected float detectRange  = 10f;
        [SerializeField] protected float attackRange  = 1.5f;

        [Header("피격")]
        [SerializeField] float hurtRecovery = 0.3f; // Hurt → Idle 복귀 시간 (초)

        [Header("감지")]
        [SerializeField] LayerMask playerLayer;

        protected float        currentHp;
        protected NavMeshAgent agent;
        protected Transform    playerTransform;
        protected EnemyState   currentState;

        // 사망 시 발행
        public event Action OnDied;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
                Debug.LogWarning("[EnemyBase] NavMeshAgent 컴포넌트를 찾을 수 없습니다.", this);
        }

        protected virtual void Start()
        {
            currentHp   = maxHp;
            agent.speed = moveSpeed;
            ChangeState(EnemyState.Idle);
        }

        protected virtual void Update()
        {
            // Hurt · Dead 상태에서는 AI 판단 중단
            if (currentState == EnemyState.Dead || currentState == EnemyState.Hurt)
                return;

            DetectPlayer();
            UpdateStateMachine();
        }

        // Physics.OverlapSphere로 플레이어 레이어 감지
        void DetectPlayer()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectRange, playerLayer);
            playerTransform = hits.Length > 0 ? hits[0].transform : null;
        }

        // 거리 기반 상태 전이 — 이미 동일 상태면 전환하지 않음
        void UpdateStateMachine()
        {
            if (playerTransform == null)
            {
                if (currentState != EnemyState.Idle)
                    ChangeState(EnemyState.Idle);
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist <= attackRange)
            {
                if (currentState != EnemyState.Attack)
                    ChangeState(EnemyState.Attack);
            }
            else
            {
                if (currentState != EnemyState.Chase)
                    ChangeState(EnemyState.Chase);
            }
        }

        public void ChangeState(EnemyState newState)
        {
            currentState = newState;
            switch (newState)
            {
                case EnemyState.Idle:   OnIdle();   break;
                case EnemyState.Chase:  OnChase();  break;
                case EnemyState.Attack: OnAttack(); break;
                case EnemyState.Dead:   OnDead();   break;
                // Hurt는 TakeDamage · Stun에서 직접 제어
            }
        }

        // 방어력 없음 — 데미지 그대로 적용
        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (currentState == EnemyState.Dead) return;

            currentHp -= damage;

            if (currentHp <= 0f)
            {
                currentHp = 0f;
                ChangeState(EnemyState.Dead);
                OnDied?.Invoke();
                return;
            }

            // Hurt 상태 진입 후 hurtRecovery 초 뒤 자동 복귀
            ChangeState(EnemyState.Hurt);
            StartCoroutine(HurtRecoveryRoutine());
        }

        // 패링 성공 시 외부(PlayerCombat 등)에서 호출 — duration 동안 행동 정지
        public void Stun(float duration)
        {
            if (currentState == EnemyState.Dead) return;
            StartCoroutine(StunRoutine(duration));
        }

        // Hurt 복귀: hurtRecovery 초 후 Idle로 돌아감
        IEnumerator HurtRecoveryRoutine()
        {
            agent.isStopped = true;
            yield return new WaitForSeconds(hurtRecovery);
            if (currentState == EnemyState.Hurt)
            {
                agent.isStopped = false;
                ChangeState(EnemyState.Idle);
            }
        }

        // 스턴: duration 동안 Hurt 상태 유지 후 Idle 복귀
        IEnumerator StunRoutine(float duration)
        {
            currentState    = EnemyState.Hurt; // 콜백 없이 상태만 전환
            agent.isStopped = true;
            yield return new WaitForSeconds(duration);
            agent.isStopped = false;
            ChangeState(EnemyState.Idle);
        }

        protected abstract void OnIdle();
        protected abstract void OnChase();
        protected abstract void OnAttack();
        protected abstract void OnDead();

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // 노랑: 감지 범위 / 빨강: 공격 범위
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, detectRange);
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawSphere(transform.position, attackRange);
        }
#endif
    }
}
