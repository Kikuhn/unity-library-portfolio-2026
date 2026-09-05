#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Sirenix.OdinInspector;

public class CollectionPerformanceTester : MonoBehaviour
{
    [Title("테스트 설정")]
    [Tooltip("테스트할 데이터 개수")]
    public int dataCount = 1000000;

    [Tooltip("새로운 객체를 생성하여 테스트할지 여부 (false면 동일한 객체를 추가)")]
    public bool createNewObjects = true;

    private List<TestData> testList;
    private HashSet<TestData> testHashSet;

    [Button("컬렉션 데이터 초기화")]
    private void InitializeCollections()
    {
        testList = new List<TestData>(dataCount);
        testHashSet = new HashSet<TestData>();

        for (int i = 0; i < dataCount; i++)
        {
            var obj = createNewObjects ? new TestData(i) : new TestData(0);

            testList.Add(obj);
            testHashSet.Add(obj);
        }

        UnityEngine.Debug.Log($"✅ 데이터 초기화 완료! ({dataCount} 개 추가됨)");
    }

    [Button("List<T> 성능 테스트")]
    private void TestListPerformance()
    {
        if (testList == null || testList.Count == 0)
        {
            UnityEngine.Debug.LogError("❌ List가 초기화되지 않았습니다. '컬렉션 데이터 초기화'를 먼저 실행하세요.");
            return;
        }

        Stopwatch sw = Stopwatch.StartNew();
        int sum = 0;
        foreach (var item in testList)
        {
            sum += item.Value;
        }
        sw.Stop();

        UnityEngine.Debug.Log($"📌 List<T> Foreach 실행 시간: {sw.ElapsedMilliseconds} ms (총합: {sum})");
    }

    [Button("HashSet<T> 성능 테스트")]
    private void TestHashSetPerformance()
    {
        if (testHashSet == null || testHashSet.Count == 0)
        {
            UnityEngine.Debug.LogError("❌ HashSet이 초기화되지 않았습니다. '컬렉션 데이터 초기화'를 먼저 실행하세요.");
            return;
        }

        Stopwatch sw = Stopwatch.StartNew();
        int sum = 0;
        foreach (var item in testHashSet)
        {
            sum += item.Value;
        }
        sw.Stop();

        UnityEngine.Debug.Log($"📌 HashSet<T> Foreach 실행 시간: {sw.ElapsedMilliseconds} ms (총합: {sum})");
    }
}

[Serializable]
public class TestData
{
    public int Value;

    public TestData(int value)
    {
        Value = value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is TestData data && Value == data.Value;
    }
}
#endif
