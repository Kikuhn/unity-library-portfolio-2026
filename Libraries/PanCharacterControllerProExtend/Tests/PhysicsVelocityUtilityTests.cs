using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class PhysicsVelocityUtilityTests
{
    private static Vector3 FromDelta(Vector3 delta, float deltaTime)
    {
        var utilityType = typeof(CCPObject).Assembly.GetType("PhysicsVelocityUtility", true);
        var method = utilityType.GetMethod("FromDelta", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (Vector3)method.Invoke(null, new object[] { delta, deltaTime });
    }

    [Test]
    public void FromDelta_ConvertsStepDeltaToMetersPerSecond()
    {
        var velocity = FromDelta(new Vector3(0.1f, 0.2f, -0.1f), 0.02f);

        Assert.That(velocity.x, Is.EqualTo(5f).Within(0.00001f));
        Assert.That(velocity.y, Is.EqualTo(10f).Within(0.00001f));
        Assert.That(velocity.z, Is.EqualTo(-5f).Within(0.00001f));
    }

    [TestCase(0f)]
    [TestCase(-0.02f)]
    public void FromDelta_WithNonPositiveDeltaTime_ReturnsZero(float deltaTime)
    {
        Assert.AreEqual(Vector3.zero, FromDelta(Vector3.one, deltaTime));
    }

    [Test]
    public void RuntimeDiagnostics_AreGroupedUnderCollapsedFoldout()
    {
        string[] diagnosticMembers =
        {
            nameof(CCPObject.PhysicsPosition),
            nameof(CCPObject.PreviousFramePhysicsPosition),
            nameof(CCPObject.ExpectedVelocity),
            nameof(CCPObject.ActualVelocity),
            "editor_IsMovingExpected",
            "editor_IsMovingExpectedX",
            "editor_IsMovingActual",
            "editor_IsMovingActualX",
            nameof(CCPObject.WillApplyVelocity),
            nameof(CCPObject.CharacterActorVelocity),
            nameof(CCPObject.IsGrounded),
            nameof(CCPObject.IsStablyGrounded),
            "editorTimeInfo",
            "fixedUpdateExecutor"
        };

        foreach (string memberName in diagnosticMembers)
        {
            MemberInfo member = typeof(CCPObject)
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault();

            Assert.IsNotNull(member, $"진단 멤버를 찾을 수 없습니다: {memberName}");

            object foldout = member.GetCustomAttributes(false)
                .SingleOrDefault(attribute => attribute.GetType().FullName == "Sirenix.OdinInspector.FoldoutGroupAttribute");

            Assert.IsNotNull(foldout, $"실시간 진단 FoldoutGroup이 없습니다: {memberName}");
            Assert.AreEqual("실시간 진단", ReadInheritedField(foldout, "GroupName"), memberName);
            Assert.AreEqual(false, foldout.GetType().GetProperty("Expanded")?.GetValue(foldout), memberName);
        }
    }

    [Test]
    public void EditorDiagnostics_UseFrameCaches()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.IsNotNull(typeof(CCPObject).GetField("editorMovementSnapshotFrame", flags));
        Assert.IsNotNull(typeof(CCPObject).GetMethod("Editor_RefreshMovementSnapshot", flags));
        Assert.IsNotNull(typeof(CCPObject).GetField("editorTimeInfoFrame", flags));
        Assert.IsNotNull(typeof(CCPObject).GetField("editorCachedTimeInfo", flags));
    }

    private static object ReadInheritedField(object target, string fieldName)
    {
        for (System.Type type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(target);
            }
        }

        Assert.Fail($"속성 필드를 찾을 수 없습니다: {fieldName}");
        return null;
    }
}
