using UnityEngine;

namespace Arcana.Systems
{
    /// <summary>스킬 활성화 방식</summary>
    public enum SkillType { Active, Passive }

    /// <summary>
    /// 스킬 한 종류의 데이터를 담는 ScriptableObject.
    /// Project 창에서 Create > Arcana > Skill Data 로 생성한다.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "Arcana/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] string    _skillName;    // 스킬 이름
        [SerializeField][TextArea]
                         string    _description;  // 스킬 설명
        [SerializeField] SkillType _skillType;    // 스킬 타입 (액티브 / 패시브)

        [Header("전투 수치")]
        [SerializeField] float _cooldown;         // 쿨다운 (초), Passive면 0
        [SerializeField] float _staminaCost;      // 스태미나 소모량
        [SerializeField] float _damageMultiplier; // 데미지 배율 (1.0 = 기본 공격력 100%)

        [Header("에셋")]
        [SerializeField] Sprite _icon;            // UI 아이콘

        public string    SkillName        => _skillName;
        public string    Description      => _description;
        public SkillType SkillType        => _skillType;
        public float     Cooldown         => _cooldown;
        public float     StaminaCost      => _staminaCost;
        public float     DamageMultiplier => _damageMultiplier;
        public Sprite    Icon             => _icon;
    }
}
