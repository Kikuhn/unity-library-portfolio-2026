using System;
using Pan.Event;



namespace Pan.Tan.PanEvent
{
    /// <summary>
    /// generation-safe Tan 접근과 PanEvent EventAble 접근을 묶는 gameplay 경계입니다.
    /// </summary>
    public interface IEventTan : IEventAble
    {
        TanHandle Tan { get; }
    }



    /// <summary>
    /// Tan fact를 해당 Tan의 local EventAble에만 전달하는 값 형식 envelope입니다.
    /// </summary>
    public readonly struct TanFactSignal
    {
        public TanFactSignal(in TanFact fact) => Fact = fact;
        public TanFact Fact { get; }
    }



    /// <summary>
    /// 한 Tan에 필요한 EventValue 묶음을 원자적으로 조립하는 설치 함수입니다.
    /// </summary>
    public delegate bool EventTanLoadoutInstaller(
        in EventTanHandle tan,
        out string failureReason);



    /// <summary>
    /// generation-safe Tan handle과 lazy EventAble 접근을 결합한 gameplay용 값 형식 handle입니다.
    /// </summary>
    public readonly struct EventTanHandle : IEventTan, IEquatable<EventTanHandle>
    {
        internal EventTanHandle(EventTanHostRegistry registry, TanHandle tan)
        {
            Registry = registry;
            Tan = tan;
        }



        private EventTanHostRegistry Registry { get; }
        public TanHandle Tan { get; }
        public TanKey Key => Tan.Key;
        public bool IsAlive => Registry != null && Registry.IsHandleAlive(this);



        /// <summary>
        /// 살아 있고 registry 소유권이 일치하는 Tan에 대응하는 lazy EventAble을 반환합니다.
        /// </summary>
        public EventAble EventAble
        {
            get
            {
                if (!IsAlive)
                {
                    throw new InvalidOperationException($"stale Tan handle에서는 EventAble을 사용할 수 없습니다: {Key}");
                }

                return Registry.GetOrCreateEventAble(this);
            }
        }



        public bool TryGetSnapshot(out TanSnapshot snapshot) => Tan.TryGetSnapshot(out snapshot);

        /// <summary>
        /// 이미 만들어진 EventAble만 반환하며 host나 table을 새로 할당하지 않습니다.
        /// </summary>
        public bool TryGetAllocatedEventAble(out EventAble eventAble)
        {
            if (Registry != null)
            {
                return Registry.TryGetAllocatedEventAble(this, out eventAble);
            }

            eventAble = null;
            return false;
        }

        /// <summary>
        /// 아직 EventAble이 없는 살아 있는 Tan에 loadout을 원자적으로 조립합니다.
        /// 실패하면 이번 호출에서 붙인 모든 EventValue를 되돌리며 오염된 host는 재사용하지 않습니다.
        /// </summary>
        public bool TryApplyLoadout(
            EventTanLoadoutInstaller installer,
            out string failureReason)
        {
            if (Registry != null)
            {
                return Registry.TryApplyLoadout(this, installer, out failureReason);
            }

            failureReason = "EventTanRegistryMissing";
            return false;
        }

        public bool TrySubmit<TCommand>(in TCommand command)
            where TCommand : unmanaged, ITanCommand => Tan.TrySubmit(in command);

        public bool Equals(EventTanHandle other) =>
            ReferenceEquals(Registry, other.Registry) && Tan.Equals(other.Tan);

        public override bool Equals(object obj) => obj is EventTanHandle other && Equals(other);
        public override int GetHashCode() => Tan.GetHashCode();
        public static bool operator ==(EventTanHandle left, EventTanHandle right) => left.Equals(right);
        public static bool operator !=(EventTanHandle left, EventTanHandle right) => !left.Equals(right);
    }
}
