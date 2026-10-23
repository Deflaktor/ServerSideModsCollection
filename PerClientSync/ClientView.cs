using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PerClientSync
{
    // Shows each client its own values of a synced component (a NetworkBehaviour) on the same object. Whenever the
    // component is written for a client, in the object's spawn message or in an update, the view puts that client's
    // values into the component's fields and the real ones back afterwards, so each client gets its values in the
    // component's vanilla format. Add it on the host; the host itself sees the real values.
    public abstract class ClientView : MonoBehaviour
    {
        internal NetworkIdentity identity;
        // fields that go to a client with the object's next update, by client
        internal readonly Dictionary<NetworkConnection, uint> pendingBits = new Dictionary<NetworkConnection, uint>();

        // The component whose fields the view changes
        public abstract NetworkBehaviour Target { get; }

        // Fields, as the component's dirty bits, that reach a client only through SetDirty or when the object spawns for
        // it, never when the real field changes, e.g. state that each player has for themselves while the real field
        // holds the host's
        public virtual uint PrivateBits => 0u;

        // Fields, as the component's dirty bits, that go to a client in an update of their own ahead of the others, for
        // the client's hooks of the other fields that read them
        public virtual uint LeadingBits => 0u;

        // Puts this client's values into the component's fields
        protected internal abstract void Apply(NetworkConnection client);

        // Puts the real values back; runs after every Apply, also one that failed partway
        protected internal abstract void Restore();

        // Sends these fields to this client with the object's next update
        public void SetDirty(NetworkConnection client, uint dirtyBits)
        {
            if (client == null || dirtyBits == 0u || PerClient.IsLocal(client))
            {
                return;
            }
            pendingBits.TryGetValue(client, out uint bits);
            pendingBits[client] = bits | dirtyBits;
        }

        // Sends these fields to every client that has the object, with its next update
        public void SetDirtyForAll(uint dirtyBits)
        {
            var observers = identity ? identity.m_Observers : null;
            if (observers == null)
            {
                return;
            }
            foreach (var client in observers)
            {
                SetDirty(client, dirtyBits);
            }
        }

        // A player was linked to this client after the object spawned for it, e.g. a player who joined late or came back,
        // so the client may have been sent values that aren't theirs. By default it gets every field again.
        protected internal virtual void OnPlayerLinked(NetworkConnection client)
        {
            SetDirty(client, uint.MaxValue);
        }

        protected virtual void OnEnable()
        {
            Replication.Add(this);
        }

        protected virtual void OnDisable()
        {
            Replication.Remove(this);
            pendingBits.Clear();
        }
    }

    // A view of the object's component of type T
    public abstract class ClientView<T> : ClientView where T : NetworkBehaviour
    {
        private T component;

        public T Component => component ? component : component = GetComponent<T>();

        public override NetworkBehaviour Target => Component;
    }
}
