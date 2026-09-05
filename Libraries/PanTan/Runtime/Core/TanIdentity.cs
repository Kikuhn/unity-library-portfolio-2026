using System;



namespace Pan.Tan
{
    /// <summary>
    /// 하나의 Tan runtime 안에서 slot 재사용을 generation으로 구분하는 값 형식 식별자입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanKey : IEquatable<TanKey>
    {
        public TanKey(int contextId, int slot, uint generation)
        {
            ContextId = contextId;
            Slot = slot;
            Generation = generation;
        }



        public int ContextId { get; }
        public int Slot { get; }
        public uint Generation { get; }
        public bool IsValid => ContextId > 0 && Slot >= 0 && Generation > 0;



        public bool Equals(TanKey other) =>
            ContextId == other.ContextId && Slot == other.Slot && Generation == other.Generation;

        public override bool Equals(object obj) => obj is TanKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ContextId;
                hash = (hash * 397) ^ Slot;
                return (hash * 397) ^ (int)Generation;
            }
        }

        public static bool operator ==(TanKey left, TanKey right) => left.Equals(right);
        public static bool operator !=(TanKey left, TanKey right) => !left.Equals(right);
        public override string ToString() => $"Tan({ContextId}:{Slot}:{Generation})";
    }



    /// <summary>
    /// Tan 접촉 대상의 identity 종류입니다.
    /// </summary>
    public enum TanTargetKind : byte
    {
        None = 0,
        Element = 1,
        LegacyPhysics2D = 2,
        External = 3
    }



    /// <summary>
    /// 대상 backend가 slot을 재사용해도 오래된 참조가 새 대상을 가리키지 않게 하는 값 형식 handle입니다.
    /// </summary>
    [Serializable]
    public readonly struct TanTargetHandle : IEquatable<TanTargetHandle>
    {
        public TanTargetHandle(TanTargetKind kind, int contextId, int slot, uint generation)
        {
            Kind = kind;
            Key = new TanKey(contextId, slot, generation);
        }

        public TanTargetHandle(TanTargetKind kind, in TanKey key)
        {
            Kind = kind;
            Key = key;
        }



        public TanTargetKind Kind { get; }
        public TanKey Key { get; }
        public bool IsValid => Kind != TanTargetKind.None && Key.IsValid;



        public uint ToStableOwnerId()
        {
            unchecked
            {
                uint hash = (uint)Key.ContextId;
                hash = hash * 397u ^ (uint)Key.Slot;
                hash = hash * 397u ^ Key.Generation;
                return hash == 0u ? 1u : hash;
            }
        }

        public bool Equals(TanTargetHandle other) => Kind == other.Kind && Key.Equals(other.Key);
        public override bool Equals(object obj) => obj is TanTargetHandle other && Equals(other);
        public override int GetHashCode() => ((int)Kind * 397) ^ Key.GetHashCode();
        public static bool operator ==(TanTargetHandle left, TanTargetHandle right) => left.Equals(right);
        public static bool operator !=(TanTargetHandle left, TanTargetHandle right) => !left.Equals(right);
        public override string ToString() => IsValid ? $"{Kind}:{Key}" : "TanTarget(None)";
    }
}
