using FrooxEngine;
using HarmonyLib;
using Renderite.Shared;
using Elements.Core;
using BepInExResoniteShim;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.NET.Common;
using BepisLocaleLoader;
using BepInEx.Logging;

namespace Optizoom;

[ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(BepisLocaleLoader.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Optizoom : BasePlugin
{
    private static ConfigEntry<bool> enabled;
    private static ConfigEntry<Key> zoomKey;
    private static ConfigEntry<float> zoomFOV;
    private static ConfigEntry<bool> toggleZoom;
    private static ConfigEntry<bool> lerpZoom;
    private static ConfigEntry<float> zoomSpeed;
    private static ConfigEntry<bool> scrollZoom;
    private static ConfigEntry<float> scrollZoomSpeed;

    private static ConfigEntry<bool> enableOverlay;
    private static ConfigEntry<float2> overlaySize;
    private static ConfigEntry<string> overlayUri;
    private static ConfigEntry<bool> overlayBg;
    private static ConfigEntry<colorX> overlayBgColor;

    private static ConfigEntry<string> zoomInSound;
    private static ConfigEntry<string> zoomOutSound;
    private static ConfigEntry<float> zoomVolume;
    private static ConfigEntry<bool> exposeZoomedInVariable;

    private static Slot overlayVisual;

    static ManualLogSource Logger = null!;

    private static bool toggleState = false;

    public override void Load()
    {
        enabled = Config.Bind("Optizoom", "enabled", true, new ConfigDescription("Enable Optizoom", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.enabled.Key", "Settings.dev.lecloutpanda.optizoom.enabled.Description")));
        zoomKey = Config.Bind("Optizoom", "keyBind", Key.Tab, new ConfigDescription("Zoom Key", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.keyBind.Key", "Settings.dev.lecloutpanda.optizoom.keyBind.Description")));
        zoomFOV = Config.Bind("Optizoom", "zoomFOV", 7f, new ConfigDescription("Zoom FOV", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.zoomFOV.Key", "Settings.dev.lecloutpanda.optizoom.zoomFOV.Description")));

        toggleZoom = Config.Bind("Optizoom", "toggleZoom", false, new ConfigDescription("Toggle Zoom", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.toggleZoom.Key", "Settings.dev.lecloutpanda.optizoom.toggleZoom.Description")));
        lerpZoom = Config.Bind("Optizoom", "lerpZoom", true, new ConfigDescription("Lerp Zoom", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.lerpZoom.Key", "Settings.dev.lecloutpanda.optizoom.lerpZoom.Description")));
        zoomSpeed = Config.Bind("Optizoom", "zoomSpeed", 50f, new ConfigDescription("Zoom Speed", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.zoomSpeed.Key", "Settings.dev.lecloutpanda.optizoom.zoomSpeed.Description")));

        scrollZoom = Config.Bind("Optizoom", "scrollZoom", true, new ConfigDescription("Zoom with scroll", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.scrollZoom.Key", "Settings.dev.lecloutpanda.optizoom.scrollZoom.Description")));
        scrollZoomSpeed = Config.Bind("Optizoom", "scrollZoomSpeed", 50f, new ConfigDescription("Scroll Zoom Speed", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.scrollZoomSpeed.Key", "Settings.dev.lecloutpanda.optizoom.scrollZoomSpeed.Description")));

        enableOverlay = Config.Bind("Overlay", "enableOverlay", true, new ConfigDescription("Enable Overlay", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.enableOverlay.Key", "Settings.dev.lecloutpanda.optizoom.enableOverlay.Description")));
        overlaySize = Config.Bind("Overlay", "overlaySize", float2.One * 1.12f, new ConfigDescription("Overlay Size", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.overlaySize.Key", "Settings.dev.lecloutpanda.optizoom.overlaySize.Description")));
        overlayUri = Config.Bind("Overlay", "overlayUri", "resdb:///55b0aea6dcdce645b3f01ff83877b88f16402155f4ba54bced02aa6bdae528b9.png", new ConfigDescription("Overlay Uri", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.overlayUri.Key", "Settings.dev.lecloutpanda.optizoom.overlayUri.Description")));
        overlayBg = Config.Bind("Overlay", "overlayBg", true, new ConfigDescription("Enable Overlay Background", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.overlayBg.Key", "Settings.dev.lecloutpanda.optizoom.overlayBg.Description")));
        overlayBgColor = Config.Bind("Overlay", "overlayBgColor", colorX.Black, new ConfigDescription("Overlay Background Color", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.overlayBgColor.Key", "Settings.dev.lecloutpanda.optizoom.overlayBgColor.Description")));

        zoomInSound = Config.Bind("Sound", "zoomInSound", "", new ConfigDescription("Zoom In Sound URI (null to disable)", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.zoomInSound.Key", "Settings.dev.lecloutpanda.optizoom.zoomInSound.Description")));
        zoomOutSound = Config.Bind("Sound", "zoomOutSound", "", new ConfigDescription("Zoom Out Sound URI (null to disable)", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.zoomOutSound.Key", "Settings.dev.lecloutpanda.optizoom.zoomOutSound.Description")));
        zoomVolume = Config.Bind("Sound", "zoomVolume", 1f, new ConfigDescription("Zoom Volume", new AcceptableValueRange<float>(0f, 1f), new ConfigLocale("Settings.dev.lecloutpanda.optizoom.zoomVolume.Key", "Settings.dev.lecloutpanda.optizoom.zoomVolume.Description")));

        exposeZoomedInVariable = Config.Bind("Optizoom", "exposeZoomedInVariable", false, new ConfigDescription("Expose zoomed-in variable on user (User/optizoom.zoomed_in)", null, new ConfigLocale("Settings.dev.lecloutpanda.optizoom.exposeZoomedInVariable.Key", "Settings.dev.lecloutpanda.optizoom.exposeZoomedInVariable.Description")));

        Logger = Log;
        HarmonyInstance.PatchAll();

        overlaySize.SettingChanged += (_, _) => TryWriteDynamicValue(overlayVisual, "OverlayVisual/overlaySize", overlaySize.Value); 
        overlayUri.SettingChanged += (_, _) => TryWriteDynamicValue(overlayVisual, "OverlayVisual/overlayUri", overlayUri.Value);
        overlayBg.SettingChanged += (_, _) => TryWriteDynamicValue(overlayVisual, "OverlayVisual/overlayBg", overlayBg.Value);
        overlayBgColor.SettingChanged += (_, _) => TryWriteDynamicValue(overlayVisual, "OverlayVisual/overlayBgColor", overlayBgColor.Value);
        zoomInSound.SettingChanged += (_, _) => TryWriteDynamicValue(overlayVisual, "OverlayVisual/zoomInSoundUri", zoomInSound.Value);
        zoomOutSound.SettingChanged += (_, _) => TryWriteDynamicValue(overlayVisual, "OverlayVisual/zoomOutSoundUri", zoomOutSound.Value);
        toggleZoom.SettingChanged += (_, _) => toggleState = false;
    }

    [HarmonyPatch]
    private static class SpyglassUserspacePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Userspace), "OnAttach")]
        public static void Postfix(Userspace __instance)
        {
            Slot overlayRoot = __instance.World.GetGloballyRegisteredComponent<OverlayManager>().OverlayRoot;
            overlayVisual = overlayRoot.AddSlot("OverlayVisual - Optizoom");
            overlayVisual.PersistentSelf = false;
            overlayVisual.LocalPosition = float3.Forward * 0.1f;
            overlayVisual.ActiveSelf = false;

            overlayVisual.AttachComponent<DynamicVariableSpace>().SpaceName.Value = "OverlayVisual";

            Uri texUri = new Uri(overlayUri.Value);
            var texture = overlayVisual.AttachTexture(texUri, wrapMode: TextureWrapMode.Clamp);
            texture.FilterMode.Value = TextureFilterMode.Point;
            // Overlay Texture
            texture.URL.SyncWithVariable("overlayUri");

            var unlit = overlayVisual.AttachComponent<UnlitMaterial>();
            unlit.Texture.TrySet(texture);
            unlit.BlendMode.Value = BlendMode.Alpha;

            var overlayQuad = overlayVisual.AttachQuad(overlaySize.Value, unlit, false);
            overlayQuad.Size.SyncWithVariable("overlaySize");

            var frameUnlit = overlayVisual.AttachComponent<UnlitMaterial>();
            frameUnlit.TintColor.Value = overlayBgColor.Value;
            // BGColor
            frameUnlit.TintColor.SyncWithVariable("overlayBgColor");

            var frame = overlayVisual.AttachComponent<FrameMesh>();

            var frameRenderer = overlayVisual.AttachMesh(frame, frameUnlit);
            frame.Thickness.Value = 5f;
            frame.ContentSize.DriveFrom(overlayQuad.Size);
            frameRenderer.EnabledField.SyncWithVariable("overlayBg");

            var zoomIn = overlayVisual.AttachComponent<StaticAudioClip>();
            zoomIn.URL.Value = new Uri(zoomInSound.Value);
            zoomIn.URL.SyncWithVariable("zoomInSoundUri");
            var zoomOut = overlayVisual.AttachComponent<StaticAudioClip>();
            zoomOut.URL.Value = new Uri(zoomOutSound.Value);
            zoomOut.URL.SyncWithVariable("zoomOutSoundUri");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Userspace), "OnCommonUpdate")]
        public static void Update(Userspace __instance)
        {
            // TODO: Broken
            if (toggleZoom.Value && __instance.InputInterface.GetKeyDown(zoomKey.Value))
            {
                toggleState = !toggleState;
            }

            //Logger.LogInfo("Deez");
            var zoom = enabled.Value
                    && !__instance.LocalUser.HasActiveFocus() // Not focused in any field
                    && !Userspace.HasFocus // Not focused in userspace field
                    && (toggleState || __instance.InputInterface.GetKey(zoomKey.Value)); // Key pressed

            if (exposeZoomedInVariable.Value)
            {
                try
                {
                    var userRoot = __instance.Engine.WorldManager.FocusedWorld?.LocalUser?.Root?.Slot;
                    DynamicVariableWriteResult result = userRoot.WriteDynamicVariable<bool>("User/optizoom.zoomed_in", zoom);
                    if (result != 0) DynamicVariableHelper.CreateVariable<bool>(userRoot, "User/optizoom.zoomed_in", zoom, false);
                }
                catch { }
            } 

            if ((zoom && enableOverlay.Value) != overlayVisual.ActiveSelf)
            {
                overlayVisual.ActiveSelf = zoom;
                var soundUri = zoom ? zoomInSound.Value : zoomOutSound.Value;
                if (soundUri.IsNullOrWhiteSpace()) return;
                var clip = overlayVisual.GetComponent<StaticAudioClip>(a => a.URL.Value == new Uri(soundUri));
                if (clip == null) return;
                overlayVisual.PlayOneShot(clip, zoomVolume.Value, false, parent: false);
            }
        }
    }



    [HarmonyPatch(typeof(UserRoot), "get_DesktopFOV")]
    class Optizoom_Patch
    {
        static readonly Dictionary<UserRoot, UserRootFOVLerps> FOVLerps = [];

        public static void Postfix(UserRoot __instance, ref float __result, DesktopRenderSettings ____renderSettings)
        {
            if (!FOVLerps.TryGetValue(__instance, out UserRootFOVLerps lerp))
            {
                lerp = new UserRootFOVLerps(); // Needs one per UserRoot or else userspace and focused world fights
                FOVLerps.Add(__instance, lerp);
            }

            var zoom = enabled.Value
                    && !__instance.LocalUser.HasActiveFocus() // Not focused in any field
                    && !Userspace.HasFocus // Not focused in userspace field
                    && __instance.Engine.WorldManager.FocusedWorld == __instance.World // Focused in the same world as the UserRoot
                    && (toggleState || __instance.InputInterface.GetKey(zoomKey.Value)); // Key pressed

            float fovSetting = (____renderSettings != null) ? ____renderSettings.FieldOfView.Value : 60f;
            float target = zoom ? fovSetting - zoomFOV.Value : 0f;//__result;

            if (zoom && scrollZoom.Value)
            {
                var scrollDelta = -__instance.InputInterface.Mouse.NormalizedScrollWheelDelta.Value.Y;//.ScrollWheelDelta.Delta.y;

                lerp.scroll += scrollDelta * scrollZoomSpeed.Value;

                lerp.scroll = MathX.Clamp(lerp.scroll, -zoomFOV.Value, 179f - zoomFOV.Value); // Clamp to the available fov
                target -= lerp.scroll;

                /* Compensation is not complete yet
                lerp.scroll = MathX.Clamp(lerp.scroll, -1, 1);

                var remap = MathX.Remap11_01(lerp.scroll);
                remap *= remap;
                remap = MathX.Remap(remap, 0f, 1f, -ZoomFOV.Value, 179f - ZoomFOV.Value);

                target -= remap;
                */
            }
            else if (scrollZoom.Value && !MathX.Approximately(lerp.scroll, 0f, 0.001)) {
                lerp.scroll = 0f;
            }

            if (lerpZoom.Value)
            {
                lerp.currentLerp = MathX.SmoothDamp(lerp.currentLerp, target, ref lerp.lerpVelocity, zoomSpeed.Value, 179f, __instance.Time.Delta); // Funny lerp
                __result -= lerp.currentLerp;
            } else
            {
                __result -= target;
            }

            __result = MathX.FilterInvalid(__result, 60f); // fallback to 60 fov if invalid
            __result = MathX.Clamp(__result, 1f, 179f);
        }
    }

    class UserRootFOVLerps
    {
        public float currentLerp = 0f;
        public float lerpVelocity = 0f;
        public float scroll = 0f;
    }


    public static bool TryWriteDynamicValue<T>(Slot root, string name, T value)
    {
        DynamicVariableHelper.ParsePath(name, out string spaceName, out string text);

        if (string.IsNullOrEmpty(text)) return false;

        DynamicVariableSpace dynamicVariableSpace = root.FindSpace(spaceName);
        if (dynamicVariableSpace == null) return false;
        return dynamicVariableSpace.TryWriteValue(text, value) == DynamicVariableWriteResult.Success;
    }
    public static bool TryReadDynamicValue<T>(Slot root, string name, out T value)
    {
        value = Coder<T>.Default;
        DynamicVariableHelper.ParsePath(name, out string spaceName, out string text);

        if (string.IsNullOrEmpty(text)) return false;

        DynamicVariableSpace dynamicVariableSpace = root.FindSpace(spaceName);
        if (dynamicVariableSpace == null) return false;
        return dynamicVariableSpace.TryReadValue(text, out value);
    }
}