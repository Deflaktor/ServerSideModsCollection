# PerClientSync

Normally every player in a multiplayer game sees the same world. PerClientSync lets a host-side mod break that rule on purpose: each client can get its own version of a synced object, and an object can be hidden from some clients altogether. It intentionally causes a desync in a controlled way.

The clients don't need the mod. They get the same messages an unmodded host would send, just with different values in them.

It's meant for things like shops where every player buys for themselves, or a price sign that only disappears for the player who bought the item. It's a library, so it does nothing on its own and is only needed when another mod depends on it.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/E1E71PHUJ2)

## Developer manual

All of this runs on the host, which is also the server. The host's own player sees the server's objects exactly as they are. Nothing per-client ever reaches it, so anything that should look different only for the host's player is up to the mod's own code.

### Client views

A `ClientView` gives each client its own values for a synced component (a `NetworkBehaviour`). The trick is simple: whenever the component gets written for a client, either in the object's spawn message or in an update, the view swaps that client's values into the component's fields first and puts the real ones back right after. The view is added to the object on the host:

```csharp
// Each player sees only their own purchase of the terminal
public class OwnPurchaseView : ClientView<PurchaseInteraction>
{
    // PurchaseInteraction's dirty bit for available
    private const uint AvailableBit = 8u;

    private readonly HashSet<PlayerCharacterMasterController> buyers = new HashSet<PlayerCharacterMasterController>();
    private bool realAvailable;

    // the real field changing doesn't reach clients, only Buy does
    public override uint PrivateBits => AvailableBit;

    protected override void Apply(NetworkConnection client)
    {
        realAvailable = Component.available;
        Component.available = !buyers.Contains(PerClient.GetPlayer(client));
    }

    protected override void Restore()
    {
        Component.available = realAvailable;
    }

    public void Buy(PlayerCharacterMasterController player)
    {
        buyers.Add(player);
        SetDirty(PerClient.GetConnection(player), AvailableBit);
    }
}

terminal.AddComponent<OwnPurchaseView>();
```

Fields are referred to by the component's dirty bits. Unity's networking gives each `[SyncVar]` field one, in the order they are declared: 1, 2, 4, 8 and so on. The easiest way to find them is the decompiled `Network<field>` setters, e.g. `SetSyncVar(value, ref available, 8u)`.

- `Apply(client)` and `Restore()`: swap the client's values into the fields, and the real ones back. `Restore` always runs after `Apply`. Since the component is written once per client, these two shouldn't touch anything else.
- `SetDirty(client, dirtyBits)`: sends these fields to that client with the object's next update. `SetDirtyForAll(dirtyBits)` does the same for every client that has the object.
- `PrivateBits`: fields that only reach a client through `SetDirty` or when the object spawns for it. For a field left out, a change of the real value still goes out to every client, each with their own value.
- `LeadingBits`: fields that are sent to a client in a separate update before the others. This is needed when the client's hooks for the other fields read them. A Lunar Bud is a good example: its pickup's hook checks `hasBeenPurchased` to play the right animation.
- `OnPlayerLinked(client)`: called when a player gets linked to the client after the object already spawned for it, e.g. someone who joined late or reconnected. By default the client just gets every field again.

Objects spawn for each client with that client's values, and that includes players who join late and every scene change. There can only be one view per component. A view added after the object has spawned should send what it changes with `SetDirtyForAll`, and one that is removed should send the real values back.

### Hiding objects

- `PerClient.Hide(identity, client)`: removes the object from that client's game using the vanilla message, and keeps it from spawning there again. From then on, nothing about the object reaches that client.
- `PerClient.Show(identity, client)`: spawns it for the client again.

### Single updates

Sometimes updates have to arrive in a particular order, e.g. when a field is cleared and set again so the client replays what its hook does. For that there are:

- `PerClient.BuildUpdate(client, component, dirtyBits)`: builds an update of just these fields for that client, with its view's values, the way Unity's networking would write it. Nothing is marked as sent. Send it on the component's channel.
- `PerClient.SendUpdate(client, component, dirtyBits)`: builds it and sends it right away.

### Clients and players

- `PerClient.GetConnection(player)`: the connection of a player's client.
- `PerClient.GetPlayer(client)`: the player of a client, or null until one is linked to it. Splitscreen players share a client, so they always see the same thing.
- `PerClient.IsLocal(client)`: the host's own connection.

### Limits

- Clients work some things out themselves from what they receive, like their own character's stats. Values that feed into that change how the game plays for them, not just what they see.
- `SyncList`s, RPCs and messages sent to everyone aren't covered.
- Some components change their own state when they are written, e.g. an `Inventory` marks its equipment as sent. Those need care in a view, since they get written once per client.
