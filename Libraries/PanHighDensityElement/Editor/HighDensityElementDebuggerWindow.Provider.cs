using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.U2D.Physics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;



namespace Pan.HighDensityElement.Editor
{
    public sealed partial class HighDensityElementDebuggerWindow
    {
        private void OnInspectorExtensionsChanged()
        {
            ElementDebuggerInspectorExtensionRegistry.CopyExtensions(inspectorExtensions);
            inspectorDirty = true;
            RefreshInspector();
        }


        private void OnRowProvidersChanged()
        {
            ElementDebuggerRowProviderRegistry.CopyProviders(rowProviders);
            foreach (WorldSnapshotSet set in snapshotSetsByWorldId.Values)
            {
                set.RowMetadataDirty = true;
            }
            structureDirty = true;
            RebuildTree();
        }


        private void OnComponentProvidersChanged()
        {
            ElementDebuggerComponentProviderRegistry.CopyProviders(componentProviders);
            inspectorDirty = true;
            RefreshInspector();
        }


        private void OnWorldMetadataProvidersChanged()
        {
            ElementDebuggerWorldMetadataProviderRegistry.CopyProviders(worldMetadataProviders);
            foreach (WorldSnapshotSet set in snapshotSetsByWorldId.Values) { set.MetadataDirty = true; }
            structureDirty = true;
            RefreshWorldsAndItems(force: true);
        }


        private void RefreshProviderCaches()
        {
            ElementDebuggerInspectorExtensionRegistry.CopyExtensions(inspectorExtensions);
            ElementDebuggerRowProviderRegistry.CopyProviders(rowProviders);
            ElementDebuggerComponentProviderRegistry.CopyProviders(componentProviders);
            ElementDebuggerWorldMetadataProviderRegistry.CopyProviders(worldMetadataProviders);
        }


        private void RefreshRowMetadata(WorldSnapshotSet set)
        {
            if (!set.RowMetadataDirty) { return; }

            set.RowMetadataByKey.Clear();
            set.AllocatedLifetimeKeys.Clear();
            for (int snapshotIndex = 0; snapshotIndex < set.Snapshots.Count; snapshotIndex++)
            {
                ElementSnapshot snapshot = set.Snapshots[snapshotIndex];
                if (set.World.TryGetLifetime(snapshot.Key, out _))
                {
                    set.AllocatedLifetimeKeys.Add(snapshot.Key);
                }
                var context = new ElementDebuggerRowContext(set.World, snapshot);
                for (int providerIndex = 0; providerIndex < rowProviders.Count; providerIndex++)
                {
                    try
                    {
                        if (!rowProviders[providerIndex].TryGetMetadata(
                                in context,
                                out ElementDebuggerRowMetadata metadata))
                        {
                            continue;
                        }

                        set.RowMetadataByKey[snapshot.Key] = metadata;
                        break;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
            set.RowMetadataDirty = false;
        }
    }
}