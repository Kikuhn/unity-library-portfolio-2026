using UnityEngine;



namespace Pan.Tan
{
    /// <summary>
    /// backend가 처리할 명령 종류입니다.
    /// </summary>
    public enum TanCommandType : byte
    {
        None = 0,
        Despawn = 1,
        SetPosition2D = 2,
        SetVelocity2D = 3,
        SetRemainingLifetime = 4,
        RequestStrictCcd2D = 5,
        MarkTeleported2D = 6,
        ConfigureLifetime = 7,
        RemoveLifetime = 8,
        ConfigureLocalClock = 9,
        RemoveLocalClock = 10,
        SetVisualId = 11,
        SetColor = 12,
        SetVisualScale2D = 13,
        SetRotation2D = 14,
        ConfigureVisualOrientation = 15,
        RemoveVisualOrientation = 16,
        ConfigurePrimaryMotion = 17,
        RemovePrimaryMotion = 18,
        UpdateHomingTargetPosition2D = 19,
        ConfigureWaveMotion = 20,
        RemoveWaveMotion = 21,
        ConfigureBoundary2D = 22,
        UpdateBoundaryBounds2D = 23,
        RemoveBoundary2D = 24
    }



    /// <summary>
    /// generic command를 boxing 없이 backend 경계로 전달하는 값 형식 명령입니다.
    /// </summary>
    public readonly struct TanCommand
    {
        internal TanCommand(
            TanCommandType type,
            Vector2 vectorValue,
            float floatValue,
            int intValue,
            TanLifetimeDefinition lifetimeValue = default,
            Color32 colorValue = default,
            TanVisualOrientationDefinition visualOrientationValue = default,
            TanPrimaryMotionDefinition primaryMotionValue = default,
            TanWaveMotionDefinition waveMotionValue = default,
            TanBoundaryDefinition boundaryValue = default)
        {
            Type = type;
            VectorValue = vectorValue;
            FloatValue = floatValue;
            IntValue = intValue;
            LifetimeValue = lifetimeValue;
            ColorValue = colorValue;
            VisualOrientationValue = visualOrientationValue;
            PrimaryMotionValue = primaryMotionValue;
            WaveMotionValue = waveMotionValue;
            BoundaryValue = boundaryValue;
        }



        public TanCommandType Type { get; }
        public Vector2 VectorValue { get; }
        public float FloatValue { get; }
        public int IntValue { get; }
        public TanLifetimeDefinition LifetimeValue { get; }

        /// <summary>
        /// Color 변경 명령이 전달하는 값입니다.
        /// </summary>
        public Color32 ColorValue { get; }

        /// <summary>
        /// 표시 방향 Feature 구성 명령이 전달하는 값입니다.
        /// </summary>
        public TanVisualOrientationDefinition VisualOrientationValue { get; }

        /// <summary>
        /// 주 이동 Feature 구성 명령이 전달하는 값입니다.
        /// </summary>
        public TanPrimaryMotionDefinition PrimaryMotionValue { get; }

        /// <summary>
        /// Wave Feature 구성 명령이 전달하는 값입니다.
        /// </summary>
        public TanWaveMotionDefinition WaveMotionValue { get; }

        /// <summary>
        /// 경계 Feature 구성 또는 bounds 갱신 명령이 전달하는 값입니다.
        /// </summary>
        public TanBoundaryDefinition BoundaryValue { get; }
    }



    public interface ITanCommand
    {
        TanCommand ToTanCommand();
    }



    public readonly struct DespawnTan : ITanCommand
    {
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.Despawn, default, 0f, 0);
    }



    public readonly struct SetTanPosition2D : ITanCommand
    {
        public SetTanPosition2D(Vector2 position) => Position = position;
        public Vector2 Position { get; }
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.SetPosition2D, Position, 0f, 0);
    }



    public readonly struct SetTanVelocity2D : ITanCommand
    {
        public SetTanVelocity2D(Vector2 velocity) => Velocity = velocity;
        public Vector2 Velocity { get; }
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.SetVelocity2D, Velocity, 0f, 0);
    }



    public readonly struct SetTanRemainingLifetime : ITanCommand
    {
        public SetTanRemainingLifetime(float remainingLifetime) => RemainingLifetime = Mathf.Max(0f, remainingLifetime);
        public float RemainingLifetime { get; }
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.SetRemainingLifetime, default, RemainingLifetime, 0);
    }



    public readonly struct RequestTanStrictCcd2D : ITanCommand
    {
        public RequestTanStrictCcd2D(int fixedStepCount) => FixedStepCount = Mathf.Max(0, fixedStepCount);
        public int FixedStepCount { get; }
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.RequestStrictCcd2D, default, 0f, FixedStepCount);
    }



    public readonly struct MarkTanTeleported2D : ITanCommand
    {
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.MarkTeleported2D, default, 0f, 0);
    }



    public readonly struct ConfigureTanLifetime : ITanCommand
    {
        public ConfigureTanLifetime(in TanLifetimeDefinition lifetime) => Lifetime = lifetime;
        public TanLifetimeDefinition Lifetime { get; }
        public TanCommand ToTanCommand() =>
            new TanCommand(TanCommandType.ConfigureLifetime, default, 0f, 0, Lifetime);
    }



    public readonly struct RemoveTanLifetime : ITanCommand
    {
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.RemoveLifetime, default, 0f, 0);
    }



    public readonly struct ConfigureTanLocalClock : ITanCommand
    {
        public ConfigureTanLocalClock(float timeScale = 1f, bool paused = false)
        {
            TimeScale = Mathf.Max(0f, timeScale);
            Paused = paused;
        }

        public float TimeScale { get; }
        public bool Paused { get; }
        public TanCommand ToTanCommand() => new TanCommand(
            TanCommandType.ConfigureLocalClock,
            default,
            TimeScale,
            Paused ? 1 : 0);
    }



    public readonly struct RemoveTanLocalClock : ITanCommand
    {
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.RemoveLocalClock, default, 0f, 0);
    }



    /// <summary>
    /// 살아 있는 Tan이 사용할 사전 등록 VisualId를 변경합니다.
    /// </summary>
    public readonly struct SetTanVisualId : ITanCommand
    {
        public SetTanVisualId(int visualId) => VisualId = Mathf.Max(0, visualId);
        public int VisualId { get; }
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.SetVisualId, default, 0f, VisualId);
    }



    /// <summary>
    /// 공유 Sprite와 Material을 유지한 채 Tan 하나의 색을 변경합니다.
    /// </summary>
    public readonly struct SetTanColor : ITanCommand
    {
        public SetTanColor(Color32 color) => Color = color;
        public Color32 Color { get; }
        public TanCommand ToTanCommand() => new TanCommand(
            TanCommandType.SetColor,
            default,
            0f,
            0,
            colorValue: Color);
    }



    /// <summary>
    /// 충돌 반경과 독립적인 Tan 그래픽의 2D 크기를 변경합니다.
    /// </summary>
    public readonly struct SetTanVisualScale2D : ITanCommand
    {
        public SetTanVisualScale2D(Vector2 scale) => Scale = scale;
        public Vector2 Scale { get; }
        public TanCommand ToTanCommand() => new TanCommand(TanCommandType.SetVisualScale2D, Scale, 0f, 0);
    }



    /// <summary>
    /// Tan 그래픽의 수동 월드 회전을 degree 단위로 변경합니다.
    /// </summary>
    public readonly struct SetTanRotation2D : ITanCommand
    {
        public SetTanRotation2D(float rotationDegrees) =>
            RotationDegrees = float.IsFinite(rotationDegrees) ? rotationDegrees : 0f;

        public float RotationDegrees { get; }
        public TanCommand ToTanCommand() =>
            new TanCommand(TanCommandType.SetRotation2D, default, RotationDegrees, 0);
    }



    /// <summary>
    /// 이동 방향 정렬과 회전 애니메이션을 위한 선택형 표시 Feature를 추가하거나 교체합니다.
    /// </summary>
    public readonly struct ConfigureTanVisualOrientation : ITanCommand
    {
        public ConfigureTanVisualOrientation(in TanVisualOrientationDefinition definition) => Definition = definition;
        public TanVisualOrientationDefinition Definition { get; }
        public TanCommand ToTanCommand() => new TanCommand(
            TanCommandType.ConfigureVisualOrientation,
            default,
            0f,
            0,
            visualOrientationValue: Definition);
    }



    /// <summary>
    /// Tan의 선택형 표시 방향 Feature를 제거하고 현재 수동 회전을 유지합니다.
    /// </summary>
    public readonly struct RemoveTanVisualOrientation : ITanCommand
    {
        public TanCommand ToTanCommand() =>
            new TanCommand(TanCommandType.RemoveVisualOrientation, default, 0f, 0);
    }



    /// <summary>
    /// 각속도 회전 또는 Homing 주 이동 Feature를 추가하거나 교체합니다.
    /// Straight 정의는 Feature 제거와 같은 의미입니다.
    /// </summary>
    public readonly struct ConfigureTanPrimaryMotion : ITanCommand
    {
        public ConfigureTanPrimaryMotion(in TanPrimaryMotionDefinition definition) => Definition = definition;
        public TanPrimaryMotionDefinition Definition { get; }
        public TanCommand ToTanCommand() => new TanCommand(
            TanCommandType.ConfigurePrimaryMotion,
            default,
            0f,
            0,
            primaryMotionValue: Definition);
    }



    /// <summary>
    /// Tan의 선택형 주 이동 Feature를 제거하고 현재 속도로 직진시킵니다.
    /// </summary>
    public readonly struct RemoveTanPrimaryMotion : ITanCommand
    {
        public TanCommand ToTanCommand() =>
            new TanCommand(TanCommandType.RemovePrimaryMotion, default, 0f, 0);
    }



    /// <summary>
    /// consumer가 복원한 Homing 대상의 최신 월드 위치와 수명 상태를 전달합니다.
    /// </summary>
    public readonly struct UpdateTanHomingTargetPosition2D : ITanCommand
    {
        public UpdateTanHomingTargetPosition2D(Vector2 position, bool targetAlive = true)
        {
            Position = position;
            TargetAlive = targetAlive;
        }

        public Vector2 Position { get; }
        public bool TargetAlive { get; }
        public TanCommand ToTanCommand() => new TanCommand(
            TanCommandType.UpdateHomingTargetPosition2D,
            Position,
            0f,
            TargetAlive ? 1 : 0);
    }



    /// <summary>
    /// 진행 방향에 합성하는 선택형 사인파 이동 Feature를 추가하거나 교체합니다.
    /// </summary>
    public readonly struct ConfigureTanWaveMotion : ITanCommand
    {
        public ConfigureTanWaveMotion(in TanWaveMotionDefinition definition) => Definition = definition;
        public TanWaveMotionDefinition Definition { get; }
        public TanCommand ToTanCommand() => new TanCommand(
            TanCommandType.ConfigureWaveMotion,
            default,
            0f,
            0,
            waveMotionValue: Definition);
    }



    /// <summary>
    /// Tan의 선택형 사인파 이동 Feature를 제거합니다.
    /// </summary>
    public readonly struct RemoveTanWaveMotion : ITanCommand
    {
        public TanCommand ToTanCommand() =>
            new TanCommand(TanCommandType.RemoveWaveMotion, default, 0f, 0);
    }



    /// <summary>
    /// World 또는 Camera 영역 밖에서 Tan을 정리하는 선택형 경계 Feature를 구성합니다.
    /// </summary>
    public readonly struct ConfigureTanBoundary2D : ITanCommand
    {
        public ConfigureTanBoundary2D(in TanBoundaryDefinition definition) => Definition = definition;
        public TanBoundaryDefinition Definition { get; }
        public TanCommand ToTanCommand() => new TanCommand(
            TanCommandType.ConfigureBoundary2D,
            default,
            0f,
            0,
            boundaryValue: Definition);
    }



    /// <summary>
    /// 이미 구성된 경계 Feature의 현재 World 또는 Camera 영역을 갱신합니다.
    /// </summary>
    public readonly struct UpdateTanBoundaryBounds2D : ITanCommand
    {
        public UpdateTanBoundaryBounds2D(Rect bounds) => Bounds = bounds;
        public Rect Bounds { get; }
        public TanCommand ToTanCommand() => new TanCommand(
            TanCommandType.UpdateBoundaryBounds2D,
            default,
            0f,
            0,
            boundaryValue: new TanBoundaryDefinition(TanBoundaryMode.WorldBounds, Bounds));
    }



    /// <summary>
    /// Tan의 선택형 경계 수명 Feature를 제거합니다.
    /// </summary>
    public readonly struct RemoveTanBoundary2D : ITanCommand
    {
        public TanCommand ToTanCommand() =>
            new TanCommand(TanCommandType.RemoveBoundary2D, default, 0f, 0);
    }
}
