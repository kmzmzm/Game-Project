using System;
using UnityEngine;

namespace Arcana.Player
{
    /// <summary>
    /// 플레이어 자식 히트박스 오브젝트에 부착한다.
    /// SphereCollider 또는 BoxCollider(isTrigger) 중 하나를 함께 부착해야 한다.
    /// OnTriggerEnter를 PlayerCombat으로 중계하는 역할을 한다.
    /// (PlayerCombat과 콜라이더가 다른 GameObject에 있어서 이 컴포넌트로 분리함)
    /// </summary>
    public class AttackHitboxTrigger : MonoBehaviour
    {
        // SphereCollider 또는 BoxCollider 중 부착된 콜라이더를 자동으로 찾는다
        Collider _col;

        // 트리거에 충돌체가 진입하면 발행 — PlayerCombat.HandleHit()에서 구독
        public event Action<Collider> OnHit;

        void Awake()
        {
            _col = GetComponent<Collider>();

            if (_col == null)
            {
                Debug.LogWarning("[AttackHitboxTrigger] SphereCollider 또는 BoxCollider가 없습니다.", this);
                return;
            }

            _col.isTrigger = true;
            _col.enabled   = false; // 공격 시에만 활성화 — 시작 시 비활성화
        }

        /// <summary>
        /// 히트박스 콜라이더를 활성화 또는 비활성화한다.
        /// PlayerCombat의 HitboxActiveRoutine에서 호출한다.
        /// </summary>
        public void SetActive(bool active)
        {
            if (_col != null)
                _col.enabled = active;
        }

        // 트리거 진입 시 OnHit 이벤트 발행 — PlayerCombat이 데미지 처리
        void OnTriggerEnter(Collider other) => OnHit?.Invoke(other);
    }
}
