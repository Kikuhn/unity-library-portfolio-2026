using NUnit.Framework;

namespace Pan.SpinePackage.Tests
{
    public class EnumIndexTests
    {
        private enum SkinLayer
        {
            Base,
            Face
        }

        [Test]
        public void GenericSkinLayerHolder_ReturnsItsManager()
        {
            var manager = new SkelSbject.EnumIndex_SkinLayer<SkinLayer>(false);
            var holder =
                (SkelSbject.IHoldEnumIndex_SkinLayer<SkinLayer>)manager;

            Assert.That(holder.EnumIndex_SkinLayer, Is.SameAs(manager));
        }
    }
}
