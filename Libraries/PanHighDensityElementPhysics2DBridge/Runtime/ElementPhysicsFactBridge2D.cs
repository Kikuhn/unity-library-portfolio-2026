using UnityEngine;



namespace Pan.HighDensityElement.Physics2DBridge
{
    /// <summary>
    /// PhysicsCore Element와 Legacy Physics2D Projection 사이에서 발생한 접촉을 양쪽 게임플레이 계층에 전달하는 표준 값 봉투입니다.
    /// </summary>
    public readonly struct ElementPhysicsFact2D
    {
        internal ElementPhysicsFact2D(
            in ElementFact fact,
            in Physics2DBridgeTarget target,
            long synchronizationSequence)
        {
            Fact = fact;
            Target = target;
            SynchronizationSequence = synchronizationSequence;
        }



        /// <summary>
        /// Job에서 생성되어 결정적으로 정렬된 원본 Element Fact입니다.
        /// </summary>
        public ElementFact Fact { get; }



        /// <summary>
        /// Projection target ID에서 복원한 Legacy Collider, Rigidbody와 owner입니다.
        /// </summary>
        public Physics2DBridgeTarget Target { get; }



        /// <summary>
        /// Legacy pose가 Core World에 마지막으로 투영된 동기화 순번입니다.
        /// </summary>
        public long SynchronizationSequence { get; }



        /// <summary>
        /// 발생한 접촉 또는 센서 수명 주기의 종류입니다.
        /// </summary>
        public ElementFactType Type => Fact.Type;



        /// <summary>
        /// Fact를 생성한 Core Element입니다.
        /// </summary>
        public ElementKey Element => Fact.Element;



        /// <summary>
        /// Core 대 Core 접촉일 때의 상대 Element입니다.
        /// </summary>
        public ElementKey TargetElement => Fact.TargetElement;



        public ElementFactFlags Flags => Fact.Flags;



        /// <summary>
        /// Core World에서 계산된 접촉 위치입니다.
        /// </summary>
        public Vector2 Position => new Vector2(Fact.Position.x, Fact.Position.y);



        /// <summary>
        /// Core World에서 계산된 접촉 법선입니다.
        /// </summary>
        public Vector2 Normal => new Vector2(Fact.Normal.x, Fact.Normal.y);



        public Vector2 SurfacePoint => new Vector2(Fact.SurfacePoint.x, Fact.SurfacePoint.y);
        public Vector2 SurfaceNormal => new Vector2(Fact.SurfaceNormal.x, Fact.SurfaceNormal.y);
        public Vector2 ImpactCenter => new Vector2(Fact.ImpactCenter.x, Fact.ImpactCenter.y);
        public bool StartedOverlapped => Fact.StartedOverlapped;
        public bool HasSurfacePoint => Fact.HasSurfacePoint;
        public bool HasSurfaceNormal => Fact.HasSurfaceNormal;
        public bool HasImpactCenter => Fact.HasImpactCenter;



        /// <summary>
        /// 현재 이동 구간에서 접촉이 발생한 0부터 1 사이의 비율입니다.
        /// </summary>
        public float TimeOfImpact => Fact.TimeOfImpact;



        /// <summary>
        /// 접촉한 Core 또는 Projection Shape가 trigger인지 나타냅니다.
        /// </summary>
        public bool IsTrigger => Fact.IsTrigger != 0;



        /// <summary>
        /// 같은 고정 스텝 안에서 Fact가 생성된 하위 단계 번호입니다.
        /// </summary>
        public ushort SubstepIndex => Fact.SubstepIndex;
    }



    /// <summary>
    /// Legacy GameObject가 PhysicsCore Element 접촉 사실을 받기 위한 최소 표준 계약입니다.
    /// </summary>
    public interface IElementPhysicsFactReceiver2D
    {
        /// <summary>
        /// 같은 논리 receiver와 Element가 한 물리 스텝에 여러 Collider로 접촉해도 한 번만 호출됩니다.
        /// </summary>
        void ReceiveElementPhysicsFact2D(in ElementPhysicsFact2D fact);
    }



    /// <summary>
    /// 표준 Fact를 Unity 콜백과 비슷한 Contact, Enter, Stay, Exit 메서드로 나누어 주는 선택형 어댑터입니다.
    /// </summary>
    public abstract class ElementPhysicsFactBehaviour2D : MonoBehaviour, IElementPhysicsFactReceiver2D
    {
        /// <summary>
        /// 표준 Fact를 타입별 가상 메서드로 분배합니다.
        /// </summary>
        public void ReceiveElementPhysicsFact2D(in ElementPhysicsFact2D fact)
        {
            switch (fact.Type)
            {
                case ElementFactType.Contact:
                    OnElementContact2D(in fact);
                    break;

                case ElementFactType.Enter:
                    OnElementEnter2D(in fact);
                    break;

                case ElementFactType.Stay:
                    OnElementStay2D(in fact);
                    break;

                case ElementFactType.Exit:
                    OnElementExit2D(in fact);
                    break;
            }
        }



        /// <summary>
        /// 일회성 접촉 Fact를 받습니다.
        /// </summary>
        protected virtual void OnElementContact2D(in ElementPhysicsFact2D fact)
        {
        }



        /// <summary>
        /// 센서 또는 지속 접촉의 진입 Fact를 받습니다.
        /// </summary>
        protected virtual void OnElementEnter2D(in ElementPhysicsFact2D fact)
        {
        }



        /// <summary>
        /// 센서 또는 지속 접촉의 유지 Fact를 받습니다.
        /// </summary>
        protected virtual void OnElementStay2D(in ElementPhysicsFact2D fact)
        {
        }



        /// <summary>
        /// 센서 또는 지속 접촉의 이탈 Fact를 받습니다.
        /// </summary>
        protected virtual void OnElementExit2D(in ElementPhysicsFact2D fact)
        {
        }
    }
}
