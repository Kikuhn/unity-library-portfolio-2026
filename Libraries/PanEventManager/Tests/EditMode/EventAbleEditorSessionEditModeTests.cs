using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Pan.Event;
using Pan.EventManagers.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;



namespace Pan.EventManagers.Tests
{
    public sealed class EventAbleEditorSessionEditModeTests
    {
        private sealed class EventAbleEditorSessionHarnessWindow : EditorWindow
        {
            internal EventAbleEditorSession Session;
            internal bool? LegacyAddPropertyVisible;
            internal bool DrawAttachedValuesOnly;
            internal bool StructuralActionPropertyFound;
            internal bool StructuralActionPropertyVisible;



            private void OnGUI()
            {
                bool drawn = DrawAttachedValuesOnly
                    ? Session?.DrawAttachedEventValuesOnly() == true
                    : Session?.Draw() == true;
                if (!drawn) { return; }

                InspectorProperty legacyAddProperty = Session.PropertyTree?
                    .GetPropertyAtPath("Editor_Test_SelectEventValue");
                if (legacyAddProperty != null)
                {
                    LegacyAddPropertyVisible = legacyAddProperty.State.Visible;
                }

                StructuralActionPropertyFound = false;
                StructuralActionPropertyVisible = false;
                foreach (InspectorProperty property in Session.PropertyTree.EnumerateTree(true, false))
                {
                    if (property.Name != "Editor_Pin" &&
                        property.Name != "Editor_Unpin" &&
                        property.Name != "Editor_Remove")
                    {
                        continue;
                    }

                    StructuralActionPropertyFound = true;
                    StructuralActionPropertyVisible |= property.State.Visible;
                }
            }



            private void OnDisable()
            {
                Session?.Dispose();
                Session = null;
            }
        }



        [SetUp]
        public void SetUp()
        {
            ResetGeneralManager();
            SetEventValueDiscoveryCache(
                typeof(SessionGeneralValue),
                typeof(SessionOwnerValue),
                typeof(SessionOtherOwnerValue));
            PanEventGeneralManager.Initialize();
        }



        [TearDown]
        public void TearDown()
        {
            ResetGeneralManager();
            SetEventValueDiscoveryCache(null);
        }



        [Test]
        public void Session_AddableTypes_OnlyIncludesCurrentOwnerCompatibleTypesAndAttachesThroughPool()
        {
            var owner = new SessionOwner();

            using (var session = new EventAbleEditorSession(owner))
            {
                Assert.That(session.AddableEventValueTypes, Has.Member(typeof(SessionGeneralValue)));
                Assert.That(session.AddableEventValueTypes, Has.Member(typeof(SessionOwnerValue)));
                Assert.That(session.AddableEventValueTypes, Has.No.Member(typeof(SessionOtherOwnerValue)));

                Assert.IsFalse(session.TryAddEventValue(typeof(SessionOtherOwnerValue), out _));
                Assert.IsTrue(session.TryAddEventValue(typeof(SessionGeneralValue), out PanBaseEventValue addedValue));

                var typedValue = addedValue as SessionGeneralValue;
                Assert.IsNotNull(typedValue);
                Assert.AreEqual(1, typedValue.EnableCount);
                Assert.IsTrue(typedValue.Valid_CurrentEventAble);
                Assert.AreSame(owner.EventAble, typedValue.CurrentEventAble);
                Assert.AreSame(typedValue, owner.EventAble.Peek<SessionGeneralValue>());
                Assert.That(session.AddableEventValueTypes, Has.No.Member(typeof(SessionGeneralValue)));
            }
        }



        [Test]
        public void Session_PropertyTree_ExposesAllEventValueFieldsForEditing()
        {
            var owner = new SessionOwner();

            Assert.IsTrue(EventAbleEditorSession.TryGetAttachedEventValueCount(owner, out int emptyCount));
            Assert.AreEqual(0, emptyCount);

            using (var session = new EventAbleEditorSession(owner))
            {
                Assert.IsTrue(session.TryAddEventValue(typeof(SessionGeneralValue), out PanBaseEventValue addedValue));
                Assert.AreEqual(1, session.AttachedEventValueCount);
                Assert.IsTrue(EventAbleEditorSession.TryGetAttachedEventValueCount(owner, out int attachedCount));
                Assert.AreEqual(1, attachedCount);

                CommitInspectorView(owner.EventAble);
                PropertyTree propertyTree = session.PropertyTree;
                Assert.IsTrue(session.TryGetEventValueProperty(addedValue, out InspectorProperty eventValueProperty));
                InspectorProperty editableProperty = eventValueProperty?.Children[nameof(SessionGeneralValue.EditableAmount)];

                Assert.IsNotNull(eventValueProperty);
                Assert.AreSame(addedValue, eventValueProperty.ValueEntry.WeakSmartValue);
                Assert.IsNotNull(editableProperty);
                Assert.IsTrue(eventValueProperty.State.Visible);
                Assert.IsTrue(editableProperty.State.Visible);
                Assert.IsTrue(editableProperty.ValueEntry.IsEditable);

                editableProperty.ValueEntry.WeakSmartValue = 73;
                propertyTree.ApplyChanges();

                Assert.AreEqual(73, ((SessionGeneralValue)addedValue).EditableAmount);
            }
        }



        [Test]
        public void Session_Remove_DisablesAndReturnsExactValueToPoolForReuse()
        {
            var owner = new SessionOwner();

            using (var session = new EventAbleEditorSession(owner))
            {
                Assert.IsTrue(session.TryAddEventValue(typeof(SessionGeneralValue), out PanBaseEventValue addedValue));
                var typedValue = (SessionGeneralValue)addedValue;

                Assert.IsTrue(session.TryRemoveEventValue(typedValue));
                Assert.AreEqual(1, typedValue.DisableCount);
                Assert.IsFalse(typedValue.Valid_CurrentEventAble);
                Assert.IsFalse(owner.EventAble.CheckValueTable<SessionGeneralValue>(false));
                Assert.That(session.AddableEventValueTypes, Has.Member(typeof(SessionGeneralValue)));

                SessionGeneralValue pooled =
                    PanEventGeneralManager.EventValueManager.PopEventValue<SessionGeneralValue>(null);

                Assert.AreSame(typedValue, pooled);
                Assert.IsNull(PanEventGeneralManager.EventValueManager.PopEventValue<SessionGeneralValue>(null));
                PanEventGeneralManager.EventValueManager.PushEventValue(pooled);

                Assert.IsTrue(session.TryAddEventValue(typeof(SessionGeneralValue), out PanBaseEventValue reusedValue));
                Assert.AreSame(typedValue, reusedValue);
                Assert.AreEqual(2, typedValue.EnableCount);
            }
        }



        [Test]
        public void Session_Dispose_ReleasesPropertyTreeAndRejectsFurtherOperations()
        {
            var owner = new SessionOwner();
            var session = new EventAbleEditorSession(owner);

            Assert.IsTrue(session.TryAddEventValue(typeof(SessionGeneralValue), out PanBaseEventValue attachedValue));
            Assert.IsNotNull(session.PropertyTree);

            session.Dispose();
            session.Dispose();

            Assert.IsFalse(session.IsValid);
            Assert.IsNull(session.Owner);
            Assert.IsNull(session.EventAble);
            Assert.IsNull(session.PropertyTree);
            Assert.IsEmpty(session.AddableEventValueTypes);
            Assert.IsFalse(session.TryAddEventValue(typeof(SessionOwnerValue), out _));
            Assert.IsFalse(session.TryRemoveEventValue(attachedValue));
            Assert.IsTrue(attachedValue.Valid_CurrentEventAble);

            owner.EventAble.Reset();
        }



        [Test]
        public void Session_StaleOwner_DisposesWithoutMutatingPreviouslyObservedEventAble()
        {
            var owner = new SessionOwner();
            EventAble oldEventAble = owner.EventAble;
            var session = new EventAbleEditorSession(owner);

            Assert.IsTrue(session.TryAddEventValue(typeof(SessionGeneralValue), out PanBaseEventValue attachedValue));

            owner.ThrowWhenReadingEventAble = true;

            Assert.IsFalse(session.IsValid);
            Assert.IsFalse(EventAbleEditorSession.TryGetAttachedEventValueCount(owner, out _));
            Assert.IsNull(session.PropertyTree);
            Assert.IsFalse(session.TryRemoveEventValue(attachedValue));
            Assert.IsTrue(attachedValue.Valid_CurrentEventAble);
            Assert.AreSame(oldEventAble, attachedValue.CurrentEventAble);

            owner.ThrowWhenReadingEventAble = false;
            oldEventAble.Reset();
            session.Dispose();
        }



        [Test]
        public void Session_TryCreate_ReturnsFalseForStaleOwnerWithoutThrowing()
        {
            var owner = new SessionOwner
            {
                ThrowWhenReadingEventAble = true
            };

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(EventAbleEditorSession.TryCreate(owner, out EventAbleEditorSession session));
                Assert.IsNull(session);
            });
        }



        [UnityTest]
        public IEnumerator Session_Draw_HidesLegacyAddDropdownOnlyInsideSessionTree()
        {
            var owner = new SessionOwner();
            EventAbleEditorSessionHarnessWindow window = null;

            using (PropertyTree directTree = PropertyTree.Create(owner.EventAble))
            {
                directTree.UpdateTree();
                InspectorProperty directLegacyProperty = directTree.GetPropertyAtPath(
                    "Editor_Test_SelectEventValue");

                Assert.IsNotNull(directLegacyProperty);
                Assert.IsTrue(directLegacyProperty.State.Visible);
            }

            try
            {
                window = ScriptableObject.CreateInstance<EventAbleEditorSessionHarnessWindow>();
                window.Session = new EventAbleEditorSession(owner);
                window.position = new Rect(100f, 100f, 500f, 600f);
                window.ShowUtility();

                for (int i = 0; i < 3; i++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.AreEqual(false, window.LegacyAddPropertyVisible);
            }
            finally
            {
                if (window != null)
                {
                    window.Close();
                    UnityEngine.Object.DestroyImmediate(window);
                }
            }

            using (PropertyTree directTree = PropertyTree.Create(owner.EventAble))
            {
                directTree.UpdateTree();
                InspectorProperty directLegacyProperty = directTree.GetPropertyAtPath(
                    "Editor_Test_SelectEventValue");

                Assert.IsNotNull(directLegacyProperty);
                Assert.IsTrue(directLegacyProperty.State.Visible);
            }
        }



        [UnityTest]
        public IEnumerator Session_DrawAttachedEventValuesOnly_HidesStructuralActionsAndKeepsFieldsEditable()
        {
            var owner = new SessionOwner();
            EventAbleEditorSessionHarnessWindow window = null;

            try
            {
                window = ScriptableObject.CreateInstance<EventAbleEditorSessionHarnessWindow>();
                window.Session = new EventAbleEditorSession(owner);
                Assert.IsTrue(window.Session.TryAddEventValue(
                    typeof(SessionGeneralValue),
                    out PanBaseEventValue attachedValue));
                CommitInspectorView(owner.EventAble);
                window.DrawAttachedValuesOnly = true;
                window.position = new Rect(100f, 100f, 500f, 600f);
                window.ShowUtility();

                for (int i = 0; i < 3; i++)
                {
                    window.Repaint();
                    yield return null;
                }

                Assert.AreEqual(false, window.LegacyAddPropertyVisible);
                Assert.IsTrue(window.StructuralActionPropertyFound);
                Assert.IsFalse(window.StructuralActionPropertyVisible);
                Assert.AreSame(attachedValue, owner.EventAble.Peek<SessionGeneralValue>());
                Assert.IsTrue(window.Session.TryGetEventValueProperty(
                    attachedValue,
                    out InspectorProperty eventValueProperty));
                Assert.IsTrue(eventValueProperty.Children[nameof(SessionGeneralValue.EditableAmount)]
                    .ValueEntry.IsEditable);
            }
            finally
            {
                if (window != null)
                {
                    window.Close();
                    UnityEngine.Object.DestroyImmediate(window);
                }
            }
        }



        private static void CommitInspectorView(EventAble eventAble)
        {
            MethodInfo commitMethod = typeof(EventAble).GetMethod(
                "EditorCommitInspectorView",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(commitMethod);
            Assert.AreEqual(true, commitMethod.Invoke(eventAble, null));
        }



        private static void ResetGeneralManager()
        {
            SetStaticAutoProperty(nameof(PanEventGeneralManager.IsInitialize), false);
            SetStaticAutoProperty(nameof(PanEventGeneralManager.EventManager), null);
            SetStaticAutoProperty(nameof(PanEventGeneralManager.EventValueManager), null);
        }



        private static void SetStaticAutoProperty(string propertyName, object value)
        {
            FieldInfo field = typeof(PanEventGeneralManager).GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            field.SetValue(null, value);
        }



        private static void SetEventValueDiscoveryCache(params Type[] eventValueTypes)
        {
            FieldInfo field = typeof(PanEventsInitializeSettingSbjectBase).GetField(
                "panBaseEventValueTypesAll",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            field.SetValue(null, eventValueTypes);
        }
    }



    public sealed class SessionOwner : IEventAble
    {
        private readonly EventAble eventAble;



        public SessionOwner()
        {
            eventAble = new EventAble(this, 3);
        }



        public bool ThrowWhenReadingEventAble { get; set; }



        public EventAble EventAble
        {
            get
            {
                if (ThrowWhenReadingEventAble)
                {
                    throw new InvalidOperationException("stale owner");
                }

                return eventAble;
            }
        }
    }



    public sealed class SessionOtherOwner : IEventAble
    {
        public SessionOtherOwner()
        {
            EventAble = new EventAble(this, 1);
        }



        public EventAble EventAble { get; }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class SessionGeneralValue : PanBaseEventValue.EventAbles<SessionGeneralValue>
    {
        public int EnableCount { get; private set; }
        public int DisableCount { get; private set; }

        [ShowInInspector]
        public int EditableAmount;



        protected override void Enable()
        {
            EnableCount++;
        }



        protected override void Disable()
        {
            DisableCount++;
        }
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class SessionOwnerValue :
        PanBaseEventValue.CustomEventAbles<SessionOwnerValue, SessionOwner>
    {
    }



    [PanEventUsageProfileAttribute(PanEventUsageProfile.ValidationOnly)]
    public sealed class SessionOtherOwnerValue :
        PanBaseEventValue.CustomEventAbles<SessionOtherOwnerValue, SessionOtherOwner>
    {
    }
}
