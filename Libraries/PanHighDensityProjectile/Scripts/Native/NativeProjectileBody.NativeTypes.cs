using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Pan.HighDensityProjectile
{
    public sealed partial class NativeProjectileBody
    {
        private static Vector3 ToVector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }



        /// <summary>
        /// NativeList 안의 위치와 수명만 병렬 갱신하는 Burst job입니다.
        /// Unity component, receiver, event 처리는 job 밖의 main-thread pipeline에서 수행합니다.
        /// </summary>
        [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
        private struct NativeProjectileMovementJob : IJobParallelFor
        {
            public NativeArray<NativeProjectileSlot> Slots;
            public float DeltaTime;



            /// <summary>한 native slot의 이전 위치를 저장하고, 남은 수명과 현재 위치를 갱신합니다.</summary>
            public void Execute(int index)
            {
                NativeProjectileSlot slot = Slots[index];
                slot.PreviousPosition = slot.Position;
                slot.RemainingLifetime -= DeltaTime;
                if (slot.RemainingLifetime > 0f)
                {
                    if (slot.UseGravity != 0)
                    {
                        slot.Velocity += slot.Gravity * DeltaTime;
                    }

                    float damping = slot.LinearDamping + slot.FrictionCoefficient;
                    if (damping > 0f)
                    {
                        slot.Velocity *= 1f / (1f + damping * DeltaTime);
                    }

                    slot.Position += slot.Velocity * DeltaTime;
                }

                Slots[index] = slot;
            }
        }



        /// <summary>
        /// job에서 직접 다룰 수 있도록 순수 값 타입으로 구성한 투사체 상태입니다.
        /// `Source` 같은 managed 참조는 같은 index의 sidecar 배열에 분리해 job safety를 유지합니다.
        /// </summary>
        private struct NativeProjectileSlot
        {
            public NativeProjectileSlot(
            int projectileId,
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
                Position = new float3(position.x, position.y, position.z);
                PreviousPosition = Position;
                Velocity = new float3(velocity.x, velocity.y, velocity.z);
                Radius = radius;
                RemainingLifetime = remainingLifetime;
                InitialLifetime = remainingLifetime;
                Damage = damage;
                TeamId = teamId;
                HitLayerMask = hitLayerMask;
                IncludeTriggers = includeTriggers ? (byte)1 : (byte)0;
                Use2D = use2D ? (byte)1 : (byte)0;
                VisualFrameIndex = visualFrameIndex;
                UseGravity = motion.UseGravity ? (byte)1 : (byte)0;
                Gravity = new float3(motion.Gravity.x, motion.Gravity.y, motion.Gravity.z);
                LinearDamping = Mathf.Max(0f, motion.LinearDamping);
                FrictionCoefficient = Mathf.Max(0f, motion.FrictionCoefficient);
            }



            public int ProjectileId;
            public float3 Position;
            public float3 PreviousPosition;
            public float3 Velocity;
            public float Radius;
            public float RemainingLifetime;
            public float InitialLifetime;
            public float Damage;
            public int TeamId;
            public int HitLayerMask;
            public byte IncludeTriggers;
            public byte Use2D;
            public int VisualFrameIndex;
            public byte UseGravity;
            public float3 Gravity;
            public float LinearDamping;
            public float FrictionCoefficient;



            /// <summary>main-thread query, render, event가 읽을 수 있는 공통 snapshot으로 변환합니다.</summary>
            public ProjectileSnapshot ToSnapshot()
            {
                return new ProjectileSnapshot(
                ProjectileId,
                ToVector3(Position),
                ToVector3(Velocity),
                Radius,
                RemainingLifetime,
                Damage,
                TeamId,
                Use2D != 0,
                VisualFrameIndex,
                InitialLifetime,
                new ProjectileMotionSpec(UseGravity != 0, ToVector3(Gravity), LinearDamping, FrictionCoefficient));
            }



            private static Vector3 ToVector3(float3 value)
            {
                return new Vector3(value.x, value.y, value.z);
            }
        }
    }
}
