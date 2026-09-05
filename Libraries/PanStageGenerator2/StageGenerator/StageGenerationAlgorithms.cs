using System;
using System.Collections.Generic;



namespace Pan.StageGenerators
{
    internal static class StageGenerationAlgorithms
    {
        internal static void CollectMaxCandidates<T>(
            IReadOnlyList<T> source,
            List<T> destination,
            Func<T, int> keySelector)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
            if (keySelector == null) { throw new ArgumentNullException(nameof(keySelector)); }

            destination.Clear();

            int maxKey = int.MinValue;

            for (int i = 0; i < source.Count; i++)
            {
                T candidate = source[i];
                int key = keySelector(candidate);
                if (destination.Count == 0 || key > maxKey)
                {
                    maxKey = key;
                    destination.Clear();
                    destination.Add(candidate);
                }
                else if (key == maxKey)
                {
                    destination.Add(candidate);
                }
            }

        }
    }
}
