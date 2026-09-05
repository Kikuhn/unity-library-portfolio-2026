using Pan.Util;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.Utilities;
using Sirenix.Serialization;
using System.Linq.Expressions;
using ZLinq;



namespace Pan.Event
{
    public abstract class PanEventsInitializeSettingSbjectBase : CustomScriptableObjectSerialized
    {
        ///======================================================================================================================================================



        //? 판 이벤트 타입 Lazy Caching



        /// <summary>
        /// 모든 <see cref="PanBaseEvent"/>의 타입이 저장된 배열
        /// </summary>
        [NonSerialized]
        private static Type[] panBaseEventTypesAll;



        /// <summary>
        /// 모든 <see cref="PanBaseEvent"/>의 타입이 저장된 배열 을 불러오기
        /// </summary>
        public static Type[] GetPanBaseEventTypesAll()
        {
            panBaseEventTypesAll ??= GetInitializableTypes(typeof(PanBaseEvent));

            return panBaseEventTypesAll;
        }



        //? 판 이벤트 밸류 타입 Lazy Caching



        /// <summary>
        /// 모든 <see cref="PanBaseEventValue"/>의 타입이 저장된 배열
        /// </summary>
        [NonSerialized]
        private static Type[] panBaseEventValueTypesAll;



        /// <summary>
        /// 모든 <see cref="PanBaseEventValue"/>의 타입이 저장된 배열 을 불러오기
        /// </summary>
        public static Type[] GetPanBaseEventValueTypesAll()
        {
            panBaseEventValueTypesAll ??= GetInitializableTypes(typeof(PanBaseEventValue));

            return panBaseEventValueTypesAll;
        }



        ///======================================================================================================================================================



        internal static Type[] GetInitializableTypes(Type baseType)
        {
            var types = SU_Collection_Types.GetTypesAssignableTo(baseType, SU_Collection_Types.TypeSearch.ConcreteClasses);

#if UNITY_EDITOR
            var editorAssemblyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in UnityEditor.Compilation.CompilationPipeline
                .GetAssemblies(UnityEditor.Compilation.AssembliesType.Editor)
                .AsValueEnumerable())
            {
                editorAssemblyNames.Add(assembly.name);
            }

            var playerAssemblyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in UnityEditor.Compilation.CompilationPipeline
                .GetAssemblies(UnityEditor.Compilation.AssembliesType.PlayerWithoutTestAssemblies)
                .AsValueEnumerable())
            {
                playerAssemblyNames.Add(assembly.name);
            }

            types = types
                .AsValueEnumerable()
                .Where(type =>
                {
                    var assembly = type.Assembly;
                    var assemblyName = assembly.GetName().Name;

                    if (editorAssemblyNames.Contains(assemblyName))
                    {
                        return playerAssemblyNames.Contains(assemblyName);
                    }

                    return !ReferencesEditorOrTestAssembly(assembly);
                })
                .ToArray();
#endif

            return types;
        }



#if UNITY_EDITOR
        private static bool ReferencesEditorOrTestAssembly(System.Reflection.Assembly assembly)
        {
            static bool IsEditorOrTestAssemblyName(string assemblyName)
            {
                return assemblyName.StartsWith("UnityEditor", StringComparison.Ordinal) ||
                    string.Equals(assemblyName, "UnityEngine.TestRunner", StringComparison.Ordinal) ||
                    string.Equals(assemblyName, "UnityEditor.TestRunner", StringComparison.Ordinal) ||
                    string.Equals(assemblyName, "nunit.framework", StringComparison.Ordinal);
            }

            return IsEditorOrTestAssemblyName(assembly.GetName().Name) ||
                assembly.GetReferencedAssemblies()
                    .AsValueEnumerable()
                    .Any(x => IsEditorOrTestAssemblyName(x.Name));
        }
#endif



        ///======================================================================================================================================================



        public static string GetStableTypeId(Type type)
        {
            if (type == null) { return null; }

            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }



        public static PanEventUsageProfile GetDefaultUsageProfile(Type type)
        {
            if (type == null) { return PanEventUsageProfile.Runtime; }

            var attribute = Attribute.GetCustomAttribute(type, typeof(PanEventUsageProfileAttribute)) as PanEventUsageProfileAttribute;

            return attribute?.UsageProfile ?? PanEventUsageProfile.Runtime;
        }



        #if UNITY_EDITOR
        protected void SaveSettingsAsset()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ||
                !UnityEditor.EditorUtility.IsPersistent(this))
            {
                return;
            }

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
        #endif



        ///======================================================================================================================================================



        protected abstract bool Inspector_ShowCondition_InitializeSettings_InValid { get; }



        ///======================================================================================================================================================
    }
}
