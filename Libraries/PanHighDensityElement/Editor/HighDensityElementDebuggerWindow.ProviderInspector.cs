using System;
using UnityEditor;
using UnityEngine;



namespace Pan.HighDensityElement.Editor
{
    public sealed partial class HighDensityElementDebuggerWindow
    {



        private void DrawComponentSection(
            string id,
            string title,
            bool defaultExpanded,
            Action drawContent)
        {
            if (!componentFoldouts.TryGetValue(id, out bool expanded)) { expanded = defaultExpanded; }
            expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
            componentFoldouts[id] = expanded;
            if (!expanded) { return; }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            drawContent();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }





        private void DrawProviderComponents(in ElementDebuggerInspectorContext context)
        {
            visibleComponents.Clear();
            componentIdCounts.Clear();
            drawnConflictIds.Clear();
            for (int i = 0; i < componentProviders.Count; i++)
            {
                IElementDebuggerComponentProvider provider = componentProviders[i];
                try
                {
                    if (!provider.TryGetDescriptor(in context, out ElementDebuggerComponentDescriptor descriptor))
                    {
                        continue;
                    }

                    visibleComponents.Add(new VisibleComponent(provider, descriptor));
                    if (string.IsNullOrWhiteSpace(descriptor.Id)) { continue; }
                    componentIdCounts.TryGetValue(descriptor.Id, out int count);
                    componentIdCounts[descriptor.Id] = count + 1;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            visibleComponents.Sort(CompareVisibleComponents);

            for (int i = 0; i < visibleComponents.Count; i++)
            {
                VisibleComponent component = visibleComponents[i];
                ElementDebuggerComponentDescriptor descriptor = component.Descriptor;
                bool conflict = !string.IsNullOrWhiteSpace(descriptor.Id) &&
                    componentIdCounts.TryGetValue(descriptor.Id, out int count) && count > 1;
                if (conflict)
                {
                    if (drawnConflictIds.Add(descriptor.Id))
                    {
                        DrawComponentSection(
                            $"provider-conflict:{descriptor.Id}",
                            string.IsNullOrWhiteSpace(descriptor.DisplayName) ? descriptor.Id : descriptor.DisplayName,
                            true,
                            () => EditorGUILayout.HelpBox(
                                $"Component id '{descriptor.Id}'를 둘 이상의 provider가 제공해 편집을 비활성화했습니다.",
                                MessageType.Error));
                    }
                    continue;
                }

                ElementDebuggerInspectorContext capturedContext = context;
                DrawComponentSection(
                    string.IsNullOrWhiteSpace(descriptor.Id)
                        ? $"provider:{component.Provider.GetType().AssemblyQualifiedName}"
                        : $"provider:{descriptor.Id}",
                    string.IsNullOrWhiteSpace(descriptor.DisplayName) ? component.Provider.GetType().Name : descriptor.DisplayName,
                    true,
                    () =>
                    {
                        EditorGUILayout.LabelField("Status", descriptor.Status.ToString());
                        if (!string.IsNullOrWhiteSpace(descriptor.Summary))
                        {
                            EditorGUILayout.LabelField(descriptor.Summary, EditorStyles.wordWrappedLabel);
                        }
                        try
                        {
                            component.Provider.OnInspectorGUI(in capturedContext);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                            EditorGUILayout.HelpBox("Component provider가 예외를 발생시켰습니다.", MessageType.Error);
                        }
                    });
            }
        }





        private static int CompareVisibleComponents(VisibleComponent left, VisibleComponent right)
        {
            int order = left.Provider.Order.CompareTo(right.Provider.Order);
            if (order != 0) { return order; }

            int displayName = string.Compare(
                left.Descriptor.DisplayName,
                right.Descriptor.DisplayName,
                StringComparison.Ordinal);
            if (displayName != 0) { return displayName; }

            return string.Compare(
                left.Provider.GetType().FullName,
                right.Provider.GetType().FullName,
                StringComparison.Ordinal);
        }





        private void DrawLegacyInspectorExtensions(in ElementDebuggerInspectorContext context)
        {
            for (int i = 0; i < inspectorExtensions.Count; i++)
            {
                IElementDebuggerInspectorExtension extension = inspectorExtensions[i];
                try
                {
                    if (!extension.IsVisible(in context)) { continue; }
                    ElementDebuggerInspectorContext capturedContext = context;
                    DrawComponentSection(
                        $"legacy:{extension.GetType().AssemblyQualifiedName}",
                        string.IsNullOrWhiteSpace(extension.DisplayName) ? "Extension" : extension.DisplayName,
                        true,
                        () => extension.OnInspectorGUI(in capturedContext));
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    EditorGUILayout.HelpBox("확장 Inspector가 예외를 발생시켰습니다.", MessageType.Error);
                }
            }
        }





        private bool TryCreateInspectorContext(out ElementDebuggerInspectorContext context)
        {
            if (ElementDebuggerSelection.TryGet(
                    out ElementWorld world,
                    out ElementHandle handle,
                    out ElementSnapshot snapshot))
            {
                context = new ElementDebuggerInspectorContext(world, handle, snapshot, recentFacts);
                return true;
            }

            context = default;
            return false;
        }



        private void DrawSelectedProviderInspector()
        {
            if (!TryCreateInspectorContext(out ElementDebuggerInspectorContext context)) { return; }
            DrawProviderComponents(in context);
            DrawLegacyInspectorExtensions(in context);
        }
    }
}
