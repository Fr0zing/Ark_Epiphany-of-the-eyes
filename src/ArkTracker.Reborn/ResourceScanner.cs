using System;
using System.Collections.Generic;
using System.Linq;

namespace ArkTracker
{
    internal static class ResourceCatalog
    {
        internal static readonly Dictionary<string, string> Common = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Silicon", "Жемчуг" }, { "BlackPearl", "Чёрный жемчуг" },
            { "Metal", "Металл" }, { "Stone", "Камень" }, { "Flint", "Кремень" },
            { "Crystal", "Кристалл" }, { "Obsidian", "Обсидиан" }, { "Oil", "Нефть" },
            { "Wood", "Дерево" }, { "Thatch", "Солома" }, { "Fiber", "Волокно" },
            { "Sulfur", "Сера" }, { "Salt", "Соль" }, { "Sand", "Песок" },
            { "Mutagel", "Мутагель" }, { "Ambergris", "Амбра" }, { "ElementShard", "Осколки элемента" }
        };
        internal static string Id(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            const string prefix = "PrimalItemResource_";
            int pos = name.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (pos >= 0) name = name.Substring(pos + prefix.Length);
            if (name.EndsWith("_C", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 2);
            return name;
        }
        internal static string Label(string id)
        {
            string label;
            return Common.TryGetValue(id, out label) ? label : id;
        }
    }

    internal sealed partial class TrackerEngine
    {
        // Layouts from the local ShooterGame.pdb matching the verified ASE build.
        // Never interpret a foliage controller's origin as a resource position.
        private readonly HashSet<ulong> foliageActors = new HashSet<ulong>();
        private readonly Dictionary<ulong, ResourceComponent> resourceComponents = new Dictionary<ulong, ResourceComponent>();
        private readonly Dictionary<ulong, string[]> harvestResources = new Dictionary<ulong, string[]>();
        private readonly Dictionary<string, string> resourceCatalog = new Dictionary<string, string>(ResourceCatalog.Common, StringComparer.OrdinalIgnoreCase);
        private volatile ResourceOptions resourceOptions = new ResourceOptions();
        private ulong resourceWorld;
        private DateTime nextResourceDiscovery;
        private DateTime nextResourceLog;
        private int resourceOwnersRead, resourcePointersSeen, resourceInstanceComponents, resourceAttachedClasses, resourceHarvestClasses, resourceHarvestDefinitions;
        private string resourceDiscoverySample = string.Empty;
        private Queue<ulong> foliageDiscovery = new Queue<ulong>();
        private int componentCursor;
        private readonly Dictionary<ulong, int> foliageDiscoveryOffsets = new Dictionary<ulong, int>();
        private ResourceOptions lastResourceOptions;
        private sealed class ResourceOptions
        {
            internal bool Enabled;
            internal float Distance = 100000f;
            internal HashSet<string> Selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        private sealed class ResourceComponent
        {
            internal ulong Owner, Address, Mesh, Data;
            internal int Count, Cursor;
            internal string[] Resources;
            internal DateTime NextScan;
            internal List<ActorRecord> Points = new List<ActorRecord>();
            internal List<ActorRecord> Pending = new List<ActorRecord>();
            internal HashSet<int> Removed = new HashSet<int>();
        }
        internal void SetResourceOptions(TrackerViewSettings settings)
        {
            var current = resourceOptions;
            if (current.Enabled == settings.ShowResources && current.Distance == settings.ResourceMaxDistanceCm && current.Selected.SetEquals(settings.SelectedResources)) return;
            resourceOptions = new ResourceOptions { Enabled = settings.ShowResources,
                Distance = settings.ResourceMaxDistanceCm,
                Selected = new HashSet<string>(settings.SelectedResources, StringComparer.OrdinalIgnoreCase) };
        }
        internal string[] GetResourceCatalog()
        {
            lock (resourceCatalog) return resourceCatalog.OrderBy(p => p.Value).Select(p => p.Value + "\t" + p.Key).ToArray();
        }
        private bool ResourceArray(ulong address, int max, out ulong data, out int count)
        {
            data = 0; count = 0; int capacity;
            return reader.TryReadUInt64(address, out data) && reader.TryReadInt32(address + 8, out count) &&
                reader.TryReadInt32(address + 12, out capacity) && count >= 0 && count <= max && capacity >= count && capacity <= max &&
                (count == 0 || AddressGuard.IsPointerValid(data));
        }
        private bool ResourceBytes(ulong address, int length, out byte[] bytes)
        {
            int read;
            return reader.TryReadBytes(address, length, out bytes, out read) && read == length;
        }
        private bool ResourceSubclass(ulong cls, string target)
        {
            for (int depth = 0; depth < 32 && AddressGuard.IsPointerValid(cls); depth++)
            {
                string name;
                if (names.TryReadObjectName(cls, out name) && string.Equals(name, target, StringComparison.OrdinalIgnoreCase)) return true;
                if (!reader.TryReadPointer(cls + config.UStructSuperStruct.Value, out cls)) break;
            }
            return false;
        }
        private string[] ReadHarvestResources(ulong component)
        {
            ulong cls;
            if (!reader.TryReadPointer(component + 0x760, out cls) || !AddressGuard.IsPointerValid(cls)) return new string[0];
            resourceAttachedClasses++;
            string[] cached;
            if (harvestResources.TryGetValue(cls, out cached)) return cached;
            if (!ResourceSubclass(cls, "PrimalHarvestingComponent")) return new string[0];
            resourceHarvestClasses++;
            ulong cdo;
            if (!reader.TryReadPointer(cls + 0xF8, out cdo) || !AddressGuard.IsPointerValid(cdo)) return new string[0];
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ulong offset in new ulong[] { 0xE8, 0xF8 })
            {
                ulong data; int count; byte[] entries;
                if (!ResourceArray(cdo + offset, 256, out data, out count) || count == 0 || !ResourceBytes(data, count * 0x78, out entries)) continue;
                for (int i = 0; i < count; i++)
                {
                    ulong itemClass = BitConverter.ToUInt64(entries, i * 0x78 + 0x18);
                    string name;
                    if (!AddressGuard.IsPointerValid(itemClass) || !names.TryReadObjectName(itemClass, out name)) continue;
                    string id = ResourceCatalog.Id(name);
                    if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || id.Contains("|")) continue;
                    found.Add(id);
                    lock (resourceCatalog) resourceCatalog[id] = ResourceCatalog.Label(id);
                }
            }
            cached = found.ToArray();
            // Retry empty/unloaded definitions on the next discovery cycle.
            if (cached.Length > 0) { harvestResources[cls] = cached; resourceHarvestDefinitions++; }
            return cached;
        }
        private ulong[] ReadFoliageComponents(ulong owner)
        {
            // InstancedStaticMeshComponent is editor-facing and may be empty in a
            // cooked client. OwnedComponents/SerializedComponents remain populated.
            var result = new HashSet<ulong>();
            foreach (ulong offset in new ulong[] { 0x488, 0x438, 0x448 })
            {
                ulong data; int count; byte[] pointers;
                if (!ResourceArray(owner + offset, 16384, out data, out count) || count == 0 ||
                    !ResourceBytes(data, count * 8, out pointers)) continue;
                for (int i = 0; i < count; i++)
                {
                    ulong component = BitConverter.ToUInt64(pointers, i * 8);
                    if (AddressGuard.IsPointerValid(component)) result.Add(component);
                }
            }
            if (result.Count > 0) resourceOwnersRead++;
            return result.ToArray();
        }
        private void DiscoverResourceComponents(ulong owner)
        {
            ulong[] pointers = ReadFoliageComponents(owner);
            int count = pointers.Length;
            if (count == 0) return;
            // Bound work per controller. The list is revisited with a rotating window.
            int start;
            foliageDiscoveryOffsets.TryGetValue(owner, out start);
            if (start >= count) start = 0;
            foliageDiscoveryOffsets[owner] = start + 64;
            for (int i = start; i < Math.Min(count, start + 64); i++)
            {
                ulong component = pointers[i], mesh, cls;
                if (!AddressGuard.IsPointerValid(component)) continue;
                resourcePointersSeen++;
                if (!reader.TryReadPointer(component + config.UObjectClass.Value, out cls) ||
                    !ResourceSubclass(cls, "InstancedStaticMeshComponent") || !reader.TryReadPointer(component + 0x688, out mesh)) continue;
                resourceInstanceComponents++;
                if (resourceDiscoverySample.Length == 0)
                {
                    string componentName = string.Empty, className = string.Empty, attachedName = string.Empty;
                    ulong attached;
                    names.TryReadObjectName(component, out componentName);
                    names.TryReadObjectName(cls, out className);
                    if (reader.TryReadPointer(component + 0x760, out attached) && AddressGuard.IsPointerValid(attached)) names.TryReadObjectName(attached, out attachedName);
                    resourceDiscoverySample = componentName + "/" + className + "/" + attachedName;
                }
                ResourceComponent existing;
                if (resourceComponents.TryGetValue(component, out existing) && existing.Mesh == mesh && existing.Owner == owner) continue;
                string[] resources = ReadHarvestResources(component);
                if (resources.Length == 0) continue;
                resourceComponents[component] = new ResourceComponent { Owner = owner, Address = component, Mesh = mesh, Resources = resources };
            }
        }
        private void CaptureResources(TrackerSnapshot snapshot)
        {
            var options = resourceOptions;
            ulong world = uWorld;
            if (world != resourceWorld)
            {
                resourceWorld = world; resourceComponents.Clear(); harvestResources.Clear(); foliageDiscovery.Clear(); foliageDiscoveryOffsets.Clear(); nextResourceDiscovery = DateTime.MinValue;
            }
            if (!object.ReferenceEquals(options, lastResourceOptions))
            {
                foreach (ResourceComponent component in resourceComponents.Values)
                { component.Cursor = 0; component.Pending.Clear(); component.Points.Clear(); component.NextScan = DateTime.MinValue; }
                lastResourceOptions = options;
            }
            if (!options.Enabled || !snapshot.HasLocalPlayer || !string.Equals(config.ExpectedSha256,
                "9BC401417A776C5244A1B0B3255DC3AF4A9D73E3F5C1BA96228FBE3FB1A43477", StringComparison.OrdinalIgnoreCase)) return;
            DateTime now = DateTime.UtcNow;
            if (now >= nextResourceDiscovery && foliageDiscovery.Count == 0)
            {
                foreach (ulong stale in resourceComponents.Where(p => !foliageActors.Contains(p.Value.Owner)).Select(p => p.Key).ToArray()) resourceComponents.Remove(stale);
                foreach (ulong stale in foliageDiscoveryOffsets.Keys.Where(id => !foliageActors.Contains(id)).ToArray()) foliageDiscoveryOffsets.Remove(stale);
                foliageDiscovery = new Queue<ulong>(foliageActors);
                nextResourceDiscovery = now.AddSeconds(2);
            }
            if (foliageDiscovery.Count > 0)
            {
                ulong owner = foliageDiscovery.Dequeue();
                if (foliageActors.Contains(owner)) DiscoverResourceComponents(owner);
            }
            ResourceComponent[] components = resourceComponents.Values.ToArray();
            if (components.Length > 0)
            {
                // One 512-instance batch per world capture; never a full-map blocking pass.
                for (int attempt = 0; attempt < components.Length; attempt++)
                {
                    ResourceComponent component = components[(componentCursor++ & int.MaxValue) % components.Length];
                    if (!foliageActors.Contains(component.Owner) || component.NextScan > now || !component.Resources.Any(options.Selected.Contains)) continue;
                    ReadResourceBatch(component, snapshot.LocalPosition, options);
                    break;
                }
            }
            snapshot.Actors.AddRange(components.Where(c => foliageActors.Contains(c.Owner) && c.Resources.Any(options.Selected.Contains))
                .SelectMany(c => c.Points).Where(p => TrackerFilter.Distance3D(p.Position, snapshot.LocalPosition) <= options.Distance)
                .OrderBy(p => TrackerFilter.Distance3D(p.Position, snapshot.LocalPosition)).Take(1500));
            if (now >= nextResourceLog)
            {
                Log.Info("Resources: foliage=" + foliageActors.Count + " components=" + components.Length + " points=" + components.Sum(c => c.Points.Count) +
                    " discovery=" + resourceOwnersRead + "/" + resourcePointersSeen + "/" + resourceInstanceComponents +
                    " attached=" + resourceAttachedClasses + " harvest=" + resourceHarvestClasses + "/" + resourceHarvestDefinitions +
                    (resourceDiscoverySample.Length == 0 ? string.Empty : " sample=\"" + resourceDiscoverySample + "\""));
                resourceOwnersRead = resourcePointersSeen = resourceInstanceComponents = resourceAttachedClasses = resourceHarvestClasses = resourceHarvestDefinitions = 0;
                resourceDiscoverySample = string.Empty;
                nextResourceLog = now.AddSeconds(15);
            }
        }
        private void ReadResourceBatch(ResourceComponent component, Vector3 origin, ResourceOptions options)
        {
            ulong data, mesh; int count; byte[] worldBytes;
            ExternalTransform transform;
            if (!reader.TryReadPointer(component.Address + 0x688, out mesh) || mesh != component.Mesh ||
                !ResourceArray(component.Address + 0x708, 1000000, out data, out count) ||
                !ResourceBytes(component.Address + 0xE0, 48, out worldBytes) || !TryDecodeTransform(worldBytes, 0, out transform))
            { component.Points.Clear(); component.Pending.Clear(); component.Cursor = 0; component.NextScan = DateTime.UtcNow.AddSeconds(2); return; }
            if (data != component.Data || count != component.Count) { component.Cursor = 0; component.Pending.Clear(); component.Points.Clear(); }
            component.Data = data; component.Count = count;
            if (component.Cursor == 0)
            {
                ulong removedData; int removedCount; byte[] removedBytes;
                component.Removed.Clear();
                if (!ResourceArray(component.Address + 0x748, 1000000, out removedData, out removedCount))
                { component.Points.Clear(); component.NextScan = DateTime.UtcNow.AddSeconds(2); return; }
                if (removedCount > 0)
                {
                    if (!ResourceBytes(removedData, removedCount * 4, out removedBytes))
                    { component.Points.Clear(); component.NextScan = DateTime.UtcNow.AddSeconds(2); return; }
                    for (int i = 0; i < removedCount; i++) component.Removed.Add(BitConverter.ToInt32(removedBytes, i * 4));
                }
            }
            int batch = Math.Min(512, count - component.Cursor);
            byte[] matrices;
            if (batch > 0 && ResourceBytes(data + (ulong)(component.Cursor * 0x50), batch * 0x50, out matrices))
            {
                for (int i = 0; i < batch; i++)
                {
                    if (component.Removed.Contains(component.Cursor + i)) continue;
                    Vector3 local;
                    if (!DecodeResourceInstance(matrices, i * 0x50, out local)) continue;
                    Vector3 position = TransformPosition(local, transform);
                    float distance = TrackerFilter.Distance3D(position, origin);
                    if (float.IsNaN(distance) || float.IsInfinity(distance) || distance > options.Distance) continue;
                    component.Pending.Add(new ActorRecord { Address = data + (ulong)((component.Cursor + i) * 0x50),
                        Kind = ActorKind.Resource, ClassName = "HarvestedResource", DisplayName = string.Join(" / ", component.Resources.Select(ResourceCatalog.Label).ToArray()),
                        Position = position, ResourceIds = component.Resources });
                }
            }
            else if (batch > 0) { component.Points.Clear(); component.Pending.Clear(); component.Cursor = 0; component.NextScan = DateTime.UtcNow.AddSeconds(2); return; }
            component.Cursor += batch;
            if (component.Cursor >= count)
            {
                component.Points = component.Pending.OrderBy(p => TrackerFilter.Distance3D(p.Position, origin)).Take(1500).ToList();
                component.Pending = new List<ActorRecord>(); component.Cursor = 0;
                component.NextScan = DateTime.UtcNow.AddSeconds(1);
            }
            // Bound temporary storage even when a forest supplies many selected resources.
            if (component.Pending.Count > 3000) component.Pending = component.Pending.OrderBy(p => TrackerFilter.Distance3D(p.Position, origin)).Take(1500).ToList();
        }
        internal static bool DecodeResourceInstance(byte[] bytes, int offset, out Vector3 position)
        {
            position = default(Vector3);
            if (bytes == null || offset < 0 || offset + 64 > bytes.Length) return false;
            double scaleSquared = 0;
            for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
            { float v = BitConverter.ToSingle(bytes, offset + row * 16 + col * 4); if (float.IsNaN(v) || float.IsInfinity(v)) return false; scaleSquared += v * v; }
            // Depleted instances are commonly hidden by a zero-scale matrix.
            if (scaleSquared < 0.000001) return false;
            position = new Vector3 { X = BitConverter.ToSingle(bytes, offset + 48), Y = BitConverter.ToSingle(bytes, offset + 52), Z = BitConverter.ToSingle(bytes, offset + 56) };
            return !float.IsNaN(position.X) && !float.IsInfinity(position.X) && !float.IsNaN(position.Y) && !float.IsInfinity(position.Y) && !float.IsNaN(position.Z) && !float.IsInfinity(position.Z);
        }
    }
}
