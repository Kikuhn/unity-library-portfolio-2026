using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using Object = UnityEngine.Object;
using Pan.Util;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor.AddressableAssets;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;



//? 유니티 에디터를 확장한 추상 클래스들이 정리가 되어있는 정도의 코드



namespace Pan.Util.Editors
{
    ///======================================================================================================================================================



    /// <summary>
    /// <typeparamref name="T"/> 형식의 오브젝트를 편집하기 위한 추상 에디터 클래스입니다.
    /// UnityEditor.Editor를 상속하며, 기본적으로 <see cref="target"/>과 <see cref="targets"/>를
    /// <typeparamref name="T"/>로 캐스팅하여 편리하게 사용할 수 있습니다.
    /// <para/>
    /// 추가로, "동일 <typeparamref name="T"/>를 다루는 Editor 인스턴스"들을
    /// 정적 딕셔너리에 등록하여, 중복 생성 시에도 목록을 관리할 수 있도록 기능을 확장했습니다.
    /// </summary>
    /// <typeparam name="T">편집할 <see cref="UnityEngine.Object"/>를 상속하는 타입</typeparam>
    public abstract class EditorExpand<T> : Editor where T : Object
    {
        /// <summary>
        /// [정적] 특정 대상 오브젝트(T)마다 "활성화된 Editor 리스트"를 보관합니다.
        /// (Key = 대상 오브젝트, Value = 그 오브젝트를 편집중인 Editor 목록)
        /// </summary>
        private static readonly Dictionary<T, List<EditorExpand<T>>> s_editorMap = new Dictionary<T, List<EditorExpand<T>>>();



        /// <summary>
        /// 현재 에디터가 다루는 단일 대상(<see cref="Editor.target"/>)을 <typeparamref name="T"/>로 캐스팅하여 반환합니다.
        /// </summary>
        public T Target => target as T;



        /// <summary>
        /// 현재 에디터가 다루는 복수 대상(<see cref="Editor.targets"/>)을 <typeparamref name="T"/> 배열로 캐스팅하여 반환합니다.
        /// </summary>
        public T[] Targets { get; private set; }



        /// <summary>
        /// 에디터 인스턴스가 생성될 때 호출되는 메서드입니다.
        /// (Unity 에디터 라이프사이클 상, 필요한 경우 재정의하여 사용)
        /// </summary>
        protected virtual void Awake()
        {
            //. 필요하다면 자식 클래스에서 재정의
        }



        /// <summary>
        /// 에디터가 활성화될 때(예: OnEnable) 호출되는 메서드로,
        /// <see cref="targets"/> 배열을 <see cref="Targets"/>에 캐스팅해 할당합니다.
        /// 또한, 정적 딕셔너리(s_editorMap)에 "이 Editor"를 등록합니다.
        /// </summary>
        protected virtual void OnEnable()
        {
            //. 1) Targets 배열 구성
            Targets = new T[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                Targets[i] = targets[i] as T;
            }

            //. 2) Target이 유효한 경우, 정적 딕셔너리에 등록
            if (Target != null)
            {
                if (!s_editorMap.TryGetValue(Target, out var list))
                {
                    list = new List<EditorExpand<T>>();
                    s_editorMap[Target] = list;
                }

                //. null(이미 파괴된 Editor) 정리
                list.RemoveAll(e => e == null);

                //. 자신이 없다면 추가
                if (!list.Contains(this))
                {
                    list.Add(this);
                }

                //. 필요하다면 이 타이밍에 "중복 에디터 제거" 등의 로직을 자식에서 구현 가능
                Refresh_OnEditorAllListChanged(list);
            }
        }



        /// <summary>
        /// 에디터가 비활성화될 때(예: OnDisable) 호출되는 메서드입니다.
        /// (필요하다면 재정의하여 정리 로직을 추가할 수 있습니다.)
        /// </summary>
        protected virtual void OnDisable()
        {
            //. 정적 딕셔너리에서 제거
            if (Target != null && s_editorMap.TryGetValue(Target, out var list))
            {
                list.RemoveAll(e => e == null); //. null 정리
                list.Remove(this);

                //. 만약 리스트가 비었으면, Key 자체를 제거
                if (list.Count == 0)
                {
                    s_editorMap.Remove(Target);
                }
                else
                {
                    Refresh_OnEditorAllListChanged(list);
                }
            }
        }



        //. Override Hooks

        /// <summary>
        /// 어떤 Target에 대해 Editor 목록이 변경(추가/제거)되었을 때 호출됩니다.
        /// <para>이 에디터를 대상으로 뭔가 작동할때, 중복 호출로 인한 충돌을 방지 해야 할 때 사용</para>
        /// <para>유니티의 기본 커스텀에디터의 초기화메서드는, 한번만 실행되는것이 보장되지 않는다</para>
        /// <para/>
        /// 예: "중복 Editor"를 제거하거나, "가장 마지막 Editor만 유지" 같은 로직을
        /// 자식 클래스에서 구현할 수 있습니다.
        /// </summary>
        /// <param name="editors">현재 Target에 연결된 Editor들의 리스트</param>
        protected virtual void Refresh_OnEditorAllListChanged(List<EditorExpand<T>> editors)
        {
            //. 자식 클래스에서 필요시 override
            //. 예: "오래된 Editor 강제 제거" / "마지막 Editor만 남기기" 등
        }



        //. Helper

        /// <summary>
        /// 현재 'Target'에 연결된 모든 EditorExpand 인스턴스를 반환합니다.
        /// (없으면 빈 배열 반환)
        /// </summary>
        protected List<EditorExpand<T>> GetEditorsOfTarget()
        {
            if (Target != null && s_editorMap.TryGetValue(Target, out var list))
            {
                //. 미리 null 정리
                list.RemoveAll(e => e == null);
                return list;
            }
            return new List<EditorExpand<T>>();
        }



        //. 기능 메서드 예시

        /// <summary>
        /// <paramref name="action"/>을 <see cref="EditorGUI.BeginChangeCheck()"/>와
        /// <see cref="EditorGUI.EndChangeCheck()"/>로 래핑하여, 변경 사항이 있으면
        /// <see cref="serializedObject.ApplyModifiedProperties()"/>와 
        /// <see cref="EditorUtility.SetDirty(Object)"/>를 자동 처리합니다.
        /// </summary>
        /// <param name="action">에디터 GUI 로직(필드 그리기 등)을 실행할 델리게이트</param>
        protected void ApplyGUIChanges(Action action)
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            action?.Invoke();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }
    }



    ///======================================================================================================================================================



    /// <summary>
    /// <see cref="EditorExpand{T}"/>를 상속받아 Odin Inspector 기능(<see cref="IOdinPropertyTree"/>)을 추가로 지원하는
    /// 인스펙터 에디터 확장 클래스입니다. 
    /// 기본 <see cref="Editor"/> 동작을 확장하고, 
    /// Default Inspector 표시 모드를 다양하게 설정할 수 있습니다.
    /// </summary>
    /// <typeparam name="T">
    /// 에디터에서 편집할 <see cref="UnityEngine.Object"/>를 상속하는 타입
    /// </typeparam>
    public abstract class EditorExpand_InspectorGUI<T> : EditorExpand<T>, IOdinPropertyTree where T : Object
    {
        ///======================================================================================================================================================



        /// <summary>
        /// DefaultInspector를 어떻게 표시할지 결정하는 열거형입니다.
        /// </summary>
        public enum EDrawDefaultInspectorMode
        {
            /// <summary>출력하지 않음</summary>
            Nothing,
            /// <summary>유니티 기본 Inspector 직접 출력</summary>
            Visible,
            /// <summary>유니티 기본 Inspector 대신 Odin Inspector 방식으로 출력</summary>
            OdinVisible,
            /// <summary>유니티 기본 Inspector를 Fold(접힘) 형태로 출력</summary>
            Fold,
            /// <summary>유니티 기본 Inspector 대신 Odin Inspector를 Fold 형태로 출력</summary>
            OdinFold
        }



        /// <summary>
        /// DefaultInspector를 어느 위치에(상단/하단) 렌더링할지 결정하는 열거형입니다.
        /// </summary>
        public enum EDrawDefaultInspectorPosition
        {
            /// <summary>인스펙터 상단에 Draw</summary>
            Top,
            /// <summary>인스펙터 하단에 Draw</summary>
            Bottom
        }



        ///======================================================================================================================================================



        /// <summary>
        /// DefaultInspector를 어떤 방식으로 Draw할지 결정하는 전역 설정값입니다.
        /// (기본값: <see cref="EDrawDefaultInspectorMode.OdinFold"/>)
        /// </summary>
        protected static EDrawDefaultInspectorMode Mode_DrawDefaultInspector = EDrawDefaultInspectorMode.OdinFold;

        /// <summary>
        /// DefaultInspector를 어느 위치(상/하단)에 Draw할지 결정하는 전역 설정값입니다.
        /// (기본값: <see cref="EDrawDefaultInspectorPosition.Top"/>)
        /// </summary>
        protected static EDrawDefaultInspectorPosition Mode_DrawDefaultInspectorPosition = EDrawDefaultInspectorPosition.Top;

        /// <summary>
        /// <see cref="EDrawDefaultInspectorMode.Fold"/> 또는 <see cref="EDrawDefaultInspectorMode.OdinFold"/> 모드일 때
        /// 열림/접힘 상태를 저장하는 스위치입니다.
        /// </summary>
        protected static bool EditorSwitch_FoldDrawDefaultInspector = false;



        ///======================================================================================================================================================



        // 내부 이름 구성
        protected static string NameOf_Mode_DrawDefaultInspector
            => $"{typeof(T).FullName}_{nameof(Mode_DrawDefaultInspector)}";
        protected static string NameOf_Mode_DrawDefaultInspectorPosition
            => $"{typeof(T).FullName}_{nameof(Mode_DrawDefaultInspectorPosition)}";
        protected static string NameOf_EditorSwitch_FoldDrawDefaultInspector
            => $"{typeof(T).FullName}_{nameof(EditorSwitch_FoldDrawDefaultInspector)}";



        ///======================================================================================================================================================



        //? 하위 클래스에서 사용하는, 고정 DefaultInspector모드



        protected virtual EDrawDefaultInspectorMode? CurrentMode_DrawDefaultInspector_Fixed => null;



        ///======================================================================================================================================================



        /// <summary>
        /// Odin Inspector에서 사용하는 <see cref="PropertyTree"/> 인스턴스입니다.
        /// </summary>
        protected PropertyTree OdinPropertyTree { get; private set; }



        /// <inheritdoc/>
        PropertyTree IOdinPropertyTree.OdinPropertyTree => OdinPropertyTree;



        ///======================================================================================================================================================



        /// <summary>
        /// 에디터가 활성화(OnEnable)될 때 Odin의 <see cref="PropertyTree"/>를 생성하고,
        /// 이전에 저장된 에디터 설정값을 불러옵니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            // Odin Inspector용 PropertyTree 초기화
            OdinPropertyTree = PropertyTree.Create(serializedObject);

            // 에디터 설정 불러오기
            EditorSettingsLoad();
        }



        /// <summary>
        /// 에디터가 비활성화(OnDisable)될 때, 에디터 설정값을 저장하고,
        /// Odin의 <see cref="PropertyTree"/>를 정리합니다.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            // 에디터 설정 저장
            EditorSettingsSave();

            // Odin PropertyTree 정리
            if (OdinPropertyTree != null)
            {
                OdinPropertyTree.Dispose();
                OdinPropertyTree = null;
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 현재 <see cref="Mode_DrawDefaultInspector"/> 값에 따라 Default Inspector를
        /// 직접 표시하거나, Odin Inspector 형태로 표시하는 로직을 실행합니다.
        /// </summary>
        /// <param name="endHorizontalLine">
        /// true면 표시 후에 수평 라인을 그리거나 여백을 넣는 등의 후처리를 할 수 있습니다.
        /// (메서드 내부의 커스텀 로직에 따라 달라질 수 있음)
        /// </param>
        protected void CurrentDrawDefaultInspectorMode(bool endHorizontalLine)
        {
            var mode = CurrentMode_DrawDefaultInspector_Fixed ?? Mode_DrawDefaultInspector;

            switch (mode)
            {
                case EDrawDefaultInspectorMode.Visible:
                SU_CustomEditor.DrawDefaultInspector_WithHead(this, null, endHorizontalLine);
                break;

                case EDrawDefaultInspectorMode.Fold:
                SU_CustomEditor.DrawDefaultInspector_WithFold(
                    this,
                    ref EditorSwitch_FoldDrawDefaultInspector,
                    null
                );
                break;

                case EDrawDefaultInspectorMode.OdinVisible:
                SU_CustomEditor.DrawOdinInspector_WithHead(OdinPropertyTree, null, endHorizontalLine);
                break;

                case EDrawDefaultInspectorMode.OdinFold:
                SU_CustomEditor.DrawOdinInspector_WithFold(
                    OdinPropertyTree,
                    ref EditorSwitch_FoldDrawDefaultInspector,
                    null
                );
                break;

                case EDrawDefaultInspectorMode.Nothing:
                default:
                // 출력하지 않음
                break;
            }
        }



        /// <summary>
        /// Unity의 인스펙터에서 불리는 주요 메서드로,
        /// <see cref="Mode_DrawDefaultInspectorPosition"/> 값에 따라
        /// Default Inspector(또는 Odin Inspector)를 상단/하단에 표시하고,
        /// 그 밖의 커스텀 GUI(<see cref="OnInspectorGUI_Current"/> 등)를 렌더링합니다.
        /// </summary>
        public sealed override void OnInspectorGUI()
        {
            SU_CustomEditor.DrawHighlightedBox(() =>
            {
                SU_CustomEditor.DrawDefaultScriptFieldDouble(target, this, "스크립트");

                // Default Inspector 표시 모드를 설정하는 GUI
                SU_CustomEditor.CheckChangeAction(
                    this,
                    () =>
                    {
                        SU_CustomEditor.HorizontalGUI(() =>
                        {
                            bool isModeNothing = (Mode_DrawDefaultInspector != EDrawDefaultInspectorMode.Nothing);

                            SU_CustomEditor.LabelField_TextAutoWidthHeight("인스펙터 표시 모드", 30, isModeNothing ? GUILayout.Width(150) : GUILayout.Width(150));

                            GUILayout.Space(-20);
                            SU_CustomEditor.ActiveGUICondition(CurrentMode_DrawDefaultInspector_Fixed == null, () =>
                            {
                                SU_CustomEditor.RenderField_Enum(this, ref Mode_DrawDefaultInspector, "", "", isModeNothing ? GUILayout.MinWidth(Screen.width / 3f) : GUILayout.MinWidth(Screen.width / 2f));
                            });

                            // Nothing 외에는 Position 설정도 표시
                            if (isModeNothing)
                            {
                                //GUILayout.Space(-10);
                                SU_CustomEditor.RenderField_Enum(this, ref Mode_DrawDefaultInspectorPosition, "", "", GUILayout.MinWidth(Screen.width / 3f), GUILayout.MaxWidth(80));
                            }
                        });
                    },
                    _ => { EditorSettingsSave(); }
                );

                if (Mode_DrawDefaultInspectorPosition == EDrawDefaultInspectorPosition.Top)
                {
                    CurrentDrawDefaultInspectorMode(false);
                }
            });
            SU_CustomEditor.Format_HorizontalLine(false);

            OnInspectorGUI_Current();

            if (Mode_DrawDefaultInspectorPosition == EDrawDefaultInspectorPosition.Bottom)
            {
                SU_CustomEditor.DrawHighlightedBox(() =>
                {
                    CurrentDrawDefaultInspectorMode(false);
                });
            }

            serializedObject.Update();
        }



        /// <summary>
        /// 상속받는 클래스에서 재정의하여, 실제 커스텀 GUI 요소를 그리는 로직을 작성할 수 있습니다.
        /// </summary>
        protected virtual void OnInspectorGUI_Current() { }



        ///======================================================================================================================================================



        /// <summary>
        /// 에디터 설정을 저장합니다. (override 가능)
        /// 여기서는 <see cref="EditorUserSettings.SetConfigValue"/>를 사용해
        /// 유니티 에디터 설정(User Settings)에 값을 저장합니다.
        /// </summary>
        protected virtual void EditorSettingsSave()
        {
            EditorUserSettings.SetConfigValue(
                NameOf_Mode_DrawDefaultInspector,
                ((int)Mode_DrawDefaultInspector).ToString()
            );
            EditorUserSettings.SetConfigValue(
                NameOf_Mode_DrawDefaultInspectorPosition,
                ((int)Mode_DrawDefaultInspectorPosition).ToString()
            );
            EditorUserSettings.SetConfigValue(
                NameOf_EditorSwitch_FoldDrawDefaultInspector,
                EditorSwitch_FoldDrawDefaultInspector.ToString()
            );
        }



        /// <summary>
        /// 에디터 설정을 불러옵니다. (override 가능)
        /// 여기서는 <see cref="EditorUserSettings.GetConfigValue"/>를 사용해 
        /// User Settings에 저장된 값을 읽고, enum/boolean으로 파싱합니다.
        /// </summary>
        protected virtual void EditorSettingsLoad()
        {
            // Mode_DrawDefaultInspector
            if (int.TryParse(
                EditorUserSettings.GetConfigValue(NameOf_Mode_DrawDefaultInspector),
                out int modeDrawDefaultInspector
            ))
            {
                Mode_DrawDefaultInspector = (EDrawDefaultInspectorMode)modeDrawDefaultInspector;
            }

            // Mode_DrawDefaultInspectorPosition
            if (int.TryParse(
                EditorUserSettings.GetConfigValue(NameOf_Mode_DrawDefaultInspectorPosition),
                out int modeDrawDefaultInspectorPosition
            ))
            {
                Mode_DrawDefaultInspectorPosition = (EDrawDefaultInspectorPosition)modeDrawDefaultInspectorPosition;
            }

            // EditorSwitch_FoldDrawDefaultInspector
            if (bool.TryParse(
                EditorUserSettings.GetConfigValue(NameOf_EditorSwitch_FoldDrawDefaultInspector),
                out bool editorSwitchFoldDrawDefaultInspector
            ))
            {
                EditorSwitch_FoldDrawDefaultInspector = editorSwitchFoldDrawDefaultInspector;
            }
        }



        ///======================================================================================================================================================
    }



    ///======================================================================================================================================================



    //? 싱글톤 SO 에디터 코드



    /// <summary>
    /// <typeparamref name="TSbject"/>가 <see cref="SingleTon_ScriptableObject{TSbject}"/>를 상속하는 경우에,
    /// 싱글톤 ScriptableObject를 위한 인스펙터 편집 기능을 제공하는 추상 클래스입니다.
    /// <para>
    /// - <typeparamref name="TEditor"/>: 실제 이 클래스의 형식을 지정하는 에디터 타입
    /// - <typeparamref name="TSbject"/>: 싱글톤 ScriptableObject의 타입
    /// </para>
    /// <remarks>
    /// <seealso cref="SingleTon_ScriptableObject{TSbject}"/>가 제공하는 <c>LoadSingleTon</c>,
    /// <c>UnLoadSingleTon</c> 등의 함수를 호출하여 싱글톤 로직을 다룹니다.
    /// </remarks>
    /// </summary>
    /// <typeparam name="TEditor">
    /// 에디터 클래스 자신의 구체 타입(예: MySingletonEditor).
    /// </typeparam>
    /// <typeparam name="TSbject">
    /// <see cref="SingleTon_ScriptableObject{TSbject}"/>를 상속하는 ScriptableObject 타입.
    /// </typeparam>
    public abstract class EditorExpandSingleTon_InspectorGUI<TEditor, TSbject> : EditorExpand_InspectorGUI<TSbject>
        where TEditor : Editor
        where TSbject : SingleTon_ScriptableObject<TSbject>
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 내부적으로 로드한 싱글톤 <typeparamref name="TSbject"/> 인스턴스를 저장합니다.
        /// </summary>
        private static TSbject instanceTarget;

        /// <summary>
        /// 현재 싱글톤 ScriptableObject를 편집하기 위한 <typeparamref name="TEditor"/> 인스턴스를 캐싱합니다.
        /// </summary>
        private static TEditor instance;



        /// <summary>
        /// <typeparamref name="TEditor"/> 싱글톤 인스턴스에 접근하기 위한 프로퍼티입니다.
        /// 내부적으로 필요시 <see cref="SettingSingleTonEditor(bool)"/>을 통해 생성합니다.
        /// </summary>
        public static TEditor O
        {
            get
            {
                if (instance == null)
                {
                    SettingSingleTonEditor(false);
                }
                return instance;
            }
        }



        ///======================================================================================================================================================



        protected override void Awake()
        {
            base.Awake();
            return; //! 250617 비활성화
            if (!Target.IsRegisteredAddressable) RegistereAddressable();
        }



        private void RegistereAddressable()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var addressableName = Target.GetType().Name;
            //! AddressableAssetSettings가 없으면 즉시 중단
            if (settings == null)
            {
                Debug.LogError("❌ [Addressables] 싱글톤 SO 등록 실패 — AddressableAssetSettings를 찾을 수 없습니다.");
                return;
            }

            //. 대상 SO의 경로·GUID
            string assetPath = AssetDatabase.GetAssetPath(Target);
            if (string.IsNullOrEmpty(assetPath)) return;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            bool isRegistered = settings.FindAssetEntry(guid) != null;

            //? 기본 그룹으로 이동(또는 생성)
            AddressableAssetGroup group = settings.DefaultGroup;
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = addressableName;   //. 명시적 주소

            AssetDatabase.SaveAssets();

            if (isRegistered)
            {
                Debug.LogWarning($"🔄 [Addressables] 싱글톤 SO 갱신 — 그룹: {group.name}, 에셋이름: {addressableName}, 에셋경로: {assetPath}");
            }
            else
            {
                Debug.Log($"✅ [Addressables] 싱글톤 SO 등록 완료 — 그룹: {group.name}, 에셋이름: {addressableName}, 에셋경로: {assetPath}");
            }

            Target.IsRegisteredAddressable = true;
        }




        /// <summary>
        /// 싱글톤 ScriptableObject를 로드하고, 이에 대한 에디터(<typeparamref name="TEditor"/>)를 생성·캐싱합니다.
        /// </summary>
        /// <param name="compulsionLoad">
        /// true면 기존 캐시와 무관하게 새로 싱글톤을 로드합니다.
        /// false면 기존 <see cref="instanceTarget"/>가 <c>null</c>일 때만 로드합니다.
        /// </param>
        /// <returns>로딩 성공 여부(기본적으로 true)</returns>
        private static bool SettingSingleTonEditor(bool compulsionLoad)
        {
            instanceTarget = SingleTon_ScriptableObject<TSbject>.LoadSingleTon(compulsionLoad);


            // Editor 인스턴스 생성
            instance = CreateEditor(instanceTarget) as TEditor;

            return true;
        }



        /// <summary>
        /// 현재 싱글톤 ScriptableObject를 강제로 다시 로드(갱신)합니다.
        /// </summary>
        private void Refresh_SingleTon()
        {
            SettingSingleTonEditor(true);
        }



        /// <summary>
        /// 현재 프로젝트에 <typeparamref name="TSbject"/>가 정확히 하나만 존재하는지 검사하고,
        /// 결과를 콘솔에 출력합니다.
        /// </summary>
        private void CheckSingleTonCounts()
        {
            var findAssets = SU_EditorPath.FindAllAssetsWithPath<TSbject>();

            // 하나만 존재
            if (findAssets.Count == 1)
            {
                Debug.Log($"{typeof(TSbject)}가 단 하나 발견!\n경로: {findAssets[0].Path}");
            }
            // 0개
            else if (findAssets.Count == 0)
            {
                Debug.LogError($"{typeof(TSbject)}가 단 하나도 존재하지 않음!");
            }
            // 2개 이상
            else
            {
                StringBuilder sb = new StringBuilder();
                int num = 1;

                foreach (var item in findAssets)
                {
                    sb.AppendLine($"경로 {num}:\t{item.Path}");
                    num++;
                }

                Debug.LogError($"{typeof(TSbject)}가 {findAssets.Count}개 존재!, 1개가 되어야 함\n{sb.ToString(true)}");
            }
        }



        ///======================================================================================================================================================



        /// <summary>
        /// 인스펙터 GUI에서 싱글톤 관련 정보를 표시하는 보조 메서드입니다.
        /// (싱글톤 로드 여부, 버튼 등을 표시)
        /// </summary>
        private void OnInspectorGUI_ForSingleTon()
        {
            return; //! 250617 비활성화
            string assetName = SU_String.WrapTargetSubstring(
                typeof(TSbject).Name,
                "<b><i><color=#ed5565>",
                "</color></i></b>"
            );

            if (O == null)
            {
                SU_CustomEditor.LabelField_TextAutoWidthHeight(
                    $"Error, 이 ScriptableObject는 <b>싱글톤</b> 입니다.\n" +
                    $"어드레서블에 {assetName}와 똑같은 이름의 에셋으로 등록이 되어 있어야 합니다.",
                    Color.red
                );
            }
            else
            {
                SU_CustomEditor.LabelField_TextAutoWidthHeight(
                    $"이 ScriptableObject는 <b>싱글톤</b> 입니다.\n" +
                    $"현재 어드레서블에 {assetName}를 잘 불러온 상태입니다."
                );
            }

            EditorGUILayout.Space();

            SU_CustomEditor.Render_Button(
                this,
                "이 ScriptableObject가 단 1개만 존재하는지 확인",
                CheckSingleTonCounts
            );

            SU_CustomEditor.HorizontalGUI(() =>
            {
                SU_CustomEditor.LabelField_TextAutoWidthHeight("싱글톤 Addressable");
                SU_CustomEditor.Render_Button(this, "수동 갱신", Refresh_SingleTon);
                SU_CustomEditor.Render_Button(this, "수동 해제", () =>
                {
                    SingleTon_ScriptableObject<TSbject>.UnLoadSingleTon();
                });
                SU_CustomEditor.Render_Button(this, "이 SO를 어드레서블에 등록", RegistereAddressable);
            });

            EditorGUILayout.Space();
            SU_CustomEditor.Format_IndentLevel_Plus();

            SU_CustomEditor.LabelField_TextAutoWidthHeight(
                "WARNING! 에셋의 이름 변경 또는 클래스의 이름이 변경되었을 때,\n" +
                "테스트 용도로 사용해주세요\n(기본적으로 컴파일 시 자동으로 갱신되나, 즉시 하고 싶다면 수동으로 가능합니다)"
            );

            SU_CustomEditor.Format_IndentLevel_Minus();

            SU_CustomEditor.Format_HorizontalLine();
        }



        /// <summary>
        /// 싱글톤 관련 GUI를 먼저 표시하고(<see cref="OnInspectorGUI_ForSingleTon"/>),
        /// 추가적인 인스펙터 GUI는 <see cref="OnInspectorGUI_Current2"/>에서 그리도록 분리합니다.
        /// </summary>
        protected override sealed void OnInspectorGUI_Current()
        {
            //// 싱글톤 ScriptableObject 헤더
            //SU_CustomEditor.AutoLabelField_Head(
            //    "싱글톤 ScriptableObject",
            //    SU_CustomEditor.LabelHeadType.H2,
            //    OnInspectorGUI_ForSingleTon,
            //    false
            //);
            //! 250617 비활성화


            // 추가적인 인스펙터 로직
            OnInspectorGUI_Current2();
        }



        /// <summary>
        /// 싱글톤 이외의 GUI를 그릴 수 있도록 재정의할 수 있는 확장 포인트입니다.
        /// (필요하다면 이 메서드를 오버라이드하여 추가 GUI를 작성)
        /// </summary>
        protected virtual void OnInspectorGUI_Current2() { }



        ///======================================================================================================================================================
    }



    /// <summary>
    /// <para>
    /// <see cref="SingleTon_ScriptableObject{TSbject}"/>를 상속한 ScriptableObject가
    /// <b>새로 생성</b>되거나 <b>이동·리네임</b>될 때 Addressables DefaultGroup에 자동 등록합니다.<br/>
    /// 이미 등록된 항목이 있을 경우
    /// <list type="bullet">
    ///   <item>같은 GUID → 아무것도 하지 않음(중복 로그 방지)</item>
    ///   <item>다른 GUID → 주소 충돌 오류 로그</item>
    /// </list>
    /// </para>
    /// <remarks>
    /// • Editor 전용 클래스이며, 빌드(런타임)에는 포함되지 않습니다.<br/>
    /// • <c>.asset</c> 파일의 <b>실질적 변경(생성·이동)</b>만 처리하고, 단순 Re-import 저장은 무시합니다.
    /// </remarks>
    /// </summary>
    sealed class SingleTonSO_CreationWatcher : AssetPostprocessor
    {
        //. 싱글톤SO가 생성되었을때, 그 싱글톤SO를 즉시 어드레서블에 등록 하는 코드



        private static readonly Type kBaseGeneric = typeof(SingleTon_ScriptableObject<>);  //. 기준 제네릭



        /// <summary>에셋 임포트·이동·리네임 직후 호출되는 콜백.</summary>
        private static void OnPostprocessAllAssets(string[] imported,
                                                   string[] deleted,
                                                   string[] moved,
                                                   string[] movedFrom)
        {
            RegisterAddressables(imported);
            RegisterAddressables(moved);
        }



        //? 전달된 경로 중 ‘.asset’만 골라 Addressables 자동 등록
        private static void RegisterAddressables(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return; //! Addressables 패키지 없음

            bool dirty = false;

            foreach (string path in paths.Where(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)))
            {
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null) continue;
                if (!IsDerivedFromSingleTon(so.GetType())) continue;                        //. 싱글톤 SO만 대상

                string guid = AssetDatabase.AssetPathToGUID(path);
                string expectedAddr = so.GetType().Name;
                var existingEntry = settings.FindAssetEntry(guid);

                //? 이미 동일 GUID로 등록돼 있고, 주소도 일치 → 아무 작업·로그 없음
                if (existingEntry != null && existingEntry.address == expectedAddr)
                    continue;

                //? 주소가 같은데 GUID가 다른 경우 → 충돌
                var addrDup = settings.groups
                                      .SelectMany(g => g.entries)
                                      .FirstOrDefault(e => e.address == expectedAddr);

                if (addrDup != null && addrDup.guid != guid)
                {
                    Debug.LogError($"❌ 싱글톤 SO Addressables 주소 충돌!\n"
                                 + $"  기존 : {addrDup.address} ↦ {AssetDatabase.GUIDToAssetPath(addrDup.guid)}\n"
                                 + $"  신규 : {expectedAddr} ↦ {path}");
                    continue;   //! 충돌 시 자동 수정하지 않고 건너뜀
                }

                //? 신규 등록 또는 주소 갱신
                var entry = existingEntry ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.address = expectedAddr;

                dirty = true;
                Debug.Log($"✅ 싱글톤 SO Addressable 등록/갱신: <b>{entry.address}</b>\n↳ {path}");
            }

            if (dirty)
            {
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
                AssetDatabase.SaveAssets();
            }
        }



        /// <summary>타입이 <see cref="SingleTon_ScriptableObject{TSbject}"/> 파생인지 확인.</summary>
        private static bool IsDerivedFromSingleTon(Type t)
        {
            while (t != null && t != typeof(ScriptableObject))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == kBaseGeneric) return true;
                t = t.BaseType;
            }
            return false;
        }
    }



    ///======================================================================================================================================================



    /// <summary>
    /// 별도의 <see cref="Editor"/>나 Custom Editor에서 구현할 수 있는 인터페이스로,
    /// <c>DrawInspectorGUI_Current</c> 메서드를 제공하여 인스펙터 GUI를 그릴 수 있도록 합니다.
    /// </summary>
    public interface IDrawInspectorGUI_Current
    {
        /// <summary>
        /// 인스펙터에 보여줄 GUI를 그리는 메서드입니다.
        /// 구현하는 클래스에서 해당 로직을 작성합니다.
        /// </summary>
        void DrawInspectorGUI_Current();

        /// <summary>
        /// <paramref name="editor"/>가 <see cref="IDrawInspectorGUI_Current"/>를 구현하면,
        /// <c>DrawInspectorGUI_Current</c>를 호출합니다. (간단한 래퍼 메서드)
        /// </summary>
        /// <param name="editor">검사 대상 에디터</param>
        public static void TryDraw(Editor editor)
        {
            (editor as IDrawInspectorGUI_Current)?.DrawInspectorGUI_Current();
        }
    }



    /// <summary>
    /// Odin Inspector에서 사용하는 <see cref="PropertyTree"/> 접근성을 제공하기 위한 인터페이스입니다.
    /// </summary>
    public interface IOdinPropertyTree
    {
        /// <summary>
        /// Odin Inspector가 사용하는 <see cref="PropertyTree"/> 인스턴스입니다.
        /// </summary>
        PropertyTree OdinPropertyTree { get; }
    }



    ///======================================================================================================================================================
}
