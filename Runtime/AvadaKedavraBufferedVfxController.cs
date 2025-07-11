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
    public class AvadaKedavraBufferedVfxController : AvadaKedavraBaseVfxController
    {
        private GraphicsBuffer _buffer;
        private NativeQueue<AvadaKedavraRequest> _growBuffer;
        private NativeQueue<int> _pool;

        private NativeList<AvadaAliveBuffered> _alive;

        public override void DoLoad(AvadaKedavraV2EffectSo request)
        {
            if (isLoaded || isLoading) return;
            Load(request);
            var emitters = _rootManaged.emitters;

            _pool = new NativeQueue<int>(Allocator.Persistent);
            int i = 0;

            foreach (var emmiter in emitters)
            {
                if (emmiter.stripData.stripped)
                {
                    Debug.Log($"[Avada] {id} contains strips but type not supported strips, select other");
                    continue;
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
            _alive = new NativeList<AvadaAliveBuffered>(Allocator.Persistent);
            _growBuffer = new NativeQueue<AvadaKedavraRequest>(Allocator.Persistent);
        }


        public override JobHandle JobUpdateEmitters(TimeData time)
        {
            return new UpdateEmittersBufferedJob()
            {
                cuttedRequests = _growBuffer,
                requests = _requests,
                plannedEmitters = _plannedEmitters,
                dropppedEmitters = _dropppedEmitters,
                aliveEffects = _alive,
                idPool = _pool,
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
            return new UpdateBufferJob()
            {
                aliveEffects = _alive,
                idPool = _pool,
                rwData = data,
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
                _alive.Dispose();
            }
           
        }
    }
}