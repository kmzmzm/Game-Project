using UnityEngine;

namespace Arcana.Core
{
    /// <summary>
    /// 데미지를 받을 수 있는 오브젝트에 구현한다.
    /// </summary>
    public interface IDamageable
    {
        /// <param name="damage">입힐 데미지 량</param>
        /// <param name="hitPoint">피격 위치 (이펙트·넉백 방향 계산에 활용)</param>
        void TakeDamage(float damage, Vector3 hitPoint);
    }

    /// <summary>
    /// 회복을 받을 수 있는 오브젝트에 구현한다.
    /// </summary>
    public interface IHealable
    {
        /// <param name="amount">회복할 HP 량</param>
        void Heal(float amount);
    }

    /// <summary>
    /// F키로 상호작용 가능한 오브젝트에 구현한다.
    /// </summary>
    public interface IInteractable
    {
        void Interact();
    }
}
