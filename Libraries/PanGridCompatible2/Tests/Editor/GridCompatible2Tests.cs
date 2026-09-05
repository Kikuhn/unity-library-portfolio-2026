using System.Reflection;
using NUnit.Framework;
using Pan.Util;
using UnityEngine;

namespace Pan.GridCompatibles2.Tests
{
    public class GridCompatible2Tests
    {
        [Test]
        public void NewInstance_HasUsableInternalSnapSetting()
        {
            var grid = new GridCompatible2();

            Assert.That(grid.CurrentSnapSetting, Is.Not.Null);
            Assert.That(grid.GridSnapTransformPosition(Vector3.zero, false), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void GridSnapTransformPosition_OddAndEvenSizesKeepSameSnapOffset()
        {
            var odd = CreateGridWithSize(3, 5);
            var even = CreateGridWithSize(4, 6);
            var input = new Vector3(2.2f, -1.4f, 0f);

            Assert.That(even.GridSnapTransformPosition(input, false),
                Is.EqualTo(odd.GridSnapTransformPosition(input, false)));
        }

        [TestCase(4, 4, ECenterStandard.LowerLeft, 8, 8)]
        [TestCase(5, 5, ECenterStandard.LowerLeft, 8, 8)]
        [TestCase(4, 4, ECenterStandard.UpperRight, 11, 11)]
        [TestCase(5, 5, ECenterStandard.UpperRight, 12, 12)]
        public void GetGridPositionStandard_PreservesEvenOddOffsets(
            int width,
            int height,
            ECenterStandard standard,
            int expectedX,
            int expectedY)
        {
            var grid = CreateGridWithSize(width, height);

            Assert.That(
                grid.GetGridPositionStandard(new Vector2Int(10, 10), standard),
                Is.EqualTo(new Vector2Int(expectedX, expectedY)));
        }

        [Test]
        public void ComponentsAndSettings_HaveUsableInternalGrid()
        {
            var gameObject = new GameObject("GridCompatible2Tests");
            var setting = ScriptableObject.CreateInstance<GridCompatible2SettingSbject>();

            try
            {
                Assert.That(gameObject.AddComponent<GridCompatible2Object>().GridCompatible, Is.Not.Null);
                Assert.That(setting.GridCompatible, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(setting);
                Object.DestroyImmediate(gameObject);
            }
        }

        [TestCase(GridLayout.CellSwizzle.XYZ)]
        [TestCase(GridLayout.CellSwizzle.XZY)]
        [TestCase(GridLayout.CellSwizzle.YXZ)]
        [TestCase(GridLayout.CellSwizzle.YZX)]
        [TestCase(GridLayout.CellSwizzle.ZXY)]
        [TestCase(GridLayout.CellSwizzle.ZYX)]
        public void GridPosition_RoundTripsAcrossSwizzles(GridLayout.CellSwizzle swizzle)
        {
            var grid = new GridCompatible2();
            SetPrivateField(grid.CurrentSnapSetting, "swizzle", swizzle);
            var expected = new Vector2Int(3, -2);

            Vector3 transformPosition =
                grid.Calculate_GridPosition_To_TransformPosition(expected, true);

            Assert.That(grid.GridPosition(transformPosition), Is.EqualTo(expected));
        }

        private static GridCompatible2 CreateGridWithSize(int width, int height)
        {
            var grid = new GridCompatible2();
            SetPrivateField(grid, "objectSizeX_Width", width);
            SetPrivateField(grid, "objectSizeY_Height", height);
            return grid;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
