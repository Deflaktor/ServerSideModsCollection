using MonoMod.RuntimeDetour;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Networking;

namespace PerClientSync
{
    // Unity's networking writes an object's changed fields once and sends that update to every client that has the object.
    // Objects with client views are written for each client instead, with that client's values, and they spawn for each
    // client with its values too. Hidden objects don't spawn for their clients at all, so nothing about them reaches them.
    internal static class Replication
    {
        private static readonly Dictionary<NetworkIdentity, List<ClientView>> views = new Dictionary<NetworkIdentity, List<ClientView>>();
        private static readonly Dictionary<NetworkIdentity, HashSet<NetworkConnection>> hidden = new Dictionary<NetworkIdentity, HashSet<NetworkConnection>>();
        private static readonly List<Hook> hooks = new List<Hook>();
        // an update for one client
        private static readonly NetworkWriter update = new NetworkWriter();
        // a component's part of an update
        private static readonly NetworkWriter part = new NetworkWriter();
        // the object whose spawn message is being written, and the client it is for
        private static NetworkIdentity spawning;
        private static NetworkConnection spawningFor;

        internal static void Hook()
        {
            Detour(typeof(NetworkIdentity), nameof(NetworkIdentity.UNetUpdate), Type.EmptyTypes, nameof(NetworkIdentity_UNetUpdate));
            Detour(typeof(NetworkIdentity), nameof(NetworkIdentity.UNetSerializeAllVars), new[] { typeof(NetworkWriter) }, nameof(NetworkIdentity_UNetSerializeAllVars));
            Detour(typeof(NetworkIdentity), nameof(NetworkIdentity.AddObserver), new[] { typeof(NetworkConnection) }, nameof(NetworkIdentity_AddObserver));
            Detour(typeof(NetworkServer), nameof(NetworkServer.SendSpawnMessage), new[] { typeof(NetworkIdentity), typeof(NetworkConnection) }, nameof(NetworkServer_SendSpawnMessage));
            PlayerCharacterMasterController.onLinkedToNetworkUserServerGlobal += OnPlayerLinked;
        }

        private static void Detour(Type type, string method, Type[] parameters, string handler)
        {
            var from = type.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null);
            var to = typeof(Replication).GetMethod(handler, BindingFlags.Static | BindingFlags.NonPublic);
            hooks.Add(new Hook(from, to));
        }

        internal static void Add(ClientView view)
        {
            var identity = view.GetComponent<NetworkIdentity>();
            var target = view.Target;
            if (!identity || !target)
            {
                PerClientSyncPlugin.Log.LogError($"{view.GetType().Name} on {view.name} needs a NetworkIdentity and the component it shows.");
                return;
            }
            if (!views.TryGetValue(identity, out var objectViews))
            {
                objectViews = new List<ClientView>();
                views[identity] = objectViews;
            }
            if (objectViews.Any(other => other.Target == target))
            {
                PerClientSyncPlugin.Log.LogError($"{view.GetType().Name} on {view.name}: its {target.GetType().Name} already has a view.");
                return;
            }
            view.identity = identity;
            objectViews.Add(view);
        }

        internal static void Remove(ClientView view)
        {
            // also while the object is being destroyed
            if ((object)view.identity != null && views.TryGetValue(view.identity, out var objectViews))
            {
                objectViews.Remove(view);
                if (objectViews.Count == 0)
                {
                    views.Remove(view.identity);
                }
            }
            view.identity = null;
        }

        private static ClientView FindView(List<ClientView> objectViews, NetworkBehaviour behaviour)
        {
            foreach (var view in objectViews)
            {
                if (view.Target == behaviour)
                {
                    return view;
                }
            }
            return null;
        }

        private static void NetworkIdentity_UNetUpdate(Action<NetworkIdentity> orig, NetworkIdentity self)
        {
            if (views.Count == 0 || self.m_NetworkBehaviours == null || !views.TryGetValue(self, out var objectViews))
            {
                orig(self);
                return;
            }
            try
            {
                Update(self, objectViews);
            }
            catch (Exception exception)
            {
                // the views go, so the object's updates are Unity's own again
                PerClientSyncPlugin.Log.LogError($"Could not update {self.name} for each client, so its views are removed: {exception}");
                foreach (var view in objectViews.ToList())
                {
                    view.enabled = false;
                }
            }
        }

        // Unity's networking sends the channels of an object's changed components; channels with something for a client
        // alone are sent too
        private static void Update(NetworkIdentity identity, List<ClientView> objectViews)
        {
            uint dirtyChannels = 0u;
            foreach (var behaviour in identity.m_NetworkBehaviours)
            {
                int channel = behaviour.GetDirtyChannel();
                if (channel != -1)
                {
                    dirtyChannels |= 1u << (channel & 31);
                }
            }
            foreach (var view in objectViews)
            {
                if (view.pendingBits.Count > 0)
                {
                    dirtyChannels |= 1u << (view.Target.GetNetworkChannel() & 31);
                }
            }
            if (dirtyChannels == 0u)
            {
                return;
            }
            for (int channel = 0; channel < NetworkServer.numChannels; channel++)
            {
                if ((dirtyChannels & (1u << (channel & 31))) != 0u)
                {
                    UpdateChannel(identity, objectViews, channel);
                }
            }
        }

        private static void UpdateChannel(NetworkIdentity identity, List<ClientView> objectViews, int channel)
        {
            var behaviours = identity.GetBehavioursOfSameChannel(channel, false);
            int count = behaviours.Length;
            var channelViews = new ClientView[count];
            var sharedBits = new uint[count];
            // Components without a view are written once for everyone, as Unity's networking does, so what writing them
            // changes, e.g. an Inventory's equipment marked as sent, happens once
            var sharedParts = new byte[count][];
            bool sharedWrote = false;
            for (int i = 0; i < count; i++)
            {
                var behaviour = behaviours[i];
                channelViews[i] = FindView(objectViews, behaviour);
                sharedBits[i] = behaviour.m_SyncVarDirtyBits;
                if (channelViews[i] == null)
                {
                    part.SeekZero();
                    if (behaviour.OnSerialize(part, false))
                    {
                        behaviour.ClearAllDirtyBits();
                        sharedWrote = true;
                    }
                    sharedParts[i] = part.ToArray();
                }
            }
            // the components without a view in a leading update, written when first needed
            byte[][] emptyParts = null;

            var observers = identity.m_Observers;
            if (observers != null)
            {
                var requestedBits = new uint[count];
                var leadingBits = new uint[count];
                var otherBits = new uint[count];
                // a view's Apply may hide the object from a client
                foreach (var client in observers.ToArray())
                {
                    if (client == null || !client.isReady || PerClient.IsLocal(client))
                    {
                        continue;
                    }
                    bool needed = sharedWrote;
                    bool leading = false;
                    for (int i = 0; i < count; i++)
                    {
                        var view = channelViews[i];
                        if (view == null)
                        {
                            continue;
                        }
                        view.pendingBits.TryGetValue(client, out uint pending);
                        requestedBits[i] = (sharedBits[i] & ~view.PrivateBits) | pending;
                        leadingBits[i] = requestedBits[i] & view.LeadingBits;
                        otherBits[i] = requestedBits[i] & ~view.LeadingBits;
                        needed |= requestedBits[i] != 0u;
                        leading |= leadingBits[i] != 0u;
                    }
                    if (!needed)
                    {
                        continue;
                    }

                    bool sent = true;
                    if (leading)
                    {
                        emptyParts = emptyParts ?? WriteEmptyParts(behaviours, channelViews);
                        sent = !WriteUpdate(identity, client, behaviours, channelViews, leadingBits, emptyParts, false) || client.SendWriter(update, channel);
                    }
                    // without its leading update, the rest would reach the client in the wrong order
                    if (sent)
                    {
                        sent = !WriteUpdate(identity, client, behaviours, channelViews, otherBits, sharedParts, sharedWrote) || client.SendWriter(update, channel);
                    }

                    for (int i = 0; i < count; i++)
                    {
                        var view = channelViews[i];
                        if (view == null)
                        {
                            continue;
                        }
                        // a full send buffer drops the update, so it is tried again with the next one
                        if (sent)
                        {
                            view.pendingBits.Remove(client);
                        }
                        else if (requestedBits[i] != 0u)
                        {
                            view.pendingBits[client] = requestedBits[i];
                        }
                    }
                }
            }

            for (int i = 0; i < count; i++)
            {
                var view = channelViews[i];
                if (view == null)
                {
                    continue;
                }
                if (sharedBits[i] != 0u)
                {
                    behaviours[i].ClearAllDirtyBits();
                }
                // clients that lost the object, e.g. by loading the next scene, get every field when it spawns for them again
                if (view.pendingBits.Count > 0)
                {
                    foreach (var client in view.pendingBits.Keys.Where(client => observers == null || !observers.Contains(client)).ToList())
                    {
                        view.pendingBits.Remove(client);
                    }
                }
            }
        }

        private static byte[][] WriteEmptyParts(NetworkBehaviour[] behaviours, ClientView[] channelViews)
        {
            var emptyParts = new byte[behaviours.Length][];
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (channelViews[i] == null)
                {
                    part.SeekZero();
                    WriteWithBits(part, behaviours[i], 0u);
                    emptyParts[i] = part.ToArray();
                }
            }
            return emptyParts;
        }

        // Writes an update for one client into update: the components with a view with these fields and its values, the
        // others as they were written for everyone. False if no component had anything to send.
        private static bool WriteUpdate(NetworkIdentity identity, NetworkConnection client, NetworkBehaviour[] behaviours, ClientView[] channelViews, uint[] viewBits, byte[][] parts, bool partsWrote)
        {
            update.StartMessage(MsgType.UpdateVars);
            update.Write(identity.netId);
            bool wrote = partsWrote;
            for (int i = 0; i < behaviours.Length; i++)
            {
                short start = update.Position;
                if (channelViews[i] == null)
                {
                    update.Write(parts[i], parts[i].Length);
                }
                else
                {
                    wrote |= WriteForClient(update, behaviours[i], channelViews[i], client, viewBits[i], false);
                }
                if (NetworkIdentity.m_CheckSerializeSizes)
                {
                    update.Write((byte)(update.Position - start));
                }
            }
            update.FinishMessage();
            return wrote;
        }

        // Writes the component with these dirty bits and the client's values; its own dirty bits and fields stay as they were
        private static bool WriteForClient(NetworkWriter writer, NetworkBehaviour behaviour, ClientView view, NetworkConnection client, uint dirtyBits, bool initialState)
        {
            uint realBits = behaviour.m_SyncVarDirtyBits;
            behaviour.m_SyncVarDirtyBits = dirtyBits;
            try
            {
                view.Apply(client);
                return behaviour.OnSerialize(writer, initialState);
            }
            finally
            {
                view.Restore();
                behaviour.m_SyncVarDirtyBits = realBits;
            }
        }

        private static bool WriteWithBits(NetworkWriter writer, NetworkBehaviour behaviour, uint dirtyBits)
        {
            uint realBits = behaviour.m_SyncVarDirtyBits;
            behaviour.m_SyncVarDirtyBits = dirtyBits;
            try
            {
                return behaviour.OnSerialize(writer, false);
            }
            finally
            {
                behaviour.m_SyncVarDirtyBits = realBits;
            }
        }

        internal static byte[] BuildUpdate(NetworkConnection client, NetworkBehaviour component, uint dirtyBits)
        {
            var identity = component.GetComponent<NetworkIdentity>();
            var objectViews = client != null && !PerClient.IsLocal(client) && views.TryGetValue(identity, out var found) ? found : null;
            var writer = new NetworkWriter();
            writer.StartMessage(MsgType.UpdateVars);
            writer.Write(identity.netId);
            foreach (var behaviour in identity.GetBehavioursOfSameChannel(component.GetNetworkChannel(), false))
            {
                short start = writer.Position;
                uint bits = behaviour == component ? dirtyBits : 0u;
                var view = objectViews != null ? FindView(objectViews, behaviour) : null;
                if (view != null)
                {
                    WriteForClient(writer, behaviour, view, client, bits, false);
                }
                else
                {
                    WriteWithBits(writer, behaviour, bits);
                }
                if (NetworkIdentity.m_CheckSerializeSizes)
                {
                    writer.Write((byte)(writer.Position - start));
                }
            }
            writer.FinishMessage();
            return writer.ToArray();
        }

        // Every spawn message is written for one client, so it carries that client's values
        private static void NetworkServer_SendSpawnMessage(Action<NetworkServer, NetworkIdentity, NetworkConnection> orig, NetworkServer self, NetworkIdentity identity, NetworkConnection client)
        {
            if (client != null && IsHidden(identity, client))
            {
                return;
            }
            if (client == null || PerClient.IsLocal(client) || !views.TryGetValue(identity, out var objectViews))
            {
                orig(self, identity, client);
                return;
            }
            var previousSpawning = spawning;
            var previousSpawningFor = spawningFor;
            spawning = identity;
            spawningFor = client;
            try
            {
                orig(self, identity, client);
            }
            finally
            {
                spawning = previousSpawning;
                spawningFor = previousSpawningFor;
            }
            // the spawn carries every field
            foreach (var view in objectViews)
            {
                view.pendingBits.Remove(client);
            }
        }

        private static void NetworkIdentity_UNetSerializeAllVars(Action<NetworkIdentity, NetworkWriter> orig, NetworkIdentity self, NetworkWriter writer)
        {
            if (!ReferenceEquals(self, spawning) || !views.TryGetValue(self, out var objectViews))
            {
                orig(self, writer);
                return;
            }
            foreach (var behaviour in self.m_NetworkBehaviours)
            {
                var view = FindView(objectViews, behaviour);
                if (view != null)
                {
                    WriteForClient(writer, behaviour, view, spawningFor, behaviour.m_SyncVarDirtyBits, true);
                }
                else
                {
                    behaviour.OnSerialize(writer, true);
                }
            }
        }

        // A client that becomes ready, e.g. after loading a scene, or a new object gets its observers here
        private static void NetworkIdentity_AddObserver(Action<NetworkIdentity, NetworkConnection> orig, NetworkIdentity self, NetworkConnection client)
        {
            if (client != null && IsHidden(self, client))
            {
                return;
            }
            orig(self, client);
        }

        private static void OnPlayerLinked(PlayerCharacterMasterController player)
        {
            var client = PerClient.GetConnection(player);
            if (client == null || PerClient.IsLocal(client) || views.Count == 0)
            {
                return;
            }
            foreach (var view in views.Values.SelectMany(objectViews => objectViews).ToList())
            {
                try
                {
                    view.OnPlayerLinked(client);
                }
                catch (Exception exception)
                {
                    PerClientSyncPlugin.Log.LogError($"{view.GetType().Name} on {view.name} could not handle a linked player: {exception}");
                }
            }
        }

        internal static bool IsHidden(NetworkIdentity identity, NetworkConnection client)
        {
            return hidden.Count > 0 && client != null && hidden.TryGetValue(identity, out var clients) && clients.Contains(client);
        }

        internal static void Hide(NetworkIdentity identity, NetworkConnection client)
        {
            if (!NetworkServer.active || !identity || client == null || PerClient.IsLocal(client))
            {
                return;
            }
            ForgetDestroyedObjects();
            if (!hidden.TryGetValue(identity, out var clients))
            {
                clients = new HashSet<NetworkConnection>();
                hidden[identity] = clients;
            }
            if (clients.Add(client) && identity.m_Observers != null && identity.m_Observers.Contains(client))
            {
                // the vanilla message that hides an object from a client, which destroys it there
                identity.RemoveObserver(client);
            }
        }

        internal static void Show(NetworkIdentity identity, NetworkConnection client)
        {
            if (!NetworkServer.active || !identity || client == null || !hidden.TryGetValue(identity, out var clients) || !clients.Remove(client))
            {
                return;
            }
            if (clients.Count == 0)
            {
                hidden.Remove(identity);
            }
            // an object that has spawned, for a client that is in the scene
            if (identity.m_Observers != null && client.isReady && !identity.m_Observers.Contains(client) && identity.OnCheckObserver(client))
            {
                identity.AddObserver(client);
            }
        }

        private static void ForgetDestroyedObjects()
        {
            foreach (var identity in hidden.Keys.Where(identity => !identity).ToList())
            {
                hidden.Remove(identity);
            }
        }
    }
}
