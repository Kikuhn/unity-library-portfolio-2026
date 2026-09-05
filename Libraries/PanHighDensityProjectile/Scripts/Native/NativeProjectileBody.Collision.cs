using UnityEngine;

namespace Pan.HighDensityProjectile
{
    public sealed partial class NativeProjectileBody
    {
        private bool TryHit(int index, in NativeProjectileSlot slot)
        {
            ProjectileInteractionRequest request = CreateInteractionRequest(index, in slot);
            ProjectileInteractionResult result = EnsureInteractionPipeline().Resolve(in request);
            return result.Consumed;
        }



        private ProjectileInteractionRequest CreateInteractionRequest(int index, in NativeProjectileSlot slot)
        {
            return new ProjectileInteractionRequest(
            slot.ToSnapshot(),
            sourceSidecar[index],
            this,
            slot.HitLayerMask,
            ToVector3(slot.PreviousPosition),
            ToVector3(slot.Position),
            consumePhysicsHitWithoutReceiver,
            slot.IncludeTriggers != 0);
        }



        private void RemoveAt(int index)
        {
            NativeProjectileSlot removedSlot = slots[index];
            ProjectileSnapshot removedSnapshot = removedSlot.ToSnapshot();
            int lastIndex = slots.Length - 1;

            if (index != lastIndex)
            {
                sourceSidecar[index] = sourceSidecar[lastIndex];
            }

            sourceSidecar[lastIndex] = null;
            slots.RemoveAtSwapBack(index);
            totalDespawned++;
            EnsureInteractionPipeline().NotifyDespawned(in removedSnapshot);
        }



        /// <summary>
        /// hit receiver가 callback 중 `Despawn`/`ClearAll`로 native storage를 바꾼 경우, 같은 슬롯을 다시 제거하지 않기 위한 재진입 방어입니다.
        /// </summary>
        private bool RemoveAtIfSlotStillMatches(int index, int projectileId)
        {
            if (index < 0 || index >= slots.Length)
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



    }
}
