using UnityEngine;
using Unity.Cinemachine;

namespace Arcana.Core
{
    /// <summary>
    /// 셰이크 임펄스 강도 단계.
    /// </summary>
    public enum ShakeIntensity { Weak, Medium, Strong, VeryStrong }

    /// <summary>
    /// Cinemachine 3.x 기반 아이소메트릭 카메라 제어. 타겟 추적 및 셰이크 임펄스를 제공한다.
    /// CinemachineCamera에 CinemachineImpulseListener 컴포넌트가 있어야 셰이크가 반영된다.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Cinemachine 컴포넌트")]
        [SerializeField] CinemachineCamera    virtualCamera;
        [SerializeField] CinemachineImpulseSource impulseSource;

        [Header("아이소메트릭 고정 각도")]
        [SerializeField] float pitchAngle = 50f; // X축 — 아래를 내려보는 각도
        [SerializeField] float yawAngle   = 45f; // Y축 — 카메라 방위각

        [Header("셰이크 임펄스 강도")]
        [SerializeField] float weakForce       = 0.5f;
        [SerializeField] float mediumForce     = 1.0f;
        [SerializeField] float strongForce     = 2.0f;
        [SerializeField] float veryStrongForce = 3.5f;

        void Awake()
        {
            ApplyIsometricAngle();
        }

        // Inspector 수치 변경 시 씬 뷰에 즉시 반영
        void OnValidate()
        {
            ApplyIsometricAngle();
        }

        void ApplyIsometricAngle()
        {
            if (virtualCamera != null)
                virtualCamera.transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        }

        /// <summary>
        /// 플레이어 스폰 후 호출해 추적 타겟을 지정한다.
        /// 아이소메트릭 특성상 LookAt은 설정하지 않아 고정 각도를 유지한다.
        /// </summary>
        public void SetTarget(Transform target)
        {
            virtualCamera.Follow = target;
        }

        /// <summary>
        /// 지정한 강도로 카메라 셰이크 임펄스를 발생시킨다.
        /// </summary>
        public void TriggerShake(ShakeIntensity intensity)
        {
            float force = intensity switch
            {
                ShakeIntensity.Weak       => weakForce,
                ShakeIntensity.Medium     => mediumForce,
                ShakeIntensity.Strong     => strongForce,
                ShakeIntensity.VeryStrong => veryStrongForce,
            };

            impulseSource.GenerateImpulse(force);
        }
    }
}
