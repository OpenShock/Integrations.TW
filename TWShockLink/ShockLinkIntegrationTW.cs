using System.Drawing;
using HarmonyLib;
using MelonLoader;
using ShockLink.Integrations.TW;
using ShockLink.Integrations.TW.API;

[assembly: MelonInfo(typeof(ShockLinkIntegrationTW), "ShockLink.Integrations.TW", "1.0.1", "ShockLink Team")]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonOptionalDependencies("TotallyWholesome")]

namespace ShockLink.Integrations.TW;

public class ShockLinkIntegrationTW : MelonMod
{
    internal static readonly MelonLogger.Instance Logger = new("ShockLink.Integrations.TW", Color.LawnGreen);
    internal static HarmonyLib.Harmony HarmonyInst;
    internal static MelonPreferences_Entry<int> IntensityLimit;
    internal static MelonPreferences_Entry<float> DurationLimit;
    internal static MelonPreferences_Entry<string> APIToken;
    internal static MelonPreferences_Entry<string> APIEndpoint;

    private const string DefaultBaseUri = "https://api.shocklink.net";

    public override void OnInitializeMelon()
    {
        HarmonyInst = HarmonyInstance;
        
        Logger.Msg("Getting config options..");
        var category = MelonPreferences.CreateCategory("ShockLinkIntegrationsTW");

        APIToken = category.CreateEntry("APIToken", "", "ShockLink API Token", "Your generated API Token for ShockLink, you can get this from the website");
        APIEndpoint = category.CreateEntry("APIBaseUri", DefaultBaseUri, "ShockLink API URL", "Base API URL for ShockLink");

        IntensityLimit = MelonPreferences.CreateEntry("ShockLinkIntegrationsTW", "IntensityLimit", 25, "Intensity Limit", "This sets the maximum intensity allowed for ShockLink operations");
        DurationLimit = MelonPreferences.CreateEntry("ShockLinkIntegrationsTW", "DurationLimit", 10f, "Duration Limit", "This sets the maximum duration allowed for ShockLink operations");

        APIToken.OnEntryValueChanged.Subscribe(
            (_, newValue) => ShockLinkAPI.Reload(APIEndpoint.Value, newValue));
        APIEndpoint.OnEntryValueChanged.Subscribe(
            (_, newValue) => ShockLinkAPI.Reload(newValue, APIToken.Value));

        ShockLinkAPI.Reload(APIEndpoint.Value, APIToken.Value);
        
        Logger.Msg("Waiting for TotallyWholesome to be loaded...");
    }
}

[HarmonyPatch(typeof(WholesomeLoader.WholesomeLoader))]
class WholesomeLoaderPatch
{
    [HarmonyPatch("LoadModuleCore")]
    [HarmonyPostfix]
    static void LoadModuleCorePostfix()
    {
        var tw = MelonBase.RegisteredMelons.FirstOrDefault(x => x.Info.Name.Equals("TotallyWholesome"));

        if (tw == null)
        {
            ShockLinkIntegrationTW.Logger.Msg("TotallyWholesome failed to load, unable to apply ShockLink integration!");
            return;
        }
        
        //Let's do the thing!
        ShockLinkIntegrationTW.Logger.Msg("Detected TotallyWholesome, applying ShockLink integration!");
        TWPatches.ApplyTWIntegration();
    }
}