using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace Pan.HighDensityProjectile
{

    public sealed partial class ManagedProjectileBody
    {
        private bool TryHit(in ProjectileSlot slot, Vector3 nextPosition)
        {
            ProjectileInteractionRequest request = CreateInteractionRequest(in slot, nextPosition);
            ProjectileInteractionResult result = EnsureInteractionPipeline().Resolve(in request);
            return result.Consumed;
        }



        private ProjectileInteractionRequest CreateInteractionRequest(in ProjectileSlot slot, Vector3 nextPosition)
        {
            return new ProjectileInteractionRequest(
            slot.ToSnapshot(),
            slot.Source,
            this,
            slot.HitLayerMask,
            slot.Position,
            nextPosition,
            consumePhysicsHitWithoutReceiver,
            slot.IncludeTriggers);
        }



        private bool TrySimulateMovementOnlyWithJobs(float deltaTime)
        {
            if (!CanUseMovementOnlyJob())
            {
                lastMovementJobSimulatedCount = 0;
                return false;
            }

            int simulatedCount = activeCount;
            EnsureMovementJobCapacity(simulatedCount);
            for (int i = 0; i < simulatedCount; i++)
            {
                ProjectileSlot slot = slots[i];
                movementJobPositions[i] = ToFloat3(slot.Position);
                movementJobVelocities[i] = ToFloat3(slot.Velocity);
                movementJobRemainingLifetimes[i] = slot.RemainingLifetime;
                movementJobUseGravities[i] = slot.Motion.UseGravity ? (byte)1 : (byte)0;
                movementJobGravities[i] = ToFloat3(slot.Motion.Gravity);
                movementJobLinearDampings[i] = slot.Motion.LinearDamping;
                movementJobFrictionCoefficients[i] = slot.Motion.FrictionCoefficient;
            }

            MovementOnlyProjectileJob job = new MovementOnlyProjectileJob
            {
                Positions = movementJobPositions,
                Velocities = movementJobVelocities,
                RemainingLifetimes = movementJobRemainingLifetimes,
                UseGravities = movementJobUseGravities,
                Gravities = movementJobGravities,
                LinearDampings = movementJobLinearDampings,
                FrictionCoefficients = movementJobFrictionCoefficients,
                DeltaTime = deltaTime,
            };

            JobHandle handle = job.Schedule(simulatedCount, movementJobBatchSize);
            handle.Complete();

            lastMovementJobSimulatedCount = simulatedCount;
            for (int i = simulatedCount - 1; i >= 0; i--)
            {
                ProjectileSlot slot = slots[i];
                slot.RemainingLifetime = movementJobRemainingLifetimes[i];
                if (slot.RemainingLifetime <= 0f)
                {
                    RemoveAt(i);
                    continue;
                }

                slot.Position = ToVector3(movementJobPositions[i]);
                slot.Velocity = ToVector3(movementJobVelocities[i]);
                slots[i] = slot;
            }

            return true;
        }



        private bool TrySimulateMovementThenMainThreadCollision(float deltaTime)
        {
            int simulatedCount = activeCount;
            EnsureMovementJobCapacity(simulatedCount);
            for (int i = 0; i < simulatedCount; i++)
            {
                ProjectileSlot slot = slots[i];
                movementJobPositions[i] = ToFloat3(slot.Position);
                movementJobVelocities[i] = ToFloat3(slot.Velocity);
                movementJobRemainingLifetimes[i] = slot.RemainingLifetime;
                movementJobUseGravities[i] = slot.Motion.UseGravity ? (byte)1 : (byte)0;
                movementJobGravities[i] = ToFloat3(slot.Motion.Gravity);
                movementJobLinearDampings[i] = slot.Motion.LinearDamping;
                movementJobFrictionCoefficients[i] = slot.Motion.FrictionCoefficient;
            }

            MovementOnlyProjectileJob job = new MovementOnlyProjectileJob
            {
                Positions = movementJobPositions,
                Velocities = movementJobVelocities,
                RemainingLifetimes = movementJobRemainingLifetimes,
                UseGravities = movementJobUseGravities,
                Gravities = movementJobGravities,
                LinearDampings = movementJobLinearDampings,
                FrictionCoefficients = movementJobFrictionCoefficients,
                DeltaTime = deltaTime,
            };

            JobHandle handle = job.Schedule(simulatedCount, movementJobBatchSize);
            handle.Complete();

            lastMovementJobSimulatedCount = simulatedCount;
            for (int i = simulatedCount - 1; i >= 0; i--)
            {
                if (i >= activeCount)
                {
                    continue;
                }

                ProjectileSlot slot = slots[i];
                slot.RemainingLifetime = movementJobRemainingLifetimes[i];
                if (slot.RemainingLifetime <= 0f)
                {
                    RemoveAt(i);
                    continue;
                }

                Vector3 nextPosition = ToVector3(movementJobPositions[i]);
                slot.Velocity = ToVector3(movementJobVelocities[i]);
                // 이동 계산은 job 결과를 사용하되, hit payload 생성과 receiver 호출은 기존 main-thread 계약에 남긴다.
                if (TryHit(in slot, nextPosition))
                {
                    totalHits++;
                    RemoveAtIfSlotStillMatches(i, slot.ProjectileId);
                    continue;
                }

                slot.Position = nextPosition;
                TryWriteSlotIfStillMatches(i, slot.ProjectileId, in slot);
            }

            return true;
        }



        private bool CanUseMovementOnlyJob()
        {
            for (int i = 0; i < activeCount; i++)
            {
                if (slots[i].HitLayerMask != 0)
                {
                    return false;
                }
            }

            return true;
        }



        /// <summary>
        /// movement job이 사용할 native buffer capacity를 활성 투사체 수에 맞춥니다.
        /// buffer는 Persistent로 유지해 매 frame allocation 없이 이동과 수명만 병렬 갱신합니다.
        /// </summary>
        private void EnsureMovementJobCapacity(int requiredCapacity)
        {
            if (movementJobPositions.IsCreated && movementJobPositions.Length >= requiredCapacity)
            {
                return;
            }

            DisposeMovementJobBuffers();
            movementJobPositions = new NativeArray<float3>(requiredCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            movementJobVelocities = new NativeArray<float3>(requiredCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            movementJobRemainingLifetimes = new NativeArray<float>(requiredCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            movementJobUseGravities = new NativeArray<byte>(requiredCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            movementJobGravities = new NativeArray<float3>(requiredCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            movementJobLinearDampings = new NativeArray<float>(requiredCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            movementJobFrictionCoefficients = new NativeArray<float>(requiredCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }



        /// <summary>
        /// movement job용 native buffer를 해제합니다.
        /// manager가 재초기화되거나 파괴될 때만 호출해 runtime frame 중 dispose 비용이 나오지 않게 합니다.
        /// </summary>
        private void DisposeMovementJobBuffers()
        {
            if (movementJobPositions.IsCreated)
            {
                movementJobPositions.Dispose();
            }

            if (movementJobVelocities.IsCreated)
            {
                movementJobVelocities.Dispose();
            }

            if (movementJobRemainingLifetimes.IsCreated)
            {
                movementJobRemainingLifetimes.Dispose();
            }

            if (movementJobUseGravities.IsCreated)
            {
                movementJobUseGravities.Dispose();
            }

            if (movementJobGravities.IsCreated)
            {
                movementJobGravities.Dispose();
            }

            if (movementJobLinearDampings.IsCreated)
            {
                movementJobLinearDampings.Dispose();
            }

            if (movementJobFrictionCoefficients.IsCreated)
            {
                movementJobFrictionCoefficients.Dispose();
            }
        }



        private static float3 ToFloat3(Vector3 value)
        {
            return new float3(value.x, value.y, value.z);
        }



        private static Vector3 ToVector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }



        private void RemoveAt(int index)
        {
            ProjectileSnapshot removedSnapshot = slots[index].ToSnapshot();
            activeCount--;
            totalDespawned++;
            if (index < activeCount)
            {
                slots[index] = slots[activeCount];
            }

            slots[activeCount] = default;
            EnsureInteractionPipeline().NotifyDespawned(in removedSnapshot);
        }



        /// <summary>
        /// hit receiver가 callback 중 `Despawn`/`ClearAll`로 저장소를 바꾼 경우, 같은 슬롯을 다시 제거하지 않기 위한 재진입 방어입니다.
        /// </summary>
        private bool RemoveAtIfSlotStillMatches(int index, int projectileId)
        {
            if (index < 0 || index >= activeCount)
            {
                return false;
            }

            if (slots[index].ProjectileId != projectileId)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }



        /// <summary>
        /// callback 중 제거된 투사체를 simulation 끝에서 다시 써 넣지 않기 위한 재진입 방어입니다.
        /// </summary>
        private bool TryWriteSlotIfStillMatches(int index, int projectileId, in ProjectileSlot slot)
        {
            if (index < 0 || index >= activeCount)
            {
                return false;
            }

            if (slots[index].ProjectileId != projectileId)
            {
                return false;
            }

            slots[index] = slot;
            return true;
        }



        /// <summary>
        /// 투사체 이동과 수명 감소만 병렬 처리하는 job입니다.
        /// gameplay receiver 호출, Unity object 접근, event 발행은 모두 main thread pipeline에 남깁니다.
        /// </summary>
        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
        private struct MovementOnlyProjectileJob : IJobParallelFor
        {
            public NativeArray<float3> Positions;

            public NativeArray<float3> Velocities;

            public NativeArray<float> RemainingLifetimes;

            [Unity.Collections.ReadOnly] public NativeArray<byte> UseGravities;

            [Unity.Collections.ReadOnly] public NativeArray<float3> Gravities;

            [Unity.Collections.ReadOnly] public NativeArray<float> LinearDampings;

            [Unity.Collections.ReadOnly] public NativeArray<float> FrictionCoefficients;

            public float DeltaTime;



            /// <summary>한 투사체의 남은 수명을 줄이고 살아 있으면 위치를 갱신합니다.</summary>
            public void Execute(int index)
            {
                float remainingLifetime = RemainingLifetimes[index] - DeltaTime;
                RemainingLifetimes[index] = remainingLifetime;
                if (remainingLifetime <= 0f)
                {
                    return;
                }

                float3 velocity = Velocities[index];
                if (UseGravities[index] != 0)
                {
                    velocity += Gravities[index] * DeltaTime;
                }

                float damping = LinearDampings[index] + FrictionCoefficients[index];
                if (damping > 0f)
                {
                    velocity *= 1f / (1f + damping * DeltaTime);
                }

                Velocities[index] = velocity;
                Positions[index] += velocity * DeltaTime;
            }
        }



        /// <summary>
        /// managed `ManagedProjectileBody`가 내부 배열에 저장하는 투사체 슬롯입니다.
        /// 외부에는 이 구조를 노출하지 않고 `ProjectileSnapshot`만 넘겨 backend 교체 가능성을 유지합니다.
        /// </summary>
        private struct ProjectileSlot
        {
            public ProjectileSlot(
            int projectileId,
            Component source,
            Vector3 position,
            Vector3 velocity,
            float radius,
            float remainingLifetime,
            float damage,
            int teamId,
            int hitLayerMask,
            bool includeTriggers,
            bool use2D,
            int visualFrameIndex,
            ProjectileMotionSpec motion)
            {
                ProjectileId = projectileId;
                Source = source;
                Position = position;
                Velocity = velocity;
                Radius = radius;
                RemainingLifetime = remainingLifetime;
                InitialLifetime = remainingLifetime;
                Damage = damage;
                TeamId = teamId;
                HitLayerMask = hitLayerMask;
                IncludeTriggers = includeTriggers;
                Use2D = use2D;
                VisualFrameIndex = visualFrameIndex;
                Motion = motion;
            }



            public int ProjectileId;
            public Component Source;
            public Vector3 Position;
            public Vector3 Velocity;
            public float Radius;
            public float RemainingLifetime;
            public float InitialLifetime;
            public float Damage;
            public int TeamId;
            public int HitLayerMask;
            public bool IncludeTriggers;
            public bool Use2D;
            public int VisualFrameIndex;
            public ProjectileMotionSpec Motion;



            /// <summary>렌더링, 이벤트, target query가 읽을 수 있는 불변 snapshot으로 변환합니다.</summary>
            public ProjectileSnapshot ToSnapshot()
            {
                return new ProjectileSnapshot(
                ProjectileId,
                new KinematicCircle2DState(Position, Velocity, Radius, Use2D),
                new TimedLifetimeState(InitialLifetime, RemainingLifetime),
                new TeamRelationTag(TeamId),
                new HitDamageSpec(Damage),
                new SpriteVisualSpec(null, VisualFrameIndex, 0f),
                Motion);
            }
        }




    }
}
