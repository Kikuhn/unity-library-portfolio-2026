using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Pan.HighDensityProjectile;



namespace Pan.HighDensityProjectile.Tests
{
    public sealed class ProjectileRenderProxyInspectorEditModeTests
    {
        private const BindingFlags InstancePrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;



        [Test]
        public void ProjectileSpriteRenderProxy_OnValidateClampsSerializedSettingsWithoutCreatingPool()
        {
            GameObject targetObject = new GameObject("ProjectileSpriteRenderProxyOnValidateTarget");
            try
            {
                ProjectileSpriteRenderProxy renderProxy = targetObject.AddComponent<ProjectileSpriteRenderProxy>();

                SetPrivateField(renderProxy, "initialRendererCapacity", 0);
                SetPrivateField(renderProxy, "radiusToScale", -2f);
                SetPrivateField(renderProxy, "frameAnimationFramesPerSecond", -4f);

                InvokeOnValidate(renderProxy);

                Assert.AreEqual(1, GetPrivateField<int>(renderProxy, "initialRendererCapacity"));
                Assert.AreEqual(0f, GetPrivateField<float>(renderProxy, "radiusToScale"));
                Assert.AreEqual(0f, GetPrivateField<float>(renderProxy, "frameAnimationFramesPerSecond"));
                Assert.AreEqual(0, renderProxy.RendererCount);
                Assert.AreEqual(0, renderProxy.VisibleCount);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileInstancedSpriteRenderProxy_OnValidateClampsSerializedSettingsWithoutCreatingBuffers()
        {
            GameObject targetObject = new GameObject("ProjectileInstancedSpriteRenderProxyOnValidateTarget");
            try
            {
                ProjectileInstancedSpriteRenderProxy renderProxy =
                    targetObject.AddComponent<ProjectileInstancedSpriteRenderProxy>();

                SetPrivateField(renderProxy, "initialMatrixCapacity", 0);
                SetPrivateField(renderProxy, "radiusToScale", -3f);
                SetPrivateField(renderProxy, "frameAnimationFramesPerSecond", -5f);

                InvokeOnValidate(renderProxy);

                Assert.AreEqual(1, GetPrivateField<int>(renderProxy, "initialMatrixCapacity"));
                Assert.AreEqual(0f, GetPrivateField<float>(renderProxy, "radiusToScale"));
                Assert.AreEqual(0f, GetPrivateField<float>(renderProxy, "frameAnimationFramesPerSecond"));
                Assert.AreEqual(0, renderProxy.MatrixCapacity);
                Assert.AreEqual(0, renderProxy.VisibleCount);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileRenderProxies_UseClampedRadiusScaleWhenRenderingExistingProjectileBody()
        {
            GameObject targetObject = new GameObject("ProjectileRenderProxyRadiusScaleTarget");
            try
            {
                ManagedProjectileBody manager = targetObject.AddComponent<ManagedProjectileBody>();
                ProjectileSpriteRenderProxy spriteRenderProxy = targetObject.AddComponent<ProjectileSpriteRenderProxy>();
                ProjectileInstancedSpriteRenderProxy instancedRenderProxy =
                    targetObject.AddComponent<ProjectileInstancedSpriteRenderProxy>();

                manager.Initialize(2, 4);
                spriteRenderProxy.Initialize(manager, 1);
                instancedRenderProxy.Initialize(manager, 1);

                SetPrivateField(spriteRenderProxy, "radiusToScale", -2f);
                SetPrivateField(instancedRenderProxy, "radiusToScale", -3f);
                InvokeOnValidate(spriteRenderProxy);
                InvokeOnValidate(instancedRenderProxy);

                ProjectileSpawnRequest spawnData = new ProjectileSpawnRequest
                {
                    Source = manager,
                    Position = Vector3.one,
                    Velocity = Vector3.right,
                    Radius = 0.5f,
                    Lifetime = 1f,
                    Damage = 1f,
                    TeamId = 1,
                    HitLayers = 0,
                    Use2D = true,
                };
                manager.Spawn(in spawnData);

                Assert.AreEqual(1, spriteRenderProxy.RenderNow());
                SpriteRenderer renderer = targetObject.GetComponentInChildren<SpriteRenderer>(true);
                Assert.IsNotNull(renderer);
                Assert.AreEqual(new Vector3(0f, 0f, 1f), renderer.transform.localScale);

                Assert.AreEqual(1, instancedRenderProxy.BuildRenderData());
                Matrix4x4[] matrices = GetPrivateField<Matrix4x4[]>(instancedRenderProxy, "matrices");
                Assert.IsNotNull(matrices);
                Assert.AreEqual(0f, matrices[0].m00);
                Assert.AreEqual(0f, matrices[0].m11);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileSpriteRenderProxy_OnDisableHidesVisiblePooledRenderers()
        {
            GameObject targetObject = new GameObject("ProjectileSpriteRenderProxyDisableTarget");
            try
            {
                ProjectileRenderProxyBodyProbe body = new ProjectileRenderProxyBodyProbe();
                body.SetSnapshots(CreateSnapshot(1, Vector3.right, 0.5f));

                ProjectileSpriteRenderProxy renderProxy = targetObject.AddComponent<ProjectileSpriteRenderProxy>();
                renderProxy.Initialize(body, 1);

                Assert.AreEqual(1, renderProxy.RenderNow());
                SpriteRenderer renderer = targetObject.GetComponentInChildren<SpriteRenderer>(true);
                Assert.IsNotNull(renderer);
                Assert.IsTrue(renderer.enabled);

                InvokePrivateMethod(renderProxy, "OnDisable");

                Assert.AreEqual(0, renderProxy.VisibleCount);
                Assert.IsFalse(renderer.enabled);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileSpriteRenderProxy_UsesVisualFrameIndexForPooledSprites()
        {
            GameObject targetObject = new GameObject("ProjectileSpriteRenderProxyVisualFrameTarget");
            Sprite firstSprite = CreateTestSprite(Color.red);
            Sprite secondSprite = CreateTestSprite(Color.blue);
            try
            {
                ProjectileRenderProxyBodyProbe body = new ProjectileRenderProxyBodyProbe();
                body.SetSnapshots(
                    CreateSnapshot(1, Vector3.left, 0.25f, 1),
                    CreateSnapshot(2, Vector3.right, 0.25f, 2));

                ProjectileSpriteRenderProxy renderProxy = targetObject.AddComponent<ProjectileSpriteRenderProxy>();
                renderProxy.FrameSprites = new[] { firstSprite, secondSprite };
                renderProxy.Initialize(body, 2);

                Assert.AreEqual(2, renderProxy.RenderNow());

                SpriteRenderer[] renderers = targetObject.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.AreEqual(2, renderers.Length);
                Assert.AreSame(firstSprite, renderers[0].sprite);
                Assert.AreSame(secondSprite, renderers[1].sprite);
            }
            finally
            {
                DestroySprite(firstSprite);
                DestroySprite(secondSprite);
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileSpriteRenderProxy_AnimatesFrameSpritesFromSnapshotLifetime()
        {
            GameObject targetObject = new GameObject("ProjectileSpriteRenderProxyLifetimeFrameTarget");
            Sprite firstSprite = CreateTestSprite(Color.red);
            Sprite secondSprite = CreateTestSprite(Color.blue);
            try
            {
                ProjectileRenderProxyBodyProbe body = new ProjectileRenderProxyBodyProbe();
                body.SetSnapshots(CreateSnapshot(
                    1,
                    Vector3.zero,
                    0.25f,
                    1,
                    remainingLifetime: 0.4f,
                    initialLifetime: 1f));

                ProjectileSpriteRenderProxy renderProxy = targetObject.AddComponent<ProjectileSpriteRenderProxy>();
                renderProxy.FrameSprites = new[] { firstSprite, secondSprite };
                renderProxy.AnimateFramesByLifetime = true;
                renderProxy.FrameAnimationFramesPerSecond = 2f;
                renderProxy.Initialize(body, 1);

                Assert.AreEqual(1, renderProxy.RenderNow());

                SpriteRenderer renderer = targetObject.GetComponentInChildren<SpriteRenderer>(true);
                Assert.IsNotNull(renderer);
                Assert.AreSame(secondSprite, renderer.sprite);
            }
            finally
            {
                DestroySprite(firstSprite);
                DestroySprite(secondSprite);
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileInstancedSpriteRenderProxy_GroupsVisualFrameIndexWithoutPooledRenderers()
        {
            GameObject targetObject = new GameObject("ProjectileInstancedSpriteRenderProxyVisualFrameTarget");
            Texture2D atlasTexture = CreateAtlasSprites(out Sprite firstSprite, out Sprite secondSprite);
            try
            {
                ProjectileRenderProxyBodyProbe body = new ProjectileRenderProxyBodyProbe();
                body.SetSnapshots(
                    CreateSnapshot(1, Vector3.left, 0.25f, 1),
                    CreateSnapshot(2, Vector3.right, 0.25f, 2));

                ProjectileInstancedSpriteRenderProxy renderProxy =
                    targetObject.AddComponent<ProjectileInstancedSpriteRenderProxy>();
                renderProxy.FrameSprites = new[] { firstSprite, secondSprite };
                renderProxy.Initialize(body, 2);

                Assert.AreEqual(2, renderProxy.BuildRenderData());

                Assert.AreEqual(2, renderProxy.VisibleCount);
                Assert.AreEqual(2, renderProxy.BatchCount);
                Assert.AreEqual(0, targetObject.GetComponentsInChildren<SpriteRenderer>(true).Length);

                Assert.AreEqual(2, renderProxy.RenderNow());
                Assert.AreEqual(2, renderProxy.RenderCallCount);
                Assert.AreEqual(2, renderProxy.BatchCount);
            }
            finally
            {
                DestroySpritesWithSharedTexture(atlasTexture, firstSprite, secondSprite);
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileInstancedSpriteRenderProxy_AnimatesFrameGroupsFromSnapshotLifetime()
        {
            GameObject targetObject = new GameObject("ProjectileInstancedSpriteRenderProxyLifetimeFrameTarget");
            Texture2D atlasTexture = CreateAtlasSprites(out Sprite firstSprite, out Sprite secondSprite);
            try
            {
                ProjectileRenderProxyBodyProbe body = new ProjectileRenderProxyBodyProbe();
                body.SetSnapshots(
                    CreateSnapshot(1, Vector3.left, 0.25f, 1, remainingLifetime: 1f, initialLifetime: 1f),
                    CreateSnapshot(2, Vector3.right, 0.25f, 1, remainingLifetime: 0.4f, initialLifetime: 1f));

                ProjectileInstancedSpriteRenderProxy renderProxy =
                    targetObject.AddComponent<ProjectileInstancedSpriteRenderProxy>();
                renderProxy.FrameSprites = new[] { firstSprite, secondSprite };
                renderProxy.AnimateFramesByLifetime = true;
                renderProxy.FrameAnimationFramesPerSecond = 2f;
                renderProxy.Initialize(body, 2);

                Assert.AreEqual(2, renderProxy.BuildRenderData());

                Assert.AreEqual(2, renderProxy.VisibleCount);
                Assert.AreEqual(2, renderProxy.BatchCount);
                Assert.AreEqual(0, targetObject.GetComponentsInChildren<SpriteRenderer>(true).Length);

                Assert.AreEqual(2, renderProxy.RenderNow());
                Assert.AreEqual(2, renderProxy.RenderCallCount);
                Assert.AreEqual(2, renderProxy.BatchCount);
            }
            finally
            {
                DestroySpritesWithSharedTexture(atlasTexture, firstSprite, secondSprite);
                Object.DestroyImmediate(targetObject);
            }
        }



        [Test]
        public void ProjectileInstancedSpriteRenderProxy_ClearsRenderDataOnEmptyBodyAndDisable()
        {
            GameObject targetObject = new GameObject("ProjectileInstancedSpriteRenderProxyClearTarget");
            try
            {
                ProjectileRenderProxyBodyProbe body = new ProjectileRenderProxyBodyProbe();
                body.SetSnapshots(CreateSnapshot(1, Vector3.up, 0.75f));

                ProjectileInstancedSpriteRenderProxy renderProxy =
                    targetObject.AddComponent<ProjectileInstancedSpriteRenderProxy>();
                renderProxy.Initialize(body, 1);

                Assert.AreEqual(1, renderProxy.BuildRenderData());
                Assert.AreEqual(1, renderProxy.VisibleCount);
                Assert.AreEqual(1, renderProxy.BatchCount);
                Matrix4x4[] matrices = GetPrivateField<Matrix4x4[]>(renderProxy, "matrices");
                Assert.IsNotNull(matrices);
                Assert.AreNotEqual(0f, matrices[0].m00);

                body.ClearAll();
                Assert.AreEqual(0, renderProxy.BuildRenderData());

                Assert.AreEqual(0, renderProxy.VisibleCount);
                Assert.AreEqual(0, renderProxy.BatchCount);
                Assert.AreEqual(0, renderProxy.RenderCallCount);
                Assert.AreEqual(0f, matrices[0].m00);
                Assert.AreEqual(0f, matrices[0].m11);

                body.SetSnapshots(CreateSnapshot(2, Vector3.left, 0.25f));
                Assert.AreEqual(1, renderProxy.BuildRenderData());
                SetPrivateField(renderProxy, "renderCallCount", 3);

                InvokePrivateMethod(renderProxy, "OnDisable");

                Assert.AreEqual(0, renderProxy.VisibleCount);
                Assert.AreEqual(0, renderProxy.BatchCount);
                Assert.AreEqual(0, renderProxy.RenderCallCount);
                Assert.AreEqual(0f, matrices[0].m00);
                Assert.AreEqual(0f, matrices[0].m11);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }



        private static void InvokeOnValidate(object target)
        {
            InvokePrivateMethod(target, "OnValidate");
        }



        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, InstancePrivateFlags);
            Assert.IsNotNull(method);
            method.Invoke(target, null);
        }



        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            target.GetType()
                .GetField(fieldName, InstancePrivateFlags)
                ?.SetValue(target, value);
        }



        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstancePrivateFlags);
            Assert.IsNotNull(field);
            return (T)field.GetValue(target);
        }



        private static ProjectileSnapshot CreateSnapshot(
            int projectileId,
            Vector3 position,
            float radius,
            int visualFrameIndex = 0,
            float remainingLifetime = 1f,
            float initialLifetime = -1f)
        {
            return new ProjectileSnapshot(
                projectileId,
                position,
                Vector3.zero,
                radius,
                remainingLifetime,
                1f,
                1,
                true,
                visualFrameIndex,
                initialLifetime);
        }



        private static Sprite CreateTestSprite(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }



        private static Texture2D CreateAtlasSprites(out Sprite firstSprite, out Sprite secondSprite)
        {
            Texture2D texture = new Texture2D(2, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
            };
            texture.SetPixel(0, 0, Color.red);
            texture.SetPixel(1, 0, Color.blue);
            texture.Apply(false, false);

            firstSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            secondSprite = Sprite.Create(texture, new Rect(1f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            firstSprite.hideFlags = HideFlags.HideAndDontSave;
            secondSprite.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }



        private static void DestroySpritesWithSharedTexture(Texture texture, params Sprite[] sprites)
        {
            if (sprites != null)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null)
                    {
                        Object.DestroyImmediate(sprites[i]);
                    }
                }
            }

            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }



        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Texture texture = sprite.texture;
            Object.DestroyImmediate(sprite);
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }



        private sealed class ProjectileRenderProxyBodyProbe : IPanProjectileBody
        {
            private readonly List<ProjectileSnapshot> snapshots = new List<ProjectileSnapshot>();
            private int nextProjectileId = 1;



            public int ActiveCount => snapshots.Count;
            public int Capacity => 32;
            public int TotalSpawned { get; private set; }
            public int TotalDespawned { get; private set; }
            public int TotalHits => 0;
            public event ProjectileSnapshotHandler ProjectileSpawned;
            public event ProjectileContactHandler ProjectileContacted
            {
                add { }
                remove { }
            }
            public event ProjectileSnapshotHandler ProjectileDespawned;



            public void SetSnapshots(params ProjectileSnapshot[] newSnapshots)
            {
                snapshots.Clear();
                if (newSnapshots != null)
                {
                    snapshots.AddRange(newSnapshots);
                }
            }



            public bool TrySpawn(in ProjectileSpawnRequest spawnData, out int projectileId)
            {
                projectileId = nextProjectileId++;
                ProjectileSnapshot snapshot = new ProjectileSnapshot(
                    projectileId,
                    spawnData.Position,
                    spawnData.Velocity,
                    spawnData.Radius,
                    spawnData.Lifetime,
                    spawnData.Damage,
                    spawnData.TeamId,
                    spawnData.Use2D,
                    spawnData.VisualFrameIndex,
                    spawnData.Lifetime);

                snapshots.Add(snapshot);
                TotalSpawned++;
                ProjectileSpawned?.Invoke(in snapshot);
                return true;
            }



            public int Spawn(in ProjectileSpawnRequest spawnData)
            {
                TrySpawn(in spawnData, out int projectileId);
                return projectileId;
            }



            public void Simulate(float deltaTime)
            {
            }



            public void ClearAll()
            {
                while (snapshots.Count > 0)
                {
                    ProjectileSnapshot snapshot = snapshots[snapshots.Count - 1];
                    snapshots.RemoveAt(snapshots.Count - 1);
                    TotalDespawned++;
                    ProjectileDespawned?.Invoke(in snapshot);
                }
            }



            public bool Despawn(int projectileId)
            {
                for (int i = 0; i < snapshots.Count; i++)
                {
                    ProjectileSnapshot snapshot = snapshots[i];
                    if (snapshot.ProjectileId != projectileId)
                    {
                        continue;
                    }

                    snapshots.RemoveAt(i);
                    TotalDespawned++;
                    ProjectileDespawned?.Invoke(in snapshot);
                    return true;
                }

                return false;
            }



            public bool TryGetSnapshot(int index, out ProjectileSnapshot snapshot)
            {
                if (index < 0 || index >= snapshots.Count)
                {
                    snapshot = default;
                    return false;
                }

                snapshot = snapshots[index];
                return true;
            }



            public bool TryGetSnapshotById(int projectileId, out ProjectileSnapshot snapshot)
            {
                for (int i = 0; i < snapshots.Count; i++)
                {
                    if (snapshots[i].ProjectileId != projectileId)
                    {
                        continue;
                    }

                    snapshot = snapshots[i];
                    return true;
                }

                snapshot = default;
                return false;
            }



            public int CopySnapshots(ProjectileSnapshot[] buffer)
            {
                if (buffer == null)
                {
                    return 0;
                }

                int count = Mathf.Min(buffer.Length, snapshots.Count);
                for (int i = 0; i < count; i++)
                {
                    buffer[i] = snapshots[i];
                }

                return count;
            }
        }
    }
}
