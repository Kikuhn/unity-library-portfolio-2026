using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;



namespace Pan.Util.Editors
{
    public class MenuItemEvent
    {
        [MenuItem("판액션/오브젝트 스냅! %#q")] // Ctrl + Alt + Shift + Q
        private static void SnapPosition_GridUnit()
        {
            var selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length > 0)
            {
                foreach (var gameObject in selectedObjects)
                {
                    if (gameObject.TryGetComponent<ISnapAbleTransform>(out var snapAble))
                    {
                        SU_CustomEditor.UndoRecordObject(gameObject.transform, snapAble.SnapTransform, $"{gameObject.name}의 스냅!");
                    }
                }
            }
        }

        [MenuItem("판액션/도메인 + 씬 리로드 %&r")] // Ctrl + Alt + R
        private static void ReloadDomainAndScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Play Mode 실행/전환 중에는 도메인 + 씬 리로드를 실행하지 않습니다.");
                return;
            }

            if (EditorApplication.isCompiling)
            {
                Debug.LogWarning("컴파일 중에는 도메인 + 씬 리로드를 실행하지 않습니다.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            var sceneReloaded = false;

            if (!string.IsNullOrEmpty(activeScene.path))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

                EditorSceneManager.OpenScene(activeScene.path);
                sceneReloaded = true;
            }

            EditorUtility.RequestScriptReload();
            Debug.Log(sceneReloaded ? "도메인 리로드를 요청하고 활성 씬을 다시 열었습니다." : "저장된 활성 씬이 없어 도메인 리로드만 요청했습니다.");
        }
    }
}