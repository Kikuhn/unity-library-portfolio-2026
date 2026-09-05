using System;
using Unity.Mathematics;



namespace Pan.HighDensityElement
{
    public sealed partial class ElementWorld
    {
        /// <summary>
        /// alive Kinematic Element에 선택형 방향 이동 Feature를 추가하거나 교체합니다.
        /// </summary>
        public bool TrySetDirectionalMotion(
            ElementKey key,
            in ElementDirectionalMotionFeature feature)
        {
            if (!TryGetKinematicFeatureState(key, out ElementRegistryEntry entry, out NativeElementState state))
            {
                return false;
            }

            if (feature.Mode == ElementDirectionalMotionMode.Straight)
            {
                if (directionalMotionFeatures.Remove(key))
                {
                    state.Capabilities &= ~ElementCapabilities.DirectionalMotion2D;
                    SetState(entry, state);
                    IncrementStructuralRevision();
                }
                return true;
            }

            bool added = directionalMotionFeatures.AddOrSet(key, in feature);
            state.Capabilities |= ElementCapabilities.DirectionalMotion2D;
            SetState(entry, state);
            if (added) { IncrementStructuralRevision(); }
            return true;
        }



        /// <summary>
        /// alive Element의 선택형 방향 이동 Feature를 조회합니다.
        /// </summary>
        public bool TryGetDirectionalMotion(
            ElementKey key,
            out ElementDirectionalMotionFeature feature)
        {
            if (IsAlive(key)) { return directionalMotionFeatures.TryGet(key, out feature); }
            feature = default;
            return false;
        }



        /// <summary>
        /// alive Element에서 선택형 방향 이동 Feature를 제거합니다.
        /// </summary>
        public bool RemoveDirectionalMotion(ElementKey key) => RemoveMotionFeature(
            key,
            directionalMotionFeatures,
            ElementCapabilities.DirectionalMotion2D);



        /// <summary>
        /// alive Kinematic Element에 선택형 사인파 이동 Feature를 추가하거나 교체합니다.
        /// </summary>
        public bool TrySetWaveMotion(ElementKey key, in ElementWaveMotionFeature feature)
        {
            if (!TryGetKinematicFeatureState(key, out ElementRegistryEntry entry, out NativeElementState state))
            {
                return false;
            }

            var native = new NativeElementWaveMotionState
            {
                Feature = feature
            };
            bool added = waveMotionFeatures.AddOrSet(key, in native);
            state.Capabilities |= ElementCapabilities.WaveMotion2D;
            SetState(entry, state);
            if (added) { IncrementStructuralRevision(); }
            return true;
        }



        /// <summary>
        /// alive Element의 선택형 사인파 이동 Feature를 조회합니다.
        /// </summary>
        public bool TryGetWaveMotion(ElementKey key, out ElementWaveMotionFeature feature)
        {
            if (IsAlive(key) && waveMotionFeatures.TryGet(key, out NativeElementWaveMotionState native))
            {
                feature = native.Feature;
                return true;
            }

            feature = default;
            return false;
        }



        /// <summary>
        /// alive Element에서 선택형 사인파 이동 Feature를 제거합니다.
        /// </summary>
        public bool RemoveWaveMotion(ElementKey key) => RemoveMotionFeature(
            key,
            waveMotionFeatures,
            ElementCapabilities.WaveMotion2D);



        /// <summary>
        /// alive Sprite Element에 이동 방향 또는 수동 표시 회전 Feature를 추가하거나 교체합니다.
        /// </summary>
        public bool TrySetVisualOrientation(
            ElementKey key,
            in ElementVisualOrientationFeature feature)
        {
            if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle != ElementLifecycle.Alive ||
                !TryGetState(key, out NativeElementState state) ||
                (state.Capabilities & ElementCapabilities.SpriteVisual2D) == 0)
            {
                return false;
            }

            var native = new NativeElementVisualOrientationState
            {
                Feature = feature
            };
            bool added = visualOrientationFeatures.AddOrSet(key, in native);
            state.Capabilities |= ElementCapabilities.VisualOrientation2D;
            SetState(entry, state);
            if (added) { IncrementStructuralRevision(); }
            return true;
        }



        /// <summary>
        /// alive Element의 선택형 표시 회전 Feature를 조회합니다.
        /// </summary>
        public bool TryGetVisualOrientation(
            ElementKey key,
            out ElementVisualOrientationFeature feature)
        {
            if (IsAlive(key) &&
                visualOrientationFeatures.TryGet(key, out NativeElementVisualOrientationState native))
            {
                feature = native.Feature;
                return true;
            }

            feature = default;
            return false;
        }



        /// <summary>
        /// alive Element에서 선택형 표시 회전 Feature를 제거합니다.
        /// </summary>
        public bool RemoveVisualOrientation(ElementKey key) => RemoveMotionFeature(
            key,
            visualOrientationFeatures,
            ElementCapabilities.VisualOrientation2D);



        /// <summary>
        /// alive Element에 공유 World 또는 View 경계 수명 Feature를 추가하거나 교체합니다.
        /// </summary>
        public bool TrySetBoundary(ElementKey key, in ElementBoundaryFeature feature)
        {
            if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle != ElementLifecycle.Alive ||
                !TryGetState(key, out NativeElementState state))
            {
                return false;
            }

            var native = new NativeElementBoundaryState
            {
                Feature = feature
            };
            bool added = boundaryFeatures.AddOrSet(key, in native);
            state.Capabilities |= ElementCapabilities.Boundary2D;
            SetState(entry, state);
            if (added) { IncrementStructuralRevision(); }
            return true;
        }



        /// <summary>
        /// alive Element의 선택형 공유 경계 수명 Feature를 조회합니다.
        /// </summary>
        public bool TryGetBoundary(ElementKey key, out ElementBoundaryFeature feature)
        {
            if (IsAlive(key) && boundaryFeatures.TryGet(key, out NativeElementBoundaryState native))
            {
                feature = native.Feature;
                return true;
            }

            feature = default;
            return false;
        }



        /// <summary>
        /// alive Element에서 선택형 공유 경계 수명 Feature를 제거합니다.
        /// </summary>
        public bool RemoveBoundary(ElementKey key) => RemoveMotionFeature(
            key,
            boundaryFeatures,
            ElementCapabilities.Boundary2D);



        /// <summary>
        /// 선택한 경계 종류가 참조할 공유 2D bounds를 다음 Tick 전에 교체합니다.
        /// </summary>
        public void SetBoundaryBounds2D(ElementBoundaryMode2D mode, in ElementBounds2D bounds)
        {
            ThrowIfDisposed();
            switch (mode)
            {
                case ElementBoundaryMode2D.WorldBounds:
                    worldBoundaryBounds = bounds;
                    hasWorldBoundaryBounds = 1;
                    break;
                case ElementBoundaryMode2D.ViewBounds:
                    viewBoundaryBounds = bounds;
                    hasViewBoundaryBounds = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }



        /// <summary>
        /// 선택한 경계 종류에 현재 등록된 공유 2D bounds를 조회합니다.
        /// </summary>
        public bool TryGetBoundaryBounds2D(ElementBoundaryMode2D mode, out ElementBounds2D bounds)
        {
            ThrowIfDisposed();
            switch (mode)
            {
                case ElementBoundaryMode2D.WorldBounds:
                    bounds = worldBoundaryBounds;
                    return hasWorldBoundaryBounds != 0;
                case ElementBoundaryMode2D.ViewBounds:
                    bounds = viewBoundaryBounds;
                    return hasViewBoundaryBounds != 0;
                default:
                    bounds = default;
                    return false;
            }
        }



        /// <summary>
        /// 선택한 공유 경계를 제거해 해당 경계 Feature의 소거 판정을 일시 중단합니다.
        /// </summary>
        public void ClearBoundaryBounds2D(ElementBoundaryMode2D mode)
        {
            ThrowIfDisposed();
            switch (mode)
            {
                case ElementBoundaryMode2D.WorldBounds:
                    hasWorldBoundaryBounds = 0;
                    break;
                case ElementBoundaryMode2D.ViewBounds:
                    hasViewBoundaryBounds = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }



        private bool TryGetKinematicFeatureState(
            ElementKey key,
            out ElementRegistryEntry entry,
            out NativeElementState state)
        {
            if (TryGetRegistryEntry(key, out entry) &&
                entry.Lifecycle == ElementLifecycle.Alive &&
                TryGetState(key, out state) &&
                (state.Capabilities & ElementCapabilities.KinematicMotion2D) != 0 &&
                (state.Capabilities & ElementCapabilities.DynamicBody2D) == 0)
            {
                return true;
            }

            entry = default;
            state = default;
            return false;
        }



        private bool RemoveMotionFeature<T>(
            ElementKey key,
            ElementSparseFeatureStore<T> store,
            ElementCapabilities capability)
            where T : unmanaged
        {
            if (!TryGetRegistryEntry(key, out ElementRegistryEntry entry) ||
                entry.Lifecycle != ElementLifecycle.Alive ||
                !store.Remove(key) ||
                !TryGetState(key, out NativeElementState state))
            {
                return false;
            }

            state.Capabilities &= ~capability;
            SetState(entry, state);
            IncrementStructuralRevision();
            return true;
        }
    }
}
