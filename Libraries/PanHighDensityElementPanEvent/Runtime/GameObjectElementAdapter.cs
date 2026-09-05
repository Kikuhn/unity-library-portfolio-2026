using System;
using System.Threading;
using Pan.Event;
using Unity.Mathematics;
using UnityEngine;



namespace Pan.HighDensityElement.PanEvent
{
    /// <summary>
    /// 기존 GameObject/EventAble 객체를 native Element와 같은 게임플레이 경계로 노출하는 검증 및 이행용 adapter입니다.
    /// </summary>
    public sealed class GameObjectElementAdapter : IEventElement, IDisposable
    {
        private static int nextWorldId;

        private readonly IEventAble owner;
        private readonly Transform target;
        private readonly bool resetEventAbleOnDespawn;
        private readonly float radius;
        private float2 previousPosition;
        private float2 velocity;
        private float remainingLifetime;
        private int visualId;
        private ElementLifecycle lifecycle;



        public GameObjectElementAdapter(
            IEventAble owner,
            Transform target,
            ElementCapabilities capabilities,
            float radius,
            float lifetime,
            float2 velocity = default,
            bool resetEventAbleOnDespawn = false)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.target = target != null ? target : throw new ArgumentNullException(nameof(target));
            this.radius = math.max(0f, radius);
            this.velocity = velocity;
            this.resetEventAbleOnDespawn = resetEventAbleOnDespawn;
            remainingLifetime = math.max(0f, lifetime);
            Vector3 position = target.position;
            previousPosition = new float2(position.x, position.y);
            Capabilities = capabilities;
            Key = new ElementKey(Interlocked.Increment(ref nextWorldId), 0, 1);
            lifecycle = ElementLifecycle.Alive;
        }



        public ElementKey Key { get; }
        public ElementLifecycle Lifecycle => lifecycle;
        public ElementCapabilities Capabilities { get; }
        public ElementPhysicsCapability PhysicsCapabilities => ElementPhysicsCapability.None;
        public bool IsAlive => lifecycle == ElementLifecycle.Alive;
        public EventAble EventAble
        {
            get
            {
                if (!IsAlive) { throw new InvalidOperationException($"stale GameObject Element adapter입니다: {Key}"); }
                return owner.EventAble;
            }
        }



        public void Tick(float deltaTime)
        {
            if (!IsAlive) { return; }

            float safeDeltaTime = math.max(0f, deltaTime);
            Vector3 current = target.position;
            previousPosition = new float2(current.x, current.y);
            if ((Capabilities & ElementCapabilities.KinematicMotion2D) != 0)
            {
                current.x += velocity.x * safeDeltaTime;
                current.y += velocity.y * safeDeltaTime;
                target.position = current;
            }

            if ((Capabilities & ElementCapabilities.Lifetime) == 0 || remainingLifetime <= 0f) { return; }

            remainingLifetime = math.max(0f, remainingLifetime - safeDeltaTime);
            if (remainingLifetime > 0f) { return; }

            DispatchFact(ElementFactType.LifetimeExpired);
            Despawn();
        }



        public bool TryGetSnapshot(out ElementSnapshot snapshot)
        {
            if (!IsAlive)
            {
                snapshot = default;
                return false;
            }

            Vector3 position = target.position;
            snapshot = new ElementSnapshot(
                Key,
                lifecycle,
                Capabilities,
                PhysicsCapabilities,
                previousPosition,
                new float2(position.x, position.y),
                velocity,
                radius,
                remainingLifetime,
                visualId,
                0,
                0);
            return true;
        }



        public bool TrySubmit<TCommand>(in TCommand command)
            where TCommand : unmanaged, IElementCommand
        {
            if (!IsAlive) { return false; }

            ElementCommand value = command.ToElementCommand(Key);
            switch (value.Type)
            {
                case ElementCommandType.Despawn:
                    Despawn();
                    return true;
                case ElementCommandType.SetPosition2D:
                    previousPosition = value.Value;
                    target.position = new Vector3(value.Value.x, value.Value.y, target.position.z);
                    return true;
                case ElementCommandType.SetVelocity2D:
                    velocity = value.Value;
                    return true;
                case ElementCommandType.SetVisualId:
                    visualId = value.IntValue;
                    return true;
                default:
                    return false;
            }
        }



        public void Submit<TCommand>(in TCommand command)
            where TCommand : unmanaged, IElementCommand
        {
            if (!TrySubmit(in command))
            {
                throw new InvalidOperationException($"stale GameObject Element adapter에 명령을 제출했습니다: {Key}");
            }
        }



        public void Dispose()
        {
            if (IsAlive) { Despawn(); }
        }



        private void Despawn()
        {
            if (!IsAlive) { return; }

            DispatchFact(ElementFactType.Despawned);
            lifecycle = ElementLifecycle.Despawned;
            if (!resetEventAbleOnDespawn) { return; }

            try
            {
                owner.EventAble.Reset();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }



        private void DispatchFact(ElementFactType type)
        {
            Vector3 position = target.position;
            var fact = new ElementFact
            {
                Type = type,
                Element = Key,
                TargetId = -1,
                BridgeTargetId = -1,
                Position = new float2(position.x, position.y),
                SubstepIndex = 0
            };
            var signal = new ElementFactSignal(in fact);
            try
            {
                owner.EventAble.DispatchLocal(in signal);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
