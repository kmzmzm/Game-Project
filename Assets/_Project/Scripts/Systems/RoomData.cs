using UnityEngine;

namespace Arcana.Systems
{
    /// <summary>
    /// 스테이지를 구성하는 룸 종류.
    /// </summary>
    public enum RoomType { Start, Battle, Shop, Elite, Boss }

    /// <summary>
    /// 룸 한 칸의 메타데이터를 담는 ScriptableObject.
    /// Project 창에서 Create > Arcana > Room Data 로 생성한다.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomData", menuName = "Arcana/Room Data")]
    public class RoomData : ScriptableObject
    {
        [Header("룸 정보")]
        [SerializeField] RoomType  _roomType;   // 룸 종류
        [SerializeField] GameObject _roomPrefab; // 인스턴스화할 룸 프리팹

        public RoomType   RoomType   => _roomType;
        public GameObject RoomPrefab => _roomPrefab;
    }
}
