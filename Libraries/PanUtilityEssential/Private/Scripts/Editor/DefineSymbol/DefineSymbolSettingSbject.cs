using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Build;



/// <summary>
/// Define Symbol을 PlayerSettings에 적용하거나, 다양한 작업을 수행하는 정적 유틸리티
/// </summary>
public static class DefineSymbolsUtility
{
    /// <summary>
    /// 입력받은 symbols 리스트를 기존 프로젝트 Define Symbol에서 추가하거나 제거한다.
    /// </summary>
    /// <param name="symbols">추가 또는 제거할 심볼 리스트</param>
    /// <param name="remove">true면 기존 심볼을 제거, false면 추가</param>
    public static void ApplySymbolsToProject(IList<string> symbols, bool remove)
    {
        var buildTargetGroups = GetValidBuildTargetGroups();

        foreach (var group in buildTargetGroups)
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);

            // 기존 심볼 가져오기
            var existingDefines = PlayerSettings
                .GetScriptingDefineSymbols(namedTarget)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            bool changed = false;

            if (remove)
            {
                // 입력받은 심볼을 제거
                foreach (var symbol in symbols)
                {
                    if (existingDefines.Remove(symbol))
                    {
                        changed = true;
                    }
                }
            }
            else
            {
                // 입력받은 심볼을 추가
                foreach (var symbol in symbols)
                {
                    if (!existingDefines.Contains(symbol))
                    {
                        existingDefines.Add(symbol);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                // 최종 문자열로 세팅
                string updatedDefines = string.Join(";", existingDefines);
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, updatedDefines);
                UnityEngine.Debug.Log($"[DefineSymbolsUtility] [{group}] 빌드 타겟에 심볼 업데이트: {updatedDefines}");
            }
        }
    }


    /// <summary>
    /// BuildTargetGroup 중에서 Unknown, 폐기된(Obsolete) 항목을 제외하여 반환
    /// </summary>
    private static List<BuildTargetGroup> GetValidBuildTargetGroups()
    {
        var result = new List<BuildTargetGroup>();
        var allGroups = (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup));
        foreach (var group in allGroups)
        {
            if (group == BuildTargetGroup.Unknown)
                continue;

            var fi = typeof(BuildTargetGroup).GetField(group.ToString());
            if (fi == null) continue;

            var obs = (ObsoleteAttribute[])fi.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            if (obs.Length > 0)
                continue;

            result.Add(group);
        }
        return result;
    }
}



/// <summary>
/// 단순히 Define 심볼 문자열들을 보관하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "DefineSymbolsManager", menuName = CreateAssetMenuInfo.DEFINESYMBOL_SETTINGS, order = CreateAssetMenuInfo.DEFINESYMBOL_SETTINGS_ORDER)]
public class DefineSymbolSettingSbject : ScriptableObject
{
    /// <summary>
    /// 에디터에서 관리할 Define Symbol 목록
    /// </summary>
    [SerializeField]
    private List<string> defineSymbols = new List<string>();

    /// <summary>
    /// 외부에서 접근/수정할 수 있는 프로퍼티
    /// </summary>
    public List<string> DefineSymbols => defineSymbols;
}
