using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AvadaKedavrav2
{
    [BurstCompile]
    internal partial struct UpdateStripsJob : IJob
    {
        public NativeList<AvadaAliveOneShootStrips> aliveEffects;
        public NativeArray<UnsafeQueue<int>> stripsPool;
        public double elapsed;

        [BurstCompile]
        public void Execute()
        {
            #region Update

            for (int i = aliveEffects.Length - 1; i >= 0; i--)
            {
                var element = aliveEffects[i];
                bool invalidate = element.invalidateAt <= elapsed;


                if (invalidate)
                {
                    aliveEffects.RemoveAt(i);

                    if (element.reservedStrips.IsCreated)
                    {
                        for (int j = 0; j < element.reservedStrips.Length; j++)
                        {
                            var reserved = element.reservedStrips[j];
                            stripsPool[reserved.emitterIndex].Enqueue(reserved.stripIndex);
                        }

                        element.reservedStrips.Dispose();
                    }
                }

            }

            #endregion
        }
    }
}