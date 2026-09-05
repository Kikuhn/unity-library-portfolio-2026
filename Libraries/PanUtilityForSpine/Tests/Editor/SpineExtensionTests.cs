using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spine;

namespace Pan.SpineUtil.Tests
{
    public class SpineExtensionTests
    {
        private const int FaceSlot = 0;
        private const int ExtraSlot = 1;
        private const string Placeholder = "attachment";

        [Test]
        public void SetSkinOnlyAttachment_ReplacesAndAddsOnlyAttachments()
        {
            var originalAttachment = new PointAttachment("original");
            var replacementAttachment = new PointAttachment("replacement");
            var addedAttachment = new PointAttachment("added");
            var baseSkin = CreateSkin("base", (FaceSlot, originalAttachment));
            var changedSkin = CreateSkin(
                "changed",
                (FaceSlot, replacementAttachment),
                (ExtraSlot, addedAttachment));

            baseSkin.SetSkin_OnlyAttachment(changedSkin);

            Assert.That(baseSkin.GetAttachment(FaceSlot, Placeholder), Is.SameAs(replacementAttachment));
            Assert.That(baseSkin.GetAttachment(ExtraSlot, Placeholder), Is.SameAs(addedAttachment));
            Assert.That(baseSkin.Bones.Count, Is.Zero);
            Assert.That(baseSkin.Constraints.Count, Is.Zero);
        }

        [TestCase(UndoOverload.Array)]
        [TestCase(UndoOverload.ReadOnlyList)]
        [TestCase(UndoOverload.Collection)]
        [TestCase(UndoOverload.Skin)]
        public void SetSkinUndoAttachment_RestoresOriginalAndRemovesNewEntry(UndoOverload overload)
        {
            var originalAttachment = new PointAttachment("original");
            var replacementAttachment = new PointAttachment("replacement");
            var addedAttachment = new PointAttachment("added");
            var originalSkin = CreateSkin("original", (FaceSlot, originalAttachment));
            var changedSkin = CreateSkin(
                "changed",
                (FaceSlot, replacementAttachment),
                (ExtraSlot, addedAttachment));
            var composedSkin = new Skin("composed");
            composedSkin.AddSkin(originalSkin);
            composedSkin.SetSkin_OnlyAttachment(changedSkin);
            var entries = changedSkin.Attachments.ToArray();

            switch (overload)
            {
                case UndoOverload.Array:
                    composedSkin.SetSkin_UndoAttachment(originalSkin, entries);
                    break;
                case UndoOverload.ReadOnlyList:
                    composedSkin.SetSkin_UndoAttachment(
                        originalSkin,
                        (IReadOnlyList<Skin.SkinEntry>)entries);
                    break;
                case UndoOverload.Collection:
                    composedSkin.SetSkin_UndoAttachment(
                        originalSkin,
                        (ICollection<Skin.SkinEntry>)entries);
                    break;
                case UndoOverload.Skin:
                    composedSkin.SetSkin_UndoAttachment(originalSkin, changedSkin);
                    break;
            }

            Assert.That(composedSkin.GetAttachment(FaceSlot, Placeholder), Is.SameAs(originalAttachment));
            Assert.That(composedSkin.GetAttachment(ExtraSlot, Placeholder), Is.Null);
        }

        [Test]
        public void DrawOrderTimelineDeepCopy_DoesNotShareMutableArrays()
        {
            var source = new DrawOrderTimeline(2);
            source.SetFrame(0, 0.25f, new[] { 1, 0 });
            source.SetFrame(1, 1f, null);

            var copy = source.DeepCopy();

            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(copy.Frames, Is.Not.SameAs(source.Frames));
            Assert.That(copy.Frames, Is.EqualTo(source.Frames));
            Assert.That(copy.DrawOrders, Is.Not.SameAs(source.DrawOrders));
            Assert.That(copy.DrawOrders[0], Is.Not.SameAs(source.DrawOrders[0]));
            Assert.That(copy.DrawOrders[0], Is.EqualTo(source.DrawOrders[0]));
            Assert.That(copy.DrawOrders[1], Is.Null);

            copy.Frames[0] = 9f;
            copy.DrawOrders[0][0] = 99;

            Assert.That(source.Frames[0], Is.EqualTo(0.25f));
            Assert.That(source.DrawOrders[0][0], Is.EqualTo(1));
        }

        private static Skin CreateSkin(
            string name,
            params (int slot, Attachment attachment)[] entries)
        {
            var skin = new Skin(name);
            foreach (var entry in entries)
            {
                skin.SetAttachment(entry.slot, Placeholder, entry.attachment);
            }

            return skin;
        }

        public enum UndoOverload
        {
            Array,
            ReadOnlyList,
            Collection,
            Skin
        }
    }
}
