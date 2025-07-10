using System.Text;
using AvadaKedavra2.Runtime;
using DotsCore.Keke;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Jobs;
using UnityEngine;

namespace AvadaKedavrav2
{
    public class AvadaKedavraOneShootStripsVfxController : AvadaKedavraBaseVfxController
    {
        private NativeArray<UnsafeQueue<int>> _stripsPool;

        private NativeList<AvadaAliveOneShootStrips> _alive;

        public override void DoLoad(AvadaKedavraRequest request)
        {
            if (isLoaded || isLoading) return;
            Load(request);
            var emitters = _rootManaged.emitters;

            _stripsPool = new NativeArray<UnsafeQueue<int>>(emitters.Length, Allocator.Persistent);
            int i = 0;

            foreach (var emmiter in emitters)
            {
                if (emmiter.stripData.stripped)
                {
                    _stripsPool[i] = new UnsafeQueue<int>(Allocator.Persistent);
                    for (int j = 0; j < emmiter.stripData.stripsMaxCount; j++)
                    {
                        _stripsPool[i].Enqueue(j);
                    }
                }

                i++;
            }
            _alive = new NativeList<AvadaAliveOneShootStrips>(Allocator.Persistent);
        }


        public override JobHandle JobUpdateEmitters(TimeData time)
        {
            return new UpdateEmittersOneShootWithStripsJob()
            {
                requests = _requests,
                plannedEmitters = _plannedEmitters,
                dropppedEmitters = _dropppedEmitters,
                aliveEffects = _alive,
                stripsPool = _stripsPool,
                emitters = _emitters,
                elapsed = time.ElapsedTime,
                root = _rootUnmanaged,
            }.Schedule();
        }

        public override JobHandle PreUpdate(JobHandle dependency, TimeData time, AvadaKedavraDispatchSystem.Lookups lookups)
        {
            return new UpdateStripsJob()
            {
                aliveEffects = _alive,
                stripsPool = _stripsPool,
                elapsed = time.ElapsedTime,
            }.Schedule(dependency);
        }


        public override void PostUpdate()
        {
        }


        public override bool HasState()
        {
            return true;
        }

        public override void DrawDebug(StringBuilder str)
        {
            str.AppendLine($"[{_rootManaged.id.id}] Queued emitters: {_plannedEmitters.Length}");
            str.AppendLine($"[{_rootManaged.id.id}] Alive CPU/GPU: {_alive.Length}/{_effect.aliveParticleCount}");
            str.Append($"\n[{_rootManaged.id.id}] Strips pools size: ");
            for (int i = 0; i < _stripsPool.Length; i++)
            {
                str.Append($"{_stripsPool[i].Count},");
            }
        }

        public override bool CanDeactivated()
        {
            if (_alive.IsEmpty && _plannedEmitters.IsEmpty && _requests.IsEmpty()) return true;
            return false;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_alive.IsCreated)
            {
                for (int i = 0; i < _alive.Length; i++)
                {
                    if (_alive[i].reservedStrips.IsCreated)
                    {
                        _alive[i].reservedStrips.Dispose();
                    }
                }

                _alive.Dispose();
            }

            if (_stripsPool.IsCreated)
            {
                for (int i = 0; i < _stripsPool.Length; i++)
                {
                    if (_stripsPool[i].IsCreated)
                    {
                        _stripsPool[i].Dispose();
                    }
                }

                _stripsPool.Dispose();
            }
        }
    }
}