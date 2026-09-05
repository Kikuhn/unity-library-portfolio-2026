using UnityEditor;
using UnityEngine;



//? PlayMode가 아닌 유니티 에디터상에서 UniTask를 실행할때, 무한정 대기 상태가 되는것을 방지하기위한 매니저와 윈도우가 있는 정도의 코드



namespace Pan.Util.Editors
{
    /// <summary>
    /// 에디터 모드에서 InstantiateAsync 등의 비동기 작업이 대기 상태가 되지 않도록
    /// EditorApplication.update에서 주기적으로 QueuePlayerLoopUpdate를 호출해주는 매니저.
    /// </summary>
    //[InitializeOnLoad]
    public static class EditorAsyncUpdateManager
    {
        // 에디터 초기 로드(Domain Reload) 시점에 실행
        //static EditorAsyncUpdateManager()
        //{
            //    // 원하는 기본값(true/false)를 설정
            //    _isEnabled = true;            

            //// 처음부터 켜두고 싶다면, 아래 메서드로 등록
            //EditorApplication.update += OnEditorUpdate;
        //}

        /// <summary>
        /// 기능 On/Off 상태. (토글 가능)
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;

                if (_isEnabled)
                {
                    // 기능을 켜면, EditorApplication.update에 등록
                    EditorApplication.update -= OnEditorUpdate;
                    EditorApplication.update += OnEditorUpdate;
                }
                else
                {
                    // 기능을 끄면, EditorApplication.update에서 제거
                    EditorApplication.update -= OnEditorUpdate;
                }
            }
        }
        private static bool _isEnabled;

        /// <summary>
        /// EditorApplication.update 때마다 호출
        /// </summary>
        private static void OnEditorUpdate()
        {
            if (_isEnabled)
            {
                // 에디터 모드에서도 PlayerLoop를 돌려주어 비동기 처리가 중단되지 않도록
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }



    /// <summary>
    /// 이 기능(QueuePlayerLoopUpdate) On/Off 상태를 보여주고 변경할 수 있는 에디터 윈도우
    /// </summary>
    public class EditorAsyncUpdateWindow : EditorWindow
    {
        //! Unity 6000.0.32f1에서 에디터에서 InstantiateAsync 가 await되는 문제를 해결


        // 메뉴에서 실행: Window/Editor Async Update 라는 식으로 뜨게끔
        [MenuItem("판/Editor 비동기 업데이트 도우미")]
        private static void Open()
        {
            var window = GetWindow<EditorAsyncUpdateWindow>();
            window.titleContent = new GUIContent("Editor 비동기 업데이트 도우미", EditorGUIUtility.IconContent("d_Refresh").image);
            window.Show();
        }



        private void OnGUI()
        {
            EditorGUILayout.LabelField("Editor Async Update Manager", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "에디터 모드에서 InstantiateAsync 등 비동기 await이 멈추지 않도록 " +
                "QueuePlayerLoopUpdate를 매 프레임마다 호출해 주는 기능입니다.\n\n" +
                "아래 체크박스로 ON/OFF 할 수 있습니다.",
                MessageType.Info
            );

            // Toggle 표시
            bool newValue = EditorGUILayout.Toggle("Enable Async Update", EditorAsyncUpdateManager.IsEnabled);
            if (newValue != EditorAsyncUpdateManager.IsEnabled)
            {
                EditorAsyncUpdateManager.IsEnabled = newValue;
            }

            GUILayout.Space(10);
            if (EditorAsyncUpdateManager.IsEnabled)
            {
                EditorGUILayout.HelpBox("Async Update is currently ENABLED.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Async Update is currently DISABLED.", MessageType.Warning);
            }
        }
    }
}