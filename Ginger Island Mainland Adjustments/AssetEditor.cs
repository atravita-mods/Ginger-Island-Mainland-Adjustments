﻿using StardewModdingAPI.Utilities;

namespace GingerIslandMainlandAdjustments;

/// <summary>
/// Manages asset editing for this mod.
/// </summary>
internal class AssetEditor : IAssetEditor
{
    // The following dialogue is edited from the code side so each NPC has at least the Resort dialogue.
    // A CP pack will override, since my asset managers are registered in Entry and CP registers in GameLaunched.
    private static readonly string GeorgeDialogueLocation = PathUtilities.NormalizeAssetName("Characters/Dialogue/George");
    private static readonly string EvelynDialogueLocation = PathUtilities.NormalizeAssetName("Characters/Dialogue/Evelyn");
    private static readonly string SandyDialogueLocation = PathUtilities.NormalizeAssetName("Characters/Dialogue/Sandy");
    private static readonly string WillyDialogueLocation = PathUtilities.NormalizeAssetName("Characters/Dialogue/Willy");

    // We edit Pam's phone dialogue into Strings/Characters so content packs can target that.
    private static readonly string PhoneStringLocation = PathUtilities.NormalizeAssetName("Strings/Characters");

    // We edit Pam's nine heart event to set flags to remember which path the player chose.
    private static readonly string DataEventsTrailerBig = PathUtilities.NormalizeAssetName("Data/Events/Trailer_Big");

    // A ten heart event and letter are included to unlock the phone.
    private static readonly string DataEventsSeedshop = PathUtilities.NormalizeAssetName("Data/Events/SeedShop");
    private static readonly string DataMail = PathUtilities.NormalizeAssetName("Data/mail");

    private static readonly string[] FilesToEdit = new string[]
    {
        GeorgeDialogueLocation,
        EvelynDialogueLocation,
        SandyDialogueLocation,
        WillyDialogueLocation,
        PhoneStringLocation,
        DataEventsTrailerBig,
        DataEventsSeedshop,
        DataMail,
    };

    private static readonly Lazy<AssetEditor> Lazy = new(() => new AssetEditor());

    private AssetEditor()
    {
    }

    /// <summary>
    /// Gets the instance of the AssetEditor.
    /// </summary>
    public static AssetEditor Instance => Lazy.Value;

    /// <inheritdoc />
    public bool CanEdit<T>(IAssetInfo asset)
    {
        return FilesToEdit.Any((string assetpath) => asset.AssetNameEquals(assetpath));
    }

    /// <inheritdoc />
    public void Edit<T>(IAssetData asset)
    {
        IAssetDataForDictionary<string, string>? editor = asset.AsDictionary<string, string>();
        if (asset.AssetNameEquals(GeorgeDialogueLocation))
        {
            editor.Data["Resort"] = I18n.GeorgeResort();
        }
        else if (asset.AssetNameEquals(EvelynDialogueLocation))
        {
            editor.Data["Resort"] = I18n.EvelynResort();
        }
        else if (asset.AssetNameEquals(WillyDialogueLocation))
        {
            editor.Data["Resort"] = I18n.WillyResort();
        }
        else if (asset.AssetNameEquals(SandyDialogueLocation))
        {
            foreach (string key in new string[] { "Resort", "Resort_Bar", "Resort_Bar_2", "Resort_Wander", "Resort_Shore", "Resort_Pier", "Resort_Approach", "Resort_Left" })
            {
                editor.Data[key] = I18n.GetByKey("Sandy_" + key);
            }
        }
        else if (asset.AssetNameEquals(PhoneStringLocation))
        {
            foreach (string key in new string[] { "Pam_Island_1", "Pam_Island_2", "Pam_Island_3", "Pam_Doctor", "Pam_Other", "Pam_Bus_1", "Pam_Bus_2", "Pam_Bus_3", "Pam_Voicemail_Island", "Pam_Voicemail_Doctor", "Pam_Voicemail_Other", "Pam_Voicemail_Bus", "Pam_Bus_Late" })
            {
                editor.Data[key] = I18n.GetByKey(key);
            }
        }
        else if (asset.AssetNameEquals(DataEventsTrailerBig))
        { // Insert mail flags into the vanilla event
            if (editor.Data.TryGetValue("positive", out string? val))
            {
                editor.Data["positive"] = "addMailReceived atravita_GIMA_PamPositive/" + val;
            }
            foreach ((string key, string value) in editor.Data)
            {
                if (key.StartsWith("503180/"))
                {
                    int lastslash = value.LastIndexOf('/');
                    if (lastslash > 0)
                    {
                        editor.Data[key] = value.Insert(lastslash, "/addMailReceived atravita_GIMA_PamInsulted");
                    }
                }
            }
        }
    }
}