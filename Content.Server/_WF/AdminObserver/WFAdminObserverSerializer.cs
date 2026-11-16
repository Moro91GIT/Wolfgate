using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Shared._WF.AdminObserver;

namespace Content.Server._WF.AdminObserver;

/// <summary>Reads, writes and cleans up <see cref="WFAdminObserverData"/> as the database stores it.</summary>
public static class WFAdminObserverSerializer
{
    private static readonly JsonSerializerOptions Json = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>The document as JSON. False when it is over <see cref="WFAdminObserverData.MaxBytes"/>.</summary>
    public static bool TrySerialize(WFAdminObserverData data, out string json)
    {
        json = JsonSerializer.Serialize(data, Json);
        return Encoding.UTF8.GetByteCount(json) <= WFAdminObserverData.MaxBytes;
    }

    /// <summary>A document from the database, or null when it can't be read or is from a newer version.</summary>
    public static WFAdminObserverData? Deserialize(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<WFAdminObserverData>(json, Json);
            return data is { Version: >= 1 and <= WFAdminObserverData.CurrentVersion } ? data : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Brings a document within its limits and drops what names a prototype that no longer exists: avatars, outfits
    /// and kit items. Returns the same document.
    /// </summary>
    public static WFAdminObserverData Sanitize(
        WFAdminObserverData data,
        Func<string, bool> entityExists,
        Func<string, bool> gearExists)
    {
        data.Version = WFAdminObserverData.CurrentVersion;
        data.Presets ??= new();
        data.Marker ??= new();

        var names = new HashSet<string>();
        var presets = new List<WFAvatarPreset>();
        foreach (var preset in data.Presets)
        {
            if (preset == null || presets.Count >= WFAdminObserverData.MaxPresets)
                continue;

            preset.Name = Clamp(preset.Name, WFAdminObserverData.MaxNameLength).Trim();
            if (preset.Name.Length == 0 || names.Contains(preset.Name))
                continue;

            if (!Enum.IsDefined(preset.Kind) || !Enum.IsDefined(preset.EntryStyle))
                continue;

            if (preset.Kind is WFAvatarKind.Object or WFAvatarKind.Mob)
            {
                if (preset.Prototype == null || !entityExists(preset.Prototype))
                    continue;
            }
            else
            {
                preset.Prototype = null;
            }

            if (preset.Kind != WFAvatarKind.Character)
            {
                preset.CharacterSlot = null;
                preset.Outfit = null;
                preset.OutfitLayout = null;
            }

            if (preset.Outfit != null && !gearExists(preset.Outfit))
                preset.Outfit = null;

            if (preset.OutfitLayout != null)
                SanitizeLayout(preset.OutfitLayout, entityExists);

            preset.DisplayName = Clamp(preset.DisplayName, WFAdminObserverData.MaxDisplayNameLength);
            preset.Description = Clamp(preset.Description, WFAdminObserverData.MaxDescriptionLength);
            names.Add(preset.Name);
            presets.Add(preset);
        }

        data.Presets = presets;
        if (data.DefaultPreset != null && !names.Contains(data.DefaultPreset))
            data.DefaultPreset = null;

        var marker = data.Marker;
        if (!Enum.IsDefined(marker.Wording))
            marker.Wording = WFMarkerWording.Rank;

        marker.Visuals &= AllVisuals;
        if (marker.Visuals == WFMarkerVisuals.None)
            marker.Visuals = WFMarkerVisuals.Nameplate;

        marker.Ring = Clamp(marker.Ring, WFAdminObserverData.MaxNameLength);
        marker.Glyph = Clamp(marker.Glyph, WFAdminObserverData.MaxNameLength);
        marker.Shield = Clamp(marker.Shield, WFAdminObserverData.MaxNameLength);
        marker.Aura = Clamp(marker.Aura, WFAdminObserverData.MaxNameLength);
        if (marker.Color != null && !Color.TryFromHex(marker.Color, out _))
            marker.Color = null;

        if (data.Kit != null)
            SanitizeLayout(data.Kit, entityExists);

        return data;
    }

    private static readonly WFMarkerVisuals AllVisuals = Enum.GetValues<WFMarkerVisuals>()
        .Aggregate(WFMarkerVisuals.None, (all, flag) => all | flag);

    /// <summary>Drops a layout's unknown items with their contents, and whatever is past the depth and count limits.</summary>
    public static void SanitizeLayout(WFKitLayout layout, Func<string, bool> entityExists)
    {
        var budget = WFAdminObserverData.MaxKitNodes;
        layout.Slots = Prune(layout.Slots, entityExists, ref budget);
        layout.Hands = Prune(layout.Hands, entityExists, ref budget);
    }

    private static Dictionary<string, WFKitNode> Prune(
        Dictionary<string, WFKitNode>? nodes,
        Func<string, bool> entityExists,
        ref int budget)
    {
        var kept = new Dictionary<string, WFKitNode>();
        if (nodes == null)
            return kept;

        foreach (var (key, node) in nodes)
        {
            if (PruneNode(node, entityExists, 1, ref budget))
                kept[key] = node;
        }

        return kept;
    }

    private static bool PruneNode(WFKitNode? node, Func<string, bool> entityExists, int depth, ref int budget)
    {
        if (node == null || budget <= 0 || depth > WFAdminObserverData.MaxKitDepth
            || string.IsNullOrEmpty(node.Prototype) || !entityExists(node.Prototype))
        {
            return false;
        }

        budget--;
        var contents = new List<WFKitStored>();
        foreach (var stored in node.Contents ?? new())
        {
            if (stored != null && PruneNode(stored.Node, entityExists, depth + 1, ref budget))
                contents.Add(stored);
        }

        node.Contents = contents;
        return true;
    }

    private static string Clamp(string? text, int length)
    {
        if (text == null)
            return string.Empty;

        return text.Length <= length ? text : text[..length];
    }
}
