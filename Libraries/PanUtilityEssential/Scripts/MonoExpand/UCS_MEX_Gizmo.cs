using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using DG.Tweening;
using Pan.Util;



//? [Monobehaviour Expand] 기즈모들을 정리한 정도의 코드



namespace Pan.Util
{
    ///======================================================================================================================================================



    //? 기즈모 클래스



#if UNITY_EDITOR



    /// <summary>
    /// 기즈모(Gizmo) 기능을 제공하는 기본 클래스입니다. <br/>
    /// 특정 <see cref="MonoBehaviour"/> 타입을 기반으로 기즈모를 그릴 수 있도록 합니다.
    /// <b>(UNITY_EDITOR 에서만 컴파일됨!)</b>
    /// </summary>
    /// <typeparam name="T">기즈모를 적용할 대상 MonoBehaviour 타입</typeparam>
    [Serializable]
    public abstract class BaseGizmoClass<T> where T : MonoBehaviour
    {
        /// <summary>
        /// 기즈모 사용 여부를 설정하거나 가져옵니다.
        /// </summary>
        public virtual bool UseGizmo { get => useGizmo; set => useGizmo = value; }

        /// <summary>
        /// 기즈모 사용 여부를 저장하는 직렬화된 필드입니다.
        /// 기본값은 <c>true</c>입니다.
        /// </summary>
        [SerializeField] public bool useGizmo = true;

        /// <summary>
        /// 기즈모를 그리는 메서드를 실행합니다.
        /// </summary>
        /// <param name="main">기즈모를 그릴 대상 <typeparamref name="T"/> 객체</param>
        public void OnDrawGizmos(T main)
        {
            if (UseGizmo) { DrawGizmo(main); }
        }

        /// <summary>
        /// 기즈모를 그리는 로직을 구현해야 하는 추상 메서드입니다.
        /// </summary>
        /// <param name="main">기즈모를 그릴 대상 <typeparamref name="T"/> 객체</param>
        protected abstract void DrawGizmo(T main);
    }




#endif



    #region Legacy 기즈모 스크립트기반일때 에디터 버튼 구현했던거

    //public static void Editor_DrawGizmoToggleButton<TGizmo>(UnityEditor.Editor editor) where TGizmo : BaseGizmoScript, new()
    //{
    //    if (editor.targets.Length == 0)
    //    {
    //        return;
    //    }

    //    // 첫 번째 선택된 오브젝트를 기준으로 버튼을 결정
    //    var firstGameObject = ((Component)editor.targets[0]).gameObject;

    //    bool hasGizmo = firstGameObject.TryGetComponent<TGizmo>(out var _);

    //    if (hasGizmo)
    //    {
    //        UnityEditor.SU_CustomEditor.Render_ButtonWithStyle(editor, $"{typeof(TGizmo).Name} 기즈모 제거", "", () =>
    //        {
    //            UnityEditor.Undo.SetCurrentGroupName($"{typeof(TGizmo).Name} 기즈모 제거");
    //            int undoGroup = UnityEditor.Undo.GetCurrentGroup();

    //            // 모든 선택된 오브젝트에 대해 기즈모 컴포넌트를 제거
    //            foreach (var target in editor.targets)
    //            {
    //                var gameObject = ((Component)target).gameObject;
    //                if (gameObject.TryGetComponent<TGizmo>(out var component))
    //                {
    //                    UnityEditor.Undo.DestroyObjectImmediate(component);
    //                }
    //            }

    //            UnityEditor.Undo.CollapseUndoOperations(undoGroup);
    //        }, SU_Color.Red_GrapeFruit1(), null);
    //    }
    //    else
    //    {
    //        UnityEditor.SU_CustomEditor.Render_ButtonWithStyle(editor, $"{typeof(TGizmo).Name} 기즈모 추가", "", () =>
    //        {
    //            UnityEditor.Undo.SetCurrentGroupName($"{typeof(TGizmo).Name} 기즈모 추가");
    //            int undoGroup = UnityEditor.Undo.GetCurrentGroup();

    //            // 모든 선택된 오브젝트에 대해 기즈모 컴포넌트를 추가
    //            foreach (var target in editor.targets)
    //            {
    //                var gameObject = ((Component)target).gameObject;
    //                if (!gameObject.TryGetComponent<TGizmo>(out var _))
    //                {
    //                    UnityEditor.Undo.AddComponent<TGizmo>(gameObject).FindMainComponents();
    //                }
    //            }

    //            UnityEditor.Undo.CollapseUndoOperations(undoGroup);
    //        }, SU_Color.Green_Emerald(), null);
    //    }
    //}



    #endregion



    ///======================================================================================================================================================
}