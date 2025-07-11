using System.Text;
using AvadaKedavra2.Runtime;
using AvadaKedavrav2.So;
using DotsCore.Keke;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Jobs;
using UnityEngine;

namespace AvadaKedavrav2
{
    public class AvadaKedavraBufferedWithStripsVfxController : AvadaKedavraBaseVfxController
    {
        private GraphicsBuffer _buffer;
        private NativeQueue<AvadaKedavraRequest> _growBuffer;
        private NativeQueue<int> _pool;
        private NativeArray<UnsafeQueue<int>> _stripsPool;

        private NativeList<AvadaAliveBufferedStrips> _alive;

        public override void DoLoad(AvadaKedavraV2EffectSo request)
        {
            if (isLoaded || isLoading) return;
            Load(request);
            var emitters = _rootManaged.emitters;

            _pool = new NativeQueue<int>(Allocator.Persistent);
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

            _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite, _rootManaged.initialBufferCapacity,
                UnsafeUtility.SizeOf<AvadaKedavdaElement>());
            for (i = 0; i < _rootManaged.initialBufferCapacity; i++)
            {
                _pool.Enqueue(i);
            }

            _effect.SetGraphicsBuffer("avadaKedavra", _buffer);
            _alive = new NativeList<AvadaAliveBufferedStrips>(Allocator.Persistent);
            _growBuffer = new NativeQueue<AvadaKedavraRequest>(Allocator.Persistent);
        }


        public override JobHandle JobUpdateEmitters(TimeData time)
        {
            return new UpdateEmittersBufferedWithStripsJob()
            {
                cuttedRequests = _growBuffer,
                requests = _requests,
                plannedEmitters = _plannedEmitters,
                dropppedEmitters = _dropppedEmitters,
                aliveEffects = _alive,
                idPool = _pool,
                stripsPool = _stripsPool,
                emitters = _emitters,
                elapsed = time.ElapsedTime,
                root = _rootUnmanaged,
                bufferLength = _buffer.count,
            }.Schedule();
        }

        public override JobHandle PreUpdate(JobHandle dependency, TimeData time, AvadaKedavraDispatchSystem.Lookups lookups)
        {
            var growCount = _growBuffer.Count;
            if (_growBuffer.Count > 0)
            {
                var curSize = _buffer.count;
                var size = curSize + growCount * _rootUnmanaged.bufferGrowRate;
                Debug.Log($"[Avada] Growing buffer by {growCount} for {id}, current: {curSize}, new: {size},");
                _buffer.Release();
                _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite, size, UnsafeUtility.SizeOf<AvadaKedavdaElement>());
                for (int i = curSize; i < size; i++)
                {
                    _pool.Enqueue(i);
                }

                _effect.SetGraphicsBuffer("avadaKedavra", _buffer);
                while (_growBuffer.TryDequeue(out var request))
                {
                    _requests.Enqueue(request);
                }
            }

            var data = _buffer.LockBufferForWrite<AvadaKedavdaElement>(0, _buffer.count);
            return new UpdateBufferWithStripsJob()
            {
                aliveEffects = _alive,
                idPool = _pool,
                rwData = data,
                stripsPool = _stripsPool,
                elapsed = time.ElapsedTime,
                root = _rootUnmanaged,
                ltwRo = lookups.ltwRo,
                avadaRo = lookups.avadaRo,
                entityStorageInfoLookup = lookups.entityStorageInfoLookup,
                transformRo = lookups.transformRo,
            }.Schedule(dependency);
        }


        public override void PostUpdate()
        {
            _buffer.UnlockBufferAfterWrite<AvadaKedavdaElement>(_buffer.count);
        }


        public override bool HasState()
        {
            return true;
        }

        public override void DrawDebug(StringBuilder str)
        {
            str.AppendLine($"[{_rootManaged.avadaId.id}] Queued emitters: {_plannedEmitters.Length}");
            str.AppendLine($"[{_rootManaged.avadaId.id}] Alive CPU/GPU: {_alive.Length}/{_effect.aliveParticleCount}");
            str.AppendLine($"[{_rootManaged.avadaId.id}] Pool size: {_pool.Count}");
            str.AppendLine($"[{_rootManaged.avadaId.id}] Grow length: {_growBuffer.Count}");

            str.Append($"\n[{_rootManaged.avadaId.id}] Strips pools size: ");
            for (int i = 0; i < _stripsPool.Length; i++)
            {
                str.Append($"{_stripsPool[i].Count},");
            }
        }

        public override bool CanDeactivated()
        {
            if (_alive.IsEmpty && _plannedEmitters.IsEmpty) return true;
            return false;
        }

        public override void Dispose()
        {
            base.Dispose();
            _buffer?.Dispose();
            _buffer?.Release();
            _pool.Dispose();
            _growBuffer.Dispose();

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