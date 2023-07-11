using System.Reflection;
using HarmonyLib;
using ShockLink.Integrations.TW.API;
using TotallyWholesome;
using TotallyWholesome.Managers;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Objects.ConfigObjects;
using TWNetCommon.Data.ControlPackets;
using Yggdrasil.Extensions;

namespace ShockLink.Integrations.TW;

public class TWPatches
{
    public static void ApplyTWIntegration()
    {
        ShockLinkIntegrationTW.Logger.Msg("Patching TotallyWholesome...");

        ApplyPatch(typeof(PiShockManager), "Execute",
            typeof(TWPatches), nameof(PatchExcecute));

        ApplyPatch(typeof(PiShockManager), "RegisterNewToken",
            typeof(TWPatches), nameof(PatchRegisterNewToken));

        ApplyPatch(typeof(PiShockManager), "GetShockerLog",
            typeof(TWPatches), nameof(PatchGetShockerLog));

        ApplyPatch(typeof(PiShockManager), "GetShockerInfo",
            typeof(TWPatches), nameof(PatchGetShockerInfo));
        
        ShockLinkIntegrationTW.Logger.Msg("Successfully patched TotallyWholesome! ShockLink Integration ready!");
    }
    
    
    private static void ApplyPatch(Type originalClass, string originalMethod, Type patchType, string patchMethod)
    {
        try
        {
            var originalMethodInfo = originalClass.GetMethod(originalMethod,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            var patchMethodInfo = patchType.GetMethod(patchMethod,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            ShockLinkIntegrationTW.HarmonyInst.Patch(originalMethodInfo, new HarmonyMethod(patchMethodInfo));
            ShockLinkIntegrationTW.Logger.Msg($"Finished applying Patch {originalMethod}");
        }
        catch (Exception ex)
        {
            ShockLinkIntegrationTW.Logger.Error($"Failed to apply patch for {originalMethod}");
            ShockLinkIntegrationTW.Logger.Error(ex);
        }
    }
    
    public static bool PatchRegisterNewToken(string code, Action<string, string> onCompleted, Action onFailed)
    {
        ShockLinkIntegrationTW.Logger.Msg("PatchRegisterNewToken");
        if (!Guid.TryParse(code, out var guid))
        {
            ShockLinkIntegrationTW.Logger.Warning($"Could not parse valid guid from input shocker id: {code}");
            Main.Instance.MainThreadQueue.Enqueue(onFailed.Invoke);
            return false;
        }

        async void Action()
        {
            var info = await ShockLinkAPI.GetShocker(guid);
            if (info == null)
            {
                ShockLinkIntegrationTW.Logger.Warning("Could not get shocker from remote api, make sure you entered the correct shocker id.");
                Main.Instance.MainThreadQueue.Enqueue(onFailed.Invoke);
                return;
            }

            Configuration.JSONConfig.PiShockShockers.Add(new PiShockShocker(code, info.Name));
            Configuration.SaveConfig();
            Main.Instance.MainThreadQueue.Enqueue(() => onCompleted.Invoke(code, info.Name));
        }

        new Task(Action).Start();
        
        return false;
    }

    public static bool PatchExcecute(ShockOperation op, int duration, int strength)
    {
        Task.Run(async () =>
        {
            if (op == ShockOperation.NoOp) return;
            if ((op == ShockOperation.Beep &&
                 !ConfigManager.Instance.IsActive(AccessType.AllowBeep, LeadManager.Instance.MasterId)) ||
                (op == ShockOperation.Vibrate &&
                 !ConfigManager.Instance.IsActive(AccessType.AllowVibrate, LeadManager.Instance.MasterId)) ||
                (op == ShockOperation.Shock &&
                 !ConfigManager.Instance.IsActive(AccessType.AllowShock, LeadManager.Instance.MasterId)))

            {
                ShockLinkIntegrationTW.Logger.Msg("PiShockManager.Execute not allowed");
                return;
            }

            ShockLinkIntegrationTW.Logger.Msg($"{op} - {duration} - {strength}");

            var shockers = Configuration.JSONConfig.PiShockShockers;

            if (!shockers.Any(x => x.Enabled)) return;

            PiShockShocker? shocker;

            if (ConfigManager.Instance.IsActive(AccessType.PiShockRandomShocker, LeadManager.Instance.MasterId))
            {
                shocker = shockers.Where(x => x.Enabled).Random();
            }
            else
            {
                shocker = shockers.FirstOrDefault(x => x.Prioritized && x.Enabled) ??
                          shockers.FirstOrDefault(x => x.Enabled);
            }

            if (shocker == null)
            {
                ShockLinkIntegrationTW.Logger.Warning("No shocker configured");
                return;
            }

            ShockLinkIntegrationTW.Logger.Msg($"selected shocker {shocker.Name} {shocker.Key}");

            if (!OperationTranslation.TryGetValue(op, out var translated)) translated = ControlType.Vibrate;

            var control = new Control
            {
                Duration = (uint)Math.Ceiling(duration/15f*ShockLinkIntegrationTW.DurationLimit.Value) * 1000,
                Intensity = (byte)Math.Ceiling(strength/100f*ShockLinkIntegrationTW.IntensityLimit.Value),
                Id = Guid.Parse(shocker.Key),
                Type = translated
            };

            await ShockLinkAPI.Control(control);
        });

        return false;
    }

    public static bool PatchGetShockerLog(string code, out Task<PiShockerLog[]> __result)
    {
        ShockLinkIntegrationTW.Logger.Msg("Getting shocker logs...");
        if (Guid.TryParse(code, out var guid))
        {
            __result = GetShockerLogsInternal(guid);
            return false;
        }
        __result = Task.FromResult(new PiShockerLog[] { });
        return false;
    }

    private static async Task<PiShockerLog[]> GetShockerLogsInternal(Guid shocker)
    {
        var response = await ShockLinkAPI.GetShockerLogs(shocker);
        if (response == null)
        {
            ShockLinkIntegrationTW.Logger.Warning($"Shocker logs could not be retrieved for {shocker}");
            return new PiShockerLog[] { };
        }

        return response.Select(x => new PiShockerLog()
        {
            Duration = (int)(x.Duration / 1000f),
            Intensity = x.Intensity,
            Origin = x.ControlledBy.Name,
            Type = 1,
            Code = 200,
            Op = OperationTranslationLog[x.Type],
            Username = "Custom Name",
            Tm = x.CreatedOn.ToString("O")
        }).ToArray();
    }

    public static bool PatchGetShockerInfo(string key, out Task<PiShockerInfo> __result)
    {
        ShockLinkIntegrationTW.Logger.Msg("Getting shocker info...");
        if (Guid.TryParse(key, out var guid))
        {
            __result = GetShockerInfoInternal(guid)!;
            return false;
        }
        
        __result = null!;
        return false;
    }

    private static async Task<PiShockerInfo?> GetShockerInfoInternal(Guid shocker)
    {
        var response = await ShockLinkAPI.GetShocker(shocker);
        if (response == null)
        {
            ShockLinkIntegrationTW.Logger.Warning($"Shocker info could not be retrieved for {shocker}");
            return null;
        }
        return new PiShockerInfo
        {
            Name = response.Name,
            MaxDuration = 15,
            MaxIntensity = 100
        };
    }

    private static readonly IReadOnlyDictionary<ShockOperation, ControlType> OperationTranslation =
        new Dictionary<ShockOperation, ControlType>
        {
            { ShockOperation.Shock, ControlType.Shock },
            { ShockOperation.Vibrate, ControlType.Vibrate },
            { ShockOperation.Beep, ControlType.Sound }
        };
    
    private static readonly IReadOnlyDictionary<ControlType, int> OperationTranslationLog =
        new Dictionary<ControlType, int>
        {
            { ControlType.Shock, 1 },
            { ControlType.Vibrate, 2 },
            { ControlType.Sound, 4 }
        };
}