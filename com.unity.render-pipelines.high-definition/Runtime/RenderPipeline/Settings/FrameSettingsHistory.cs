using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum FrameSettingsRenderType
    {
        Camera,
        CustomOrBakedReflection,
        RealtimeReflection
    }

    public struct FrameSettingsHistory : IDebugData
    {
        public FrameSettingsRenderType defaultType;
        public FrameSettings custom;
        public FrameSettingsOverrideMask customMask;
        public FrameSettings sanitazed;
        public FrameSettings debug;
        bool m_DebugMenuResetTriggered;
        int m_LitShaderModeEnumIndex;

        internal static Dictionary<Camera, FrameSettingsHistory> frameSettingsHistory = new Dictionary<Camera, FrameSettingsHistory>();

        public static void AggregateFrameSettings(ref FrameSettings aggregatedFrameSettings, Camera camera, HDAdditionalCameraData additionalData, HDRenderPipelineAsset hdrpAsset)
        {
            FrameSettingsHistory history = new FrameSettingsHistory();
            history.defaultType = additionalData ? additionalData.defaultFrameSettings : FrameSettingsRenderType.Camera;
            aggregatedFrameSettings = hdrpAsset.GetDefaultFrameSettings(history.defaultType);
            if (additionalData && additionalData.customRenderingSettings)
            {
                FrameSettings.Override(ref aggregatedFrameSettings, additionalData.renderingPathCustomFrameSettings, additionalData.renderingPathCustomFrameSettingsOverrideMask);
                history.customMask = additionalData.renderingPathCustomFrameSettingsOverrideMask;
            }
            history.custom = aggregatedFrameSettings;
            FrameSettings.Sanitize(ref aggregatedFrameSettings, camera, hdrpAsset.GetRenderPipelineSettings());

            bool dirty =
                history.sanitazed != aggregatedFrameSettings                // updated components/asset
                || !frameSettingsHistory.ContainsKey(camera)                // no history yet
                || frameSettingsHistory[camera].m_DebugMenuResetTriggered;  // reset requested by debug menu on previous frame

            history.sanitazed = aggregatedFrameSettings;

            if (dirty)
            {
                history.debug = history.sanitazed;
                switch (history.debug.litShaderMode)
                {
                    case LitShaderMode.Forward:
                        history.m_LitShaderModeEnumIndex = 0;
                        break;
                    case LitShaderMode.Deferred:
                        history.m_LitShaderModeEnumIndex = 1;
                        break;
                    default:
                        throw new ArgumentException("Unknown LitShaderMode");
                }
            }

            aggregatedFrameSettings = history.debug;
            frameSettingsHistory[camera] = history;
        }

        ref FrameSettingsHistory persistantFrameSettingsHistory
        {
            get
            {
                unsafe
                {
                    fixed (FrameSettingsHistory* pthis = &this)
                        return ref *pthis;
                }
            }
        }

        static void RegisterDebug(string menuName, FrameSettingsHistory frameSettings)
        {
            var persistant = frameSettings.persistantFrameSettingsHistory;
            List<DebugUI.Widget> widgets = new List<DebugUI.Widget>();
            widgets.AddRange(
            new DebugUI.Widget[]
            {
                new DebugUI.Foldout
                {
                    displayName = "Rendering Passes",
                    children =
                    {
                        new DebugUI.BoolField { displayName = "Enable Transparent Prepass", getter = () => persistant.debug.IsEnable(FrameSettingsField.TransparentPrepass), setter = value => persistant.debug.SetEnable(FrameSettingsField.TransparentPrepass, value) },
                        new DebugUI.BoolField { displayName = "Enable Transparent Postpass", getter = () => persistant.debug.IsEnable(FrameSettingsField.TransparentPostpass), setter = value => persistant.debug.SetEnable(FrameSettingsField.TransparentPostpass, value) },
                        new DebugUI.BoolField { displayName = "Enable Motion Vectors", getter = () => persistant.debug.IsEnable(FrameSettingsField.MotionVectors), setter = value => persistant.debug.SetEnable(FrameSettingsField.MotionVectors, value) },
                        new DebugUI.BoolField { displayName = "  Enable Object Motion Vectors", getter = () => persistant.debug.IsEnable(FrameSettingsField.ObjectMotionVectors), setter = value => persistant.debug.SetEnable(FrameSettingsField.ObjectMotionVectors, value) },
                        new DebugUI.BoolField { displayName = "Enable DBuffer", getter = () => persistant.debug.IsEnable(FrameSettingsField.Decals), setter = value => persistant.debug.SetEnable(FrameSettingsField.Decals, value) },
                        new DebugUI.BoolField { displayName = "Enable Rough Refraction", getter = () => persistant.debug.IsEnable(FrameSettingsField.RoughRefraction), setter = value => persistant.debug.SetEnable(FrameSettingsField.RoughRefraction, value) },
                        new DebugUI.BoolField { displayName = "Enable Distortion", getter = () => persistant.debug.IsEnable(FrameSettingsField.Distortion), setter = value => persistant.debug.SetEnable(FrameSettingsField.Distortion, value) },
                        new DebugUI.BoolField { displayName = "Enable Postprocess", getter = () => persistant.debug.IsEnable(FrameSettingsField.Postprocess), setter = value => persistant.debug.SetEnable(FrameSettingsField.Postprocess, value) },
                    }
                },
                new DebugUI.Foldout
                {
                    displayName = "Rendering Settings",
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Lit Shader Mode", getter = () => (int)persistant.debug.litShaderMode, setter = value => persistant.debug.litShaderMode = (LitShaderMode)value, autoEnum = typeof(LitShaderMode), getIndex = () => persistant.m_LitShaderModeEnumIndex, setIndex = value => persistant.m_LitShaderModeEnumIndex = value },
                        new DebugUI.BoolField { displayName = "Deferred Depth Prepass", getter = () => persistant.debug.IsEnable(FrameSettingsField.DepthPrepassWithDeferredRendering), setter = value => persistant.debug.SetEnable(FrameSettingsField.DepthPrepassWithDeferredRendering, value) },
                        new DebugUI.BoolField { displayName = "Enable Opaque Objects", getter = () => persistant.debug.IsEnable(FrameSettingsField.OpaqueObjects), setter = value => persistant.debug.SetEnable(FrameSettingsField.OpaqueObjects, value) },
                        new DebugUI.BoolField { displayName = "Enable Transparent Objects", getter = () => persistant.debug.IsEnable(FrameSettingsField.TransparentObjects), setter = value => persistant.debug.SetEnable(FrameSettingsField.TransparentObjects, value) },
                        new DebugUI.BoolField { displayName = "Enable Realtime Planar Reflection", getter = () => persistant.debug.IsEnable(FrameSettingsField.RealtimePlanarReflection), setter = value => persistant.debug.SetEnable(FrameSettingsField.RealtimePlanarReflection, value) },
                        new DebugUI.BoolField { displayName = "Enable MSAA", getter = () => persistant.debug.IsEnable(FrameSettingsField.MSAA), setter = value => persistant.debug.SetEnable(FrameSettingsField.MSAA, value) },
                    }
                },
                new DebugUI.Foldout
                {
                    displayName = "Lighting Settings",
                    children =
                    {
                        new DebugUI.BoolField { displayName = "Enable SSR", getter = () => persistant.debug.IsEnable(FrameSettingsField.SSR), setter = value => persistant.debug.SetEnable(FrameSettingsField.SSR, value) },
                        new DebugUI.BoolField { displayName = "Enable SSAO", getter = () => persistant.debug.IsEnable(FrameSettingsField.SSAO), setter = value => persistant.debug.SetEnable(FrameSettingsField.SSAO, value) },
                        new DebugUI.BoolField { displayName = "Enable SubsurfaceScattering", getter = () => persistant.debug.IsEnable(FrameSettingsField.SubsurfaceScattering), setter = value => persistant.debug.SetEnable(FrameSettingsField.SubsurfaceScattering, value) },
                        new DebugUI.BoolField { displayName = "Enable Transmission", getter = () => persistant.debug.IsEnable(FrameSettingsField.Transmission), setter = value => persistant.debug.SetEnable(FrameSettingsField.Transmission, value) },
                        new DebugUI.BoolField { displayName = "Enable Shadows", getter = () => persistant.debug.IsEnable(FrameSettingsField.Shadow), setter = value => persistant.debug.SetEnable(FrameSettingsField.Shadow, value) },
                        new DebugUI.BoolField { displayName = "Enable Contact Shadows", getter = () => persistant.debug.IsEnable(FrameSettingsField.ContactShadows), setter = value => persistant.debug.SetEnable(FrameSettingsField.ContactShadows, value) },
                        new DebugUI.BoolField { displayName = "Enable ShadowMask", getter = () => persistant.debug.IsEnable(FrameSettingsField.ShadowMask), setter = value => persistant.debug.SetEnable(FrameSettingsField.ShadowMask, value) },
                        new DebugUI.BoolField { displayName = "Enable Atmospheric Scattering", getter = () => persistant.debug.IsEnable(FrameSettingsField.AtmosphericScattering), setter = value => persistant.debug.SetEnable(FrameSettingsField.AtmosphericScattering, value) },
                        new DebugUI.BoolField { displayName = "Enable Volumetrics", getter = () => persistant.debug.IsEnable(FrameSettingsField.Volumetrics), setter = value => persistant.debug.SetEnable(FrameSettingsField.Volumetrics, value) },
                        new DebugUI.BoolField { displayName = "Enable Reprojection For Volumetrics", getter = () => persistant.debug.IsEnable(FrameSettingsField.ReprojectionForVolumetrics), setter = value => persistant.debug.SetEnable(FrameSettingsField.ReprojectionForVolumetrics, value) },
                        new DebugUI.BoolField { displayName = "Enable LightLayers", getter = () => persistant.debug.IsEnable(FrameSettingsField.LightLayers), setter = value => persistant.debug.SetEnable(FrameSettingsField.LightLayers, value) },
                    }
                },
                new DebugUI.Foldout
                {
                    displayName = "Async Compute Settings",
                    children =
                    {
                        new DebugUI.BoolField { displayName = "Enable Async Compute", getter = () => persistant.debug.IsEnable(FrameSettingsField.AsyncCompute), setter = value => persistant.debug.SetEnable(FrameSettingsField.AsyncCompute, value) },
                        new DebugUI.BoolField { displayName = "Run Build Light List Async", getter = () => persistant.debug.IsEnable(FrameSettingsField.LightListAsync), setter = value => persistant.debug.SetEnable(FrameSettingsField.LightListAsync, value) },
                        new DebugUI.BoolField { displayName = "Run SSR Async", getter = () => persistant.debug.IsEnable(FrameSettingsField.SSRAsync), setter = value => persistant.debug.SetEnable(FrameSettingsField.SSRAsync, value) },
                        new DebugUI.BoolField { displayName = "Run SSAO Async", getter = () => persistant.debug.IsEnable(FrameSettingsField.SSAOAsync), setter = value => persistant.debug.SetEnable(FrameSettingsField.SSAOAsync, value) },
                        new DebugUI.BoolField { displayName = "Run Contact Shadows Async", getter = () => persistant.debug.IsEnable(FrameSettingsField.ContactShadowsAsync), setter = value => persistant.debug.SetEnable(FrameSettingsField.ContactShadowsAsync, value) },
                        new DebugUI.BoolField { displayName = "Run Volume Voxelization Async", getter = () => persistant.debug.IsEnable(FrameSettingsField.VolumeVoxelizationsAsync), setter = value => persistant.debug.SetEnable(FrameSettingsField.VolumeVoxelizationsAsync, value) },
                    }
                },
                new DebugUI.Foldout
                {
                    displayName = "Light Loop Settings",
                    children =
                    {
                        // Uncomment if you re-enable LIGHTLOOP_SINGLE_PASS multi_compile in lit*.shader
                        //new DebugUI.BoolField { displayName = "Enable Tile/Cluster", getter = () => persistant.debug.enableTileAndCluster, setter = value => persistant.debug.enableTileAndCluster, value) },
                        new DebugUI.BoolField { displayName = "Enable Fptl for Forward Opaque", getter = () => persistant.debug.IsEnable(FrameSettingsField.FptlForForwardOpaque), setter = value => persistant.debug.SetEnable(FrameSettingsField.FptlForForwardOpaque, value) },
                        new DebugUI.BoolField { displayName = "Enable Big Tile", getter = () => persistant.debug.IsEnable(FrameSettingsField.BigTilePrepass), setter = value => persistant.debug.SetEnable(FrameSettingsField.BigTilePrepass, value) },
                        new DebugUI.BoolField { displayName = "Enable Compute Lighting", getter = () => persistant.debug.IsEnable(FrameSettingsField.ComputeLightEvaluation), setter = value => persistant.debug.SetEnable(FrameSettingsField.ComputeLightEvaluation, value) },
                        new DebugUI.BoolField { displayName = "Enable Light Classification", getter = () => persistant.debug.IsEnable(FrameSettingsField.ComputeLightVariants), setter = value => persistant.debug.SetEnable(FrameSettingsField.ComputeLightVariants, value) },
                        new DebugUI.BoolField { displayName = "Enable Material Classification", getter = () => persistant.debug.IsEnable(FrameSettingsField.ComputeMaterialVariants), setter = value => persistant.debug.SetEnable(FrameSettingsField.ComputeMaterialVariants, value) },
                    }
                }
            });

            var panel = DebugManager.instance.GetPanel(menuName, true);
            panel.children.Add(widgets.ToArray());
        }

        public static IDebugData RegisterDebug(Camera camera, HDAdditionalCameraData additionalCameraData)
        {
            HDRenderPipelineAsset hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            Assertions.Assert.IsNotNull(hdrpAsset);

            // complete frame settings history is required for displaying debug menu.
            // AggregateFrameSettings will finish the registration if it is not yet registered
            FrameSettings registering = new FrameSettings();
            AggregateFrameSettings(ref registering, camera, additionalCameraData, hdrpAsset);
            RegisterDebug(camera.name, frameSettingsHistory[camera]);
            return frameSettingsHistory[camera];
        }

        public static void UnRegisterDebug(Camera camera)
        {
            DebugManager.instance.RemovePanel(camera.name);
            frameSettingsHistory.Remove(camera);
        }

        public static IDebugData GetPersistantDebugData(Camera camera) => frameSettingsHistory[camera].persistantFrameSettingsHistory;

        void TriggerReset() => m_DebugMenuResetTriggered = true;
        Action IDebugData.GetReset() => TriggerReset;
    }
}
