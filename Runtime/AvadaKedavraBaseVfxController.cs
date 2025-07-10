using System;
using System.Text;
using AvadaKedavrav2;
using AvadaKedavrav2.So;
using Unity.Collections;
using Unity.Core;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.VFX;

namespace AvadaKedavra2.Runtime
{
    public abstract class AvadaKedavraBaseVfxController : IEquatable<AvadaKedavraBaseVfxController>, IDisposable
    {
        protected VisualEffect _effect;
        public bool isLoaded;
        public bool isLoading;
        public AvadaKedavraVfxId id;
        protected AvadaKedavraV2EffectSo _rootManaged;
        protected static readonly int _propFrom = Shader.PropertyToID("f1");
        protected static readonly int _propTo = Shader.PropertyToID("f2");
        protected static readonly int _propDirection = Shader.PropertyToID("f3");
        protected static readonly int _propScale = Shader.PropertyToID("f4");
        protected static readonly int _propExtra = Shader.PropertyToID("f5");
        protected static readonly int _propCount = Shader.PropertyToID("spawnCount");
        protected static readonly int _propIdInt = Shader.PropertyToID("runtimeId");
        protected static readonly int _propStripIndexUint = Shader.PropertyToID("stripIndexCustom");
        protected NativeArray<AvadaKedavraEmitter> _emitters;
        protected NativeQueue<AvadaKedavraRequest> _requests;
        protected AvadaKedavraRoot _rootUnmanaged;
        protected NativeList<RuntimeEmitter> _plannedEmitters;
        protected NativeList<RuntimeEmitter> _dropppedEmitters;

        public void StoreRequest(AvadaKedavraRequest request)
        {
            _requests.Enqueue(request);
        }

        public void Load(AvadaKedavraRequest request)
        {
            isLoaded = true;

            _rootManaged = request.vfx.Value;
            _rootUnmanaged = _rootManaged.AsUnmanaged();
            // var root = asset.vfx;
            var spawned = new GameObject($"AvadaKedavra");
            var ve = spawned.AddComponent<VisualEffect>();
            id = _rootManaged.id;

            ve.visualEffectAsset = _rootManaged.vfx;
            _effect = ve;
            _requests = new NativeQueue<AvadaKedavraRequest>(Allocator.Persistent);


            _emitters = new NativeArray<AvadaKedavraEmitter>(_rootManaged.emitters.Length, Allocator.Persistent);
            var emitters = _rootManaged.emitters;
            for (int i = 0; i < emitters.Length; i++)
            {
                //Unity shader prop id is not a hash code or whatever, its a sequenced N of all shader props. And in build  can be changed 
                var managed = emitters[i];
                _emitters[i] = new AvadaKedavraEmitter()
                {
                    delay = managed.delay,
                    particles = managed.particles,
                    eventId = Shader.PropertyToID(managed.eventId),
                    stripData = managed.stripData,
                };
            }

            _plannedEmitters = new NativeList<RuntimeEmitter>(Allocator.Persistent);
            _dropppedEmitters = new NativeList<RuntimeEmitter>(Allocator.Persistent);
        }

        public abstract bool CanDeactivated();
        public abstract JobHandle JobUpdateEmitters(TimeData time);
        public abstract JobHandle PreUpdate(JobHandle dependency, TimeData time, AvadaKedavraDispatchSystem.Lookups lookups);
        public abstract void PostUpdate();

        public void SendEvents()
        {
            for (int i = 0; i < _dropppedEmitters.Length; i++)
            {
                var dropped = _dropppedEmitters[i];
                var evt = _effect.CreateVFXEventAttribute();
                if (dropped.stripIndex != -1)
                {
                    evt.SetUint(_propStripIndexUint, (uint)dropped.stripIndex);
                }

                evt.SetFloat(_propCount, dropped.particles);
                evt.SetVector3(_propFrom, dropped.request.from);
                evt.SetVector3(_propTo, dropped.request.to);
                evt.SetVector3(_propDirection, dropped.request.direction);
                evt.SetVector3(_propScale, dropped.request.scale);
                evt.SetVector3(_propExtra, dropped.request.extra);
                if (dropped.bufferIndex > -1)
                {
                    evt.SetInt(_propIdInt, dropped.bufferIndex);
                }

                _effect.SendEvent(dropped.eventId, evt);
            }

            _dropppedEmitters.Clear();
        }

        public abstract bool HasState();
        public abstract void DrawDebug(StringBuilder str);
        public abstract void DoLoad(AvadaKedavraRequest request);

        public bool Equals(AvadaKedavraBaseVfxController other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return id.Equals(other.id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AvadaKedavraBaseVfxController)obj);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public virtual void Dispose()
        {
            _requests.Dispose();
            _emitters.Dispose();
            _plannedEmitters.Dispose();
            _dropppedEmitters.Dispose();
        }
    }
}