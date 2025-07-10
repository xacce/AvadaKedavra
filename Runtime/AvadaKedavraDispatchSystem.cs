using System.Collections.Generic;
using System.Linq;
using AvadaKedavra2.Runtime;
#if AVADA_ENABLE_SCREEN_DEBUG
using System.Text;
#endif
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;

namespace AvadaKedavrav2
{
#if AVADA_DISABLE_AUTO_START
 [DisableAutoCreation]
#else
    [UpdateInGroup(typeof(PresentationSystemGroup))]
#endif
    public partial class AvadaKedavraDispatchSystem : SystemBase
    {
        private static readonly ProfilerMarker _profileLoadRoots = new ProfilerMarker("AvadaKedavraDispatchSystem.LoadRoots");
        private static readonly ProfilerMarker _profileUpdateEmittersJobs = new ProfilerMarker("AvadaKedavraDispatchSystem.UpdateEmittersJobs");
        private static readonly ProfilerMarker _profileUpdateNativeBuffers = new ProfilerMarker("AvadaKedavraDispatchSystem.UpdateNativeBuffers");
        private static readonly ProfilerMarker _profileUpdate = new ProfilerMarker("AvadaKedavraDispatchSystem.Update");
        private static readonly ProfilerMarker _profileSendEvents = new ProfilerMarker("AvadaKedavraDispatchSystem.SendEvents");

        // private ProfilerMarker _avad;
        private Dictionary<AvadaKedavraVfxId, AvadaKedavraBaseVfxController> _roots;
        private Dictionary<AvadaKedavraVfxId, AvadaKedavraBaseVfxController> _active;
        private Lookups _lookups;
        private Entity _registry;
#if AVADA_ENABLE_SCREEN_DEBUG
        private AvadaDebugMonobeh mbh;
#endif

        protected override void OnCreate()
        {
            base.OnCreate();
            _lookups = new Lookups(this);
            _roots = new Dictionary<AvadaKedavraVfxId, AvadaKedavraBaseVfxController>();
            _active = new Dictionary<AvadaKedavraVfxId, AvadaKedavraBaseVfxController>();
            _registry = this.EntityManager.CreateSingletonBuffer<AvadaKedavraRequest>();
#if AVADA_ENABLE_SCREEN_DEBUG
            var m = new GameObject("mbh");
            mbh = m.AddComponent<AvadaDebugMonobeh>();
            mbh.rs = new StringBuilder();
#endif
        }

        protected override void OnDestroy()
        {
#if AVADA_ENABLE_SCREEN_DEBUG
            if (mbh != null && mbh.gameObject != null)
                GameObject.Destroy(mbh.gameObject);
#endif
            foreach (var root in _roots)
            {
                root.Value.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            _lookups.Update(this);
            _profileLoadRoots.Begin();
            var requests = SystemAPI.GetBuffer<AvadaKedavraRequest>(_registry);
            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                AvadaKedavraBaseVfxController vfxController;
                if (!_active.TryGetValue(request.id, out vfxController))
                {
                    Debug.Log($"[Avada] Effect id: {request.id} currently not active, try to activate");
                    if (!_roots.TryGetValue(request.id, out vfxController))
                    {
                        Debug.Log($"[Avada] Effect id: {request.id} currently not loaded, try to load, effect was skipped");
                        var data = request.vfx.Value;
                        switch (data.avadaEffectType)
                        {
                            case AvadaEffectType.BufferedWithStrips:
                                vfxController = new AvadaKedavraBufferedWithStripsVfxController();
                                break;
                            case AvadaEffectType.Buffered:
                                vfxController = new AvadaKedavraBufferedVfxController();
                                break;
                            case AvadaEffectType.OneShootWithStrips:
                                vfxController = new AvadaKedavraOneShootStripsVfxController();
                                break;
                            default:
                            case AvadaEffectType.OneHoot:
                                vfxController = new AvadaKedavraOneShootVfxController();
                                break;
                        }

                        vfxController.DoLoad(request);
                        _roots[request.id] = vfxController;
                        continue;
                    }
                    else
                    {
                        if (!vfxController.isLoaded)
                        {
                            Debug.Log($"[Avada] Effect id: {request.id} currently loading. skip");
                            continue;
                        }

                        Debug.Log($"[Avada] Effect id: {request.id} loaded, added to pool");
                        _active.Add(request.id, vfxController);
                    }
                }

                vfxController.StoreRequest(request);
                requests.RemoveAt(i);
                i--;
            }

            _profileLoadRoots.End();

            var roots = _active.Values.ToList(); //todo need?

            _profileUpdateEmittersJobs.Begin();
            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(roots.Count, Allocator.Temp);

            int ii = 0;
            var time = SystemAPI.Time;
            foreach (var root in roots)
            {
                handles[ii] = root.JobUpdateEmitters(time);
                ii++;
            }

            JobHandle.CombineDependencies(handles).Complete();
            _profileUpdateEmittersJobs.End();

            _profileUpdateNativeBuffers.Begin();
            ii = 0;
            foreach (var root in roots)
            {
                if (!root.HasState()) continue;
                handles[ii] = root.PreUpdate(Dependency, time, _lookups);
                ii++;
            }


            Dependency = JobHandle.CombineDependencies(handles);
            Dependency.Complete();
            _profileUpdateNativeBuffers.End();

            _profileUpdate.Begin();
            foreach (var root in roots)
            {
                if (!root.HasState()) continue;
                root.PostUpdate();
            }

            _profileUpdate.End();
            _profileSendEvents.Begin();
            foreach (var root in roots)
            {
                root.SendEvents();
                if (root.CanDeactivated())
                {
                    _active.Remove(root.id);
                }
            }

            _profileSendEvents.End();
#if AVADA_ENABLE_SCREEN_DEBUG
            mbh.rs.Clear();
            mbh.rs.AppendLine($"[Avada] Activated effects: {roots.Count}");
            mbh.rs.AppendLine($"[Avada] Unhandled requests: {requests.Length}");
            foreach (var root in roots)
            {
                root.DrawDebug(mbh.rs);
            }
#endif
        }

        public struct Lookups
        {
            [ReadOnly] public EntityStorageInfoLookup entityStorageInfoLookup;
            [ReadOnly] public ComponentLookup<AvadaKedavraData> avadaRo;
            [ReadOnly] public ComponentLookup<LocalToWorld> ltwRo;

            public Lookups(SystemBase state) : this()
            {
                entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
                avadaRo = state.GetComponentLookup<AvadaKedavraData>(true);
                ltwRo = state.GetComponentLookup<LocalToWorld>(true);
            }

            public void Update(SystemBase state)
            {
                entityStorageInfoLookup.Update(state);
                avadaRo.Update(state);
                ltwRo.Update(state);
            }
        }
    }
}