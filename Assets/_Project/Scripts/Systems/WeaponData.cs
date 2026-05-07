using System.Collections.Generic;
using UnityEngine;

namespace Arcana.Systems
{
    /// <summary>무기 종류</summary>
    public enum WeaponType { OldSword, OneSword, GreatSword, DualDagger }

    /// <summary>무기 등급</summary>
    public enum WeaponGrade { Common, Rare, Epic, Legendary }

    /// <summary>
    /// 무기 한 종류의 데이터를 담는 ScriptableObject.
    /// Project 창에서 Create > Arcana > Weapon Data 로 생성한다.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Arcana/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] string      _weaponName;   // 무기 이름
        [SerializeField][TextArea]
                         string      _description;  // 무기 설명
        [SerializeField] WeaponType  _weaponType;   // 무기 종류
        [SerializeField] WeaponGrade _weaponGrade;  // 무기 등급

        [Header("전투 스탯")]
        [SerializeField] float _attackDamage;       // 공격력
        [SerializeField] float _attackSpeed;        // 공격속도 (애니메이션 배속 배수, 1.0 = 기본)
        [SerializeField] float _staminaCost;        // 기본 공격 1회당 스태미나 소모량
        [SerializeField] float _moveSpeedModifier;  // 이동속도 보정 (1.0 = 기본, 0.9 = 10% 감소)

        [Header("스킬 풀")]
        [SerializeField] List<SkillData> _skillPool; // 스킬 풀 (3~5개 권장)

        [Header("에셋")]
        [SerializeField] GameObject _weaponPrefab; // 플레이어에 장착할 무기 프리팹
        [SerializeField] Sprite     _icon;          // UI 아이콘

        public string      WeaponName        => _weaponName;
        public string      Description       => _description;
        public WeaponType  WeaponType        => _weaponType;
        public WeaponGrade WeaponGrade       => _weaponGrade;
        public float       AttackDamage      => _attackDamage;
        public float       AttackSpeed       => _attackSpeed;
        public float       StaminaCost       => _staminaCost;
        public float       MoveSpeedModifier => _moveSpeedModifier;

        // 읽기 전용으로 노출 — 외부에서 직접 수정 불가
        public IReadOnlyList<SkillData> SkillPool => _skillPool;

        public GameObject WeaponPrefab => _weaponPrefab;
        public Sprite     Icon         => _icon;
    }
}
