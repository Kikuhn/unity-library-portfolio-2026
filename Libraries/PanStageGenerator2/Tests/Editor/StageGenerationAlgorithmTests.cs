using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pan.Util;
using UnityEngine;



namespace Pan.StageGenerators.Tests
{
    public class StageGenerationAlgorithmTests
    {
        [Test]
        public void CollectMaxCandidates_ScansOnceAndPreservesInputOrder()
        {
            var source = new List<Candidate>
            {
                new Candidate(0, 3),
                new Candidate(1, 7),
                new Candidate(2, 5),
                new Candidate(3, 7),
                new Candidate(4, 4)
            };
            var destination = new List<Candidate>();
            int keyReadCount = 0;

            StageGenerationAlgorithms.CollectMaxCandidates(
                source,
                destination,
                candidate =>
                {
                    keyReadCount++;
                    return candidate.Score;
                });

            Assert.That(keyReadCount, Is.EqualTo(source.Count));
            Assert.That(destination.Select(x => x.Id), Is.EqualTo(new[] { 1, 3 }));
        }



        [Test]
        public void CollectMaxCandidates_EmptyInputClearsDestination()
        {
            var destination = new List<Candidate> { new Candidate(99, 99) };

            StageGenerationAlgorithms.CollectMaxCandidates(
                Array.Empty<Candidate>(),
                destination,
                candidate => candidate.Score);

            Assert.That(destination, Is.Empty);
        }



        [Test]
        public void CollectMaxCandidates_FixedSeedMatchesLegacyAndConsumesOneRandomDraw()
        {
            var source = new List<Candidate>
            {
                new Candidate(0, 3),
                new Candidate(1, 7),
                new Candidate(2, 5),
                new Candidate(3, 7),
                new Candidate(4, 7)
            };
            var optimizedCandidates = new List<Candidate>();
            var legacyRandom = new CustomRandom(13579);
            var optimizedRandom = new CustomRandom(13579);

            int maxScore = source.Max(x => x.Score);
            var legacyCandidates = source.Where(x => x.Score == maxScore);
            Candidate legacySelected = legacyCandidates
                .Skip(legacyRandom.Range(0, legacyCandidates.Count()))
                .FirstOrDefault();

            StageGenerationAlgorithms.CollectMaxCandidates(
                source,
                optimizedCandidates,
                x => x.Score);
            Candidate optimizedSelected = optimizedCandidates[
                optimizedRandom.Range(0, optimizedCandidates.Count)];

            Assert.That(optimizedSelected.Id, Is.EqualTo(legacySelected.Id));
            Assert.That(
                optimizedRandom.Range(0, int.MaxValue),
                Is.EqualTo(legacyRandom.Range(0, int.MaxValue)));
        }



        [Test]
        public void IndexedPriorityQueue_LookupDoesNotScanQueuedItems()
        {
            var queue = new StageGenerator.PlaceManger.Generator_ASharpPathFinder.Pathfinder.IndexedPriorityQueue<EqualityProbe>();
            var probes = new List<EqualityProbe>(4096);

            for (int i = 0; i < probes.Capacity; i++)
            {
                var probe = new EqualityProbe(i);
                probes.Add(probe);
                Assert.That(queue.TryEnqueue(probe, i), Is.True);
            }

            EqualityProbe.EqualsCallCount = 0;

            Assert.That(queue.Contains(probes[probes.Count - 1]), Is.True);
            Assert.That(EqualityProbe.EqualsCallCount, Is.LessThanOrEqualTo(1));
        }



        [Test]
        public void IndexedPriorityQueue_UpdatePriorityPreservesPriorityThenInsertionOrder()
        {
            var queue = new StageGenerator.PlaceManger.Generator_ASharpPathFinder.Pathfinder.IndexedPriorityQueue<int>();

            Assert.That(queue.TryEnqueue(1, 10), Is.True);
            Assert.That(queue.TryEnqueue(2, 10), Is.True);
            Assert.That(queue.TryEnqueue(3, 20), Is.True);
            queue.UpdatePriority(3, 10);

            Assert.That(queue.Dequeue(), Is.EqualTo(1));
            Assert.That(queue.Dequeue(), Is.EqualTo(2));
            Assert.That(queue.Dequeue(), Is.EqualTo(3));
        }



        [Test]
        public void IndexedPriorityQueue_DuplicateInsertIsRejectedWithoutChangingState()
        {
            var queue = new StageGenerator.PlaceManger.Generator_ASharpPathFinder.Pathfinder.IndexedPriorityQueue<int>();

            Assert.That(queue.TryEnqueue(10, 20), Is.True);
            Assert.That(queue.TryEnqueue(10, 1), Is.False);
            Assert.That(queue.Count, Is.EqualTo(1));
            Assert.That(queue.Dequeue(), Is.EqualTo(10));
            Assert.That(queue.Count, Is.Zero);
        }



        [Test]
        public void IndexedPriorityQueue_ClearRemovesIndexAndResetsInsertionOrder()
        {
            var queue = new StageGenerator.PlaceManger.Generator_ASharpPathFinder.Pathfinder.IndexedPriorityQueue<int>();

            queue.TryEnqueue(1, 5);
            queue.TryEnqueue(2, 5);
            queue.Clear();

            Assert.That(queue.Count, Is.Zero);
            Assert.That(queue.Contains(1), Is.False);
            Assert.That(queue.TryEnqueue(2, 5), Is.True);
            Assert.That(queue.TryEnqueue(1, 5), Is.True);
            Assert.That(queue.Dequeue(), Is.EqualTo(2));
            Assert.That(queue.Dequeue(), Is.EqualTo(1));
        }



        [Test]
        public void AStar_EqualCostGridPreservesDeterministicTiePath()
        {
            var root = new GameObject("AStarContract");

            try
            {
                var generator = root.AddComponent<StageGenerator>();
                generator.Refresh_StageGenerator(true);
                Assert.That(generator.GridM.Generate_GridArray(), Is.True);

                for (int x = 0; x < 5; x++)
                {
                    for (int y = 0; y < 5; y++)
                    {
                        generator.GridM.GetGrid(x, y).PathCost = 0;
                    }
                }

                using var pathfinder = new StageGenerator.PlaceManger.Generator_ASharpPathFinder.Pathfinder();
                var path = new List<Vector2Int>();

                bool found = pathfinder.PathFinding(
                    generator.GridM,
                    path,
                    new Vector2Int(0, 0),
                    new Vector2Int(4, 4),
                    StageGenerator.PlaceManger.Generator_ASharpPathFinder.HeuristicType.Manhattan,
                    bottomLeft: new Vector2Int(0, 0),
                    topRight: new Vector2Int(4, 4));

                Assert.That(found, Is.True);
                Assert.That(path, Is.EqualTo(new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(3, 0),
                    new Vector2Int(4, 0),
                    new Vector2Int(4, 1),
                    new Vector2Int(4, 2),
                    new Vector2Int(4, 3),
                    new Vector2Int(4, 4)
                }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }



        [Test]
        public void DoorSpineConnector_OrderDoorPointsUsesAxisAndPreservesTieOrder()
        {
            var source = new[]
            {
                DoorPoint(new Vector2(4f, 3f), 0),
                DoorPoint(new Vector2(2f, 1f), 1),
                DoorPoint(new Vector2(1f, 1f), 2),
                DoorPoint(new Vector2(3f, 2f), 3)
            };

            var vertical = StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.OrderDoorPoints(
                StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.SplitAxis.Vertical,
                source);
            var horizontal = StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.OrderDoorPoints(
                StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.SplitAxis.Horizontal,
                source);

            Assert.That(vertical.Select(x => (int)x.DoorRef), Is.EqualTo(new[] { 1, 2, 3, 0 }));
            Assert.That(horizontal.Select(x => (int)x.DoorRef), Is.EqualTo(new[] { 2, 1, 3, 0 }));
        }



        [Test]
        public void DoorSpineConnector_SelectFirstDoorByProjectionPreservesFirstTie()
        {
            var root = new GameObject("DoorProjectionContract");

            try
            {
                var room = root.AddComponent<RoomObject>();
                room.RoomDoorM.WakeUp(room);
                var first = CreateInitializedDoor(room, EDirection4.Right, axis: 3);
                var expected = CreateInitializedDoor(room, EDirection4.Right, axis: 1);
                var tied = CreateInitializedDoor(room, EDirection4.Right, axis: 1);

                RoomObject.Door selected =
                    StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.SelectFirstDoorByProjection(
                        StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.SplitAxis.Vertical,
                        new[] { first, expected, tied });

                Assert.That(selected, Is.SameAs(expected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }



        [Test]
        public void DoorSpineConnector_OrderedPathUsesProvidedOrderAndSkipsZeroLengthStubs()
        {
            var callbacks = new List<string>();
            var stubStarts = new List<Vector2>();
            var stubEnds = new List<Vector2>();
            var orderedA = new[]
            {
                DoorPoint(new Vector2(0f, 3f), 0),
                DoorPoint(new Vector2(0f, 1f), 1),
                DoorPoint(new Vector2(5f, 9f), 2)
            };

            StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.ConnectViaSpineOrdered(
                StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.SplitAxis.Vertical,
                5f,
                0f,
                10f,
                orderedA,
                null,
                1f,
                (start, end) => callbacks.Add($"main:{start}:{end}"),
                (start, end) =>
                {
                    callbacks.Add("stub");
                    stubStarts.Add(start);
                    stubEnds.Add(end);
                });

            Assert.That(callbacks[0], Does.StartWith("main:"));
            Assert.That(callbacks.Skip(1), Is.EqualTo(new[] { "stub", "stub" }));
            Assert.That(stubStarts[0], Is.EqualTo(new Vector2(5f, 3f)));
            Assert.That(stubEnds[0], Is.EqualTo(new Vector2(0f, 3f)));
            Assert.That(stubStarts[1].x, Is.EqualTo(5f));
            Assert.That(stubStarts[1].y, Is.EqualTo(1.001f).Within(0.00001f));
            Assert.That(stubEnds[1], Is.EqualTo(new Vector2(0f, stubStarts[1].y)));
        }



        private static StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint DoorPoint(
            Vector2 position,
            int id)
        {
            return new StageGenerator.PlaceManger.Generator2_LinkRooms.DoorSpineConnector.DoorPoint(position, id);
        }



        private static RoomObject.Door CreateInitializedDoor(
            RoomObject room,
            EDirection4 direction,
            int axis)
        {
            var door = new RoomObject.Door { DoorGridPositionAxis = axis };
            door.InitializeRoomDoor(room, direction);
            return door;
        }



        private readonly struct Candidate
        {
            public readonly int Id;
            public readonly int Score;

            public Candidate(int id, int score)
            {
                Id = id;
                Score = score;
            }
        }



        private sealed class EqualityProbe : IEquatable<EqualityProbe>
        {
            public static int EqualsCallCount;
            private readonly int id;

            public EqualityProbe(int id)
            {
                this.id = id;
            }

            public bool Equals(EqualityProbe other)
            {
                EqualsCallCount++;
                return other != null && id == other.id;
            }

            public override bool Equals(object obj)
            {
                return obj is EqualityProbe other && Equals(other);
            }

            public override int GetHashCode()
            {
                return id;
            }
        }
    }
}
