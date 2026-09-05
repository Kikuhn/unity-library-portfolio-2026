#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System;
using UnityEngine.U2D;
using UnityEngine.Tilemaps;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using System.Linq.Expressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using Pan.Util;



//? 에디터 컨트롤 관련 코드가 저장되어있는 정도의 코드 (Editor Only)



namespace Pan.Util.Editors
{
    ///======================================================================================================================================================



    public static class SU_EditorControl
    {
        /// <summary>
        /// 특정 오브젝트가 선택되었는지 확인
        /// </summary>
        /// <param name="transform">확인할 Transform</param>
        /// <param name="checkMultipleSelection">여러 오브젝트 선택 시 포함 여부 확인</param>
        /// <param name="findParent">부모 오브젝트까지 탐색 여부</param>
        /// <param name="findChild">자식 오브젝트까지 탐색 여부</param>
        /// <returns>선택 여부</returns>
        public static bool IsObjectSelected(Transform transform, bool checkMultipleSelection = false, bool findParent = false, bool findChild = false)
        {
            static bool IsAncestor(Transform ancestor, Transform child)
            {
                var current = child;
                while (current != null)
                {
                    if (current == ancestor) { return true; }
                    current = current.parent;
                }
                return false;
            }

            if (checkMultipleSelection)
            {
                foreach (Transform selectedTransform in Selection.transforms)
                {
                    if (selectedTransform == transform)
                    {
                        return true;
                    }

                    if (findParent)
                    {
                        Transform current = selectedTransform;
                        while (current != null)
                        {
                            if (current == transform)
                            {
                                return true;
                            }
                            current = current.parent;
                        }
                    }

                    if (findChild)
                    {
                        if (IsAncestor(selectedTransform, transform))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                Transform active = Selection.activeTransform;

                if (active == transform)
                {
                    return true;
                }

                if (findParent)
                {
                    Transform current = active;
                    while (current != null)
                    {
                        if (current == transform)
                        {
                            return true;
                        }
                        current = current.parent;
                    }
                }

                if (findChild && active != null)
                {
                    if (IsAncestor(active, transform))
                    {
                        return true;
                    }
                }

                return false;
            }
        }




        /// <summary>
        /// 특정 오브젝트가 선택되고 이동 도구가 활성화된 상태인지를 확인하는 메서드
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static bool IsObjectSelectedAndByMoveTool(Transform transform)
        {
            return Selection.activeTransform == transform && Tools.current == Tool.Move;
        }



        /// <summary>
        /// 현재 프리팹 편집 모드인지 확인합니다.
        /// </summary>
        /// <returns>현재 프리팹 편집 모드라면 <c>true</c>, 그렇지 않으면 <c>false</c>를 반환합니다.</returns>
        public static bool IsPrefabEditMode()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() != null;
        }
    }



    ///======================================================================================================================================================
}



#endif