using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pan.Util;



namespace Pan.StageGenerators.Tests
{
    public class DoorManagerOptimizationTests
    {
        [TestCase(true, 1, 3)]
        [TestCase(true, 2, 7)]
        [TestCase(true, 3, 7)]
        [TestCase(true, 99, 11)]
        [TestCase(false, 1, 11)]
        [TestCase(false, 2, 11)]
        [TestCase(false, 99, 11)]
        public void TryGetDoorTotalDistanceByCount_MatchesLegacySelection(
            bool isMin,
            int needCount,
            int expected)
        {
            var manager = CreateDistanceManager(11, 3, 7, 7);
            var doors = manager.GetDoorList(EDirection4.Down);
            var originalOrder = doors.ToArray();

            bool success = manager.TryGetDoorTotalDistanceByCount(
                EDirection4.Down,
                needCount,
                isMin,
                out int actual);

            Assert.That(success, Is.True);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(doors, Is.EqualTo(originalOrder), "조회 과정에서 직렬화된 도어 순서를 변경하면 안 됩니다.");
        }



        [Test]
        public void TryGetDoorTotalDistanceByCount_InvalidRequestPreservesFailureValue()
        {
            var manager = new RoomObject.DoorManager();

            Assert.That(
                manager.TryGetDoorTotalDistanceByCount(EDirection4.Down, 1, true, out int emptyResult),
                Is.False);
            Assert.That(emptyResult, Is.EqualTo(-1));

            manager.GetDoorList(EDirection4.Down).Add(CreateDoorWithTotalDistance(5));

            Assert.That(
                manager.TryGetDoorTotalDistanceByCount(EDirection4.Down, 0, true, out int zeroCountResult),
                Is.False);
            Assert.That(zeroCountResult, Is.EqualTo(-1));
        }



        [Test]
        public void TryGetDoorTotalDistanceByCount_QuickSelectMatchesReferenceAcrossPoolBoundary()
        {
            var values = new int[257];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = 2 + ((i * 37) % 31);
            }

            var manager = CreateDistanceManager(values);
            int[] ordered = values.OrderBy(value => value).ToArray();
            int[] counts = { 1, 2, 64, 128, 129, 200, values.Length, 999 };

            for (int i = 0; i < counts.Length; i++)
            {
                int take = System.Math.Min(counts[i], values.Length);

                Assert.That(
                    manager.TryGetDoorTotalDistanceByCount(
                        EDirection4.Down,
                        counts[i],
                        true,
                        out int actualMinBound),
                    Is.True);
                Assert.That(actualMinBound, Is.EqualTo(ordered[take - 1]));

                Assert.That(
                    manager.TryGetDoorTotalDistanceByCount(
                        EDirection4.Down,
                        counts[i],
                        false,
                        out int actualMaxBound),
                    Is.True);
                Assert.That(actualMaxBound, Is.EqualTo(ordered[ordered.Length - 1]));
            }
        }



        [Test]
        public void TryGetNeighborDoorGaps_MatchesStableLegacyOrderingWithoutMutatingSource()
        {
            var manager = new RoomObject.DoorManager();
            var doors = manager.GetDoorList(EDirection4.Down);
            doors.Add(CreateDoor(axis: 0, width: 1));
            doors.Add(CreateDoor(axis: 0, width: 4));
            doors.Add(CreateDoor(axis: 4, width: 1));
            doors.Add(CreateDoor(axis: 8, width: 3));
            var originalOrder = doors.ToArray();

            CalculateLegacyNeighborGaps(doors, out int expectedMin, out int expectedMax);

            bool success = manager.TryGetNeighborDoorGaps(
                EDirection4.Down,
                out int actualMin,
                out int actualMax);

            Assert.That(success, Is.True);
            Assert.That(actualMin, Is.EqualTo(expectedMin));
            Assert.That(actualMax, Is.EqualTo(expectedMax));
            Assert.That(doors, Is.EqualTo(originalOrder), "정렬용 구체화가 원본 도어 리스트 순서를 바꾸면 안 됩니다.");
        }



        [Test]
        public void TryGetNeighborDoorGaps_FewerThanTwoDoorsReturnsFailureValues()
        {
            var manager = new RoomObject.DoorManager();

            Assert.That(
                manager.TryGetNeighborDoorGaps(EDirection4.Down, out int emptyMin, out int emptyMax),
                Is.False);
            Assert.That((emptyMin, emptyMax), Is.EqualTo((-1, -1)));

            manager.GetDoorList(EDirection4.Down).Add(CreateDoor(axis: 0, width: 1));

            Assert.That(
                manager.TryGetNeighborDoorGaps(EDirection4.Down, out int singleMin, out int singleMax),
                Is.False);
            Assert.That((singleMin, singleMax), Is.EqualTo((-1, -1)));
        }



        private static RoomObject.DoorManager CreateDistanceManager(params int[] totalDistances)
        {
            var manager = new RoomObject.DoorManager();
            var doors = manager.GetDoorList(EDirection4.Down);

            for (int i = 0; i < totalDistances.Length; i++)
            {
                doors.Add(CreateDoorWithTotalDistance(totalDistances[i]));
            }

            return manager;
        }



        private static RoomObject.Door CreateDoorWithTotalDistance(int totalDistance)
        {
            const int DefaultDoorTotalLength = 2;

            return new RoomObject.Door
            {
                DoorWidth = 1,
                DoorPlusLength = 0,
                DoorGridPositionDistance = totalDistance - DefaultDoorTotalLength
            };
        }



        private static RoomObject.Door CreateDoor(int axis, int width)
        {
            return new RoomObject.Door
            {
                DoorGridPositionAxis = axis,
                DoorWidth = width
            };
        }



        private static void CalculateLegacyNeighborGaps(
            IReadOnlyList<RoomObject.Door> source,
            out int minGap,
            out int maxGap)
        {
            var ordered = source.OrderBy(door => door.DoorGridPositionAxis).ToList();
            GetSpan(ordered[0], out _, out int previousRight);
            minGap = int.MaxValue;
            maxGap = int.MinValue;

            for (int i = 1; i < ordered.Count; i++)
            {
                GetSpan(ordered[i], out int currentLeft, out int currentRight);
                int gap = currentLeft - previousRight - 1;

                if (gap < minGap) { minGap = gap; }
                if (gap > maxGap) { maxGap = gap; }

                previousRight = currentRight;
            }
        }



        private static void GetSpan(RoomObject.Door door, out int left, out int right)
        {
            if ((door.DoorWidth & 1) == 0)
            {
                int half = door.DoorWidth / 2;
                left = door.DoorGridPositionAxis - (half - 1);
                right = door.DoorGridPositionAxis + half;
                return;
            }

            int oddHalf = (door.DoorWidth - 1) / 2;
            left = door.DoorGridPositionAxis - oddHalf;
            right = door.DoorGridPositionAxis + oddHalf;
        }
    }
}
