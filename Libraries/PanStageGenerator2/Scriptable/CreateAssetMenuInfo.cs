using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.StageGenerators
{
    internal static class CreateAssetMenuInfo
    {
        internal const string PATH = "스테이지 생성기 설정 Scriptable";

        internal const string ROOM_GIZMO = PATH + "/방 Gizmo 설정 Scriptable";
        internal const string STAGEGEN_SETTING = PATH + "/설정 Scriptable";
        internal const string STAGEGEN_CALCULATE_SETTING = PATH + "/연산 설정 Scriptable";
        internal const string STAGEGEN_PREFAB_SETTING = PATH + "/프리팹 설정 Scriptable";
        internal const string STAGEGEN_GIZMO_SETTING = PATH + "/기즈모 설정 Scriptable";
        internal const string STAGEGEN_SINGLETON_SETTING = PATH + "/통합 설정 Scriptable (Singleton)";
        internal const string STAGEGEN_GRID_SETTING = PATH + "/그리드 설정 Scriptable";

        internal const string STAGEGEN_CUSTOMEVENTCREATER = PATH + "/커스텀 이벤트 생성기 Scriptable (Singleton)";

        internal const string STAGEGEN_CUSTOMEVENTFOLDER = PATH + "/커스텀 이벤트";

        internal const string STAGEGEN_SINGLETON_SO_MANAGER = PATH + "/StageGenerator2 싱글톤 SO 매니저 (패키지에 단 하나만 생성할것)";
    }
}