using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedFrameSettings>;

    partial class FrameSettingsUI
    {
        enum Expandable
        {
            RenderingPasses = 1 << 0,
            RenderingSettings = 1 << 1,
            LightingSettings = 1 << 2,
            AsynComputeSettings = 1 << 3,
            LightLoop = 1 << 4,
        }

        readonly static ExpandedState<Expandable, FrameSettings> k_ExpandedState = new ExpandedState<Expandable, FrameSettings>(~(-1), "HDRP");
        
        internal static CED.IDrawer Inspector(bool withOverride = true) => CED.Group(
                CED.Group((serialized, owner) =>
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(FrameSettingsUI.frameSettingsHeaderContent, EditorStyles.boldLabel);
                }),
                InspectorInnerbox(withOverride),
                CED.Group((serialized, owner) => EditorGUILayout.EndVertical())
                );

        //separated to add enum popup on default frame settings
        internal static CED.IDrawer InspectorInnerbox(bool withOverride = true) => CED.Group(
                CED.FoldoutGroup(renderingSettingsHeaderContent, Expandable.RenderingPasses, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(180, (serialized, owner) => Drawer_SectionRenderingSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(lightSettingsHeaderContent, Expandable.LightingSettings, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(180, (serialized, owner) => Drawer_SectionLightingSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(asyncComputeSettingsHeaderContent, Expandable.AsynComputeSettings, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(180, (serialized, owner) => Drawer_SectionAsyncComputeSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(lightLoopSettingsHeaderContent, Expandable.LightLoop, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(180, (serialized, owner) => Drawer_SectionLightLoopSettings(serialized, owner, withOverride))
                    )
                );

        static HDRenderPipelineAsset GetHDRPAssetFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset;
            if (owner is HDRenderPipelineEditor)
            {
                // When drawing the inspector of a selected HDRPAsset in Project windows, access HDRP by owner drawing itself
                hdrpAsset = (owner as HDRenderPipelineEditor).target as HDRenderPipelineAsset;
            }
            else
            {
                // Else rely on GraphicsSettings are you should be in hdrp and owner could be probe or camera.
                hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            }
            return hdrpAsset;
        }

        static FrameSettings GetDefaultFrameSettingsFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset = GetHDRPAssetFor(owner);
            if (owner is IHDProbeEditor)
            {
                if ((owner as IHDProbeEditor).GetTarget(owner.target).mode == ProbeSettings.Mode.Realtime)
                    return hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.RealtimeReflection);
                else
                    return hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.CustomOrBakedReflection);
            }
            return hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
        }

        static void Drawer_SectionRenderingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).renderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableFrameSettingsArea area = new OverridableFrameSettingsArea(14, defaultFrameSettings, serialized);
            LitShaderMode defaultShaderLitMode;
            switch (hdrpSettings.supportedLitShaderMode)
            {
                case RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly:
                    defaultShaderLitMode = LitShaderMode.Forward;
                    break;
                case RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly:
                    defaultShaderLitMode = LitShaderMode.Deferred;
                    break;
                case RenderPipelineSettings.SupportedLitShaderMode.Both:
                    defaultShaderLitMode = defaultFrameSettings.litShaderMode;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException("Unknown ShaderLitMode");
            }

            area.Add(FrameSettingsField.LitShaderMode,
                overrideable: () => !GL.wireframe && hdrpSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.Both,
                overridedDefaultValue: defaultShaderLitMode);

            bool hdrpAssetSupportForward = hdrpSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly;
            bool hdrpAssetSupportDeferred = hdrpSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly;
            bool frameSettingsOverrideToForward = serialized.GetOverrides(FrameSettingsField.LitShaderMode) && serialized.litShaderMode == LitShaderMode.Forward;
            bool frameSettingsOverrideToDeferred = serialized.GetOverrides(FrameSettingsField.LitShaderMode) && serialized.litShaderMode == LitShaderMode.Deferred;
            bool defaultForwardUsed = !serialized.GetOverrides(FrameSettingsField.LitShaderMode) && defaultShaderLitMode == LitShaderMode.Forward;
            bool defaultDefferedUsed = !serialized.GetOverrides(FrameSettingsField.LitShaderMode) && defaultShaderLitMode == LitShaderMode.Deferred;
            bool msaaEnablable = !GL.wireframe && hdrpAssetSupportForward && hdrpSettings.supportMSAA && (frameSettingsOverrideToForward || defaultForwardUsed);
            bool depthPrepassEnablable = hdrpAssetSupportDeferred && (defaultDefferedUsed || frameSettingsOverrideToDeferred);
            area.Add(FrameSettingsField.MSAA,
                overrideable: () => msaaEnablable,
                overridedDefaultValue: msaaEnablable && defaultFrameSettings.IsEnable(FrameSettingsField.MSAA));
            area.Add(FrameSettingsField.DepthPrepassWithDeferredRendering,
                overrideable: () => depthPrepassEnablable,
                overridedDefaultValue: depthPrepassEnablable && defaultFrameSettings.IsEnable(FrameSettingsField.DepthPrepassWithDeferredRendering));

            area.Add(FrameSettingsField.OpaqueObjects);
            area.Add(FrameSettingsField.TransparentObjects);
            area.Add(FrameSettingsField.RealtimePlanarReflection);

            area.Add(FrameSettingsField.TransparentPrepass);
            area.Add(FrameSettingsField.TransparentPostpass);
            area.Add(FrameSettingsField.MotionVectors, overrideable: () => hdrpSettings.supportMotionVectors);
            area.Add(FrameSettingsField.ObjectMotionVectors, overrideable: () => hdrpSettings.supportMotionVectors);
            area.Add(FrameSettingsField.Decals, overrideable: () => hdrpSettings.supportDecals);
            area.Add(FrameSettingsField.RoughRefraction);
            area.Add(FrameSettingsField.Distortion, overrideable: () => hdrpSettings.supportDistortion);
            area.Add(FrameSettingsField.Postprocess);
            area.Draw(withOverride);
        }

        static void Drawer_SectionLightingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).renderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableFrameSettingsArea area = new OverridableFrameSettingsArea(10, defaultFrameSettings, serialized);
            area.Add(FrameSettingsField.Shadow);
            area.Add(FrameSettingsField.ContactShadows);
            area.Add(FrameSettingsField.ShadowMask, overrideable: () => hdrpSettings.supportShadowMask);
            area.Add(FrameSettingsField.SSR, overrideable: () => hdrpSettings.supportSSR);
            area.Add(FrameSettingsField.SSAO, overrideable: () => hdrpSettings.supportSSAO);
            area.Add(FrameSettingsField.SubsurfaceScattering, overrideable: () => hdrpSettings.supportSubsurfaceScattering);
            area.Add(FrameSettingsField.Transmission);
            area.Add(FrameSettingsField.AtmosphericScattering);
            area.Add(FrameSettingsField.Volumetrics, overrideable: () => hdrpSettings.supportVolumetrics);
            area.Add(FrameSettingsField.ReprojectionForVolumetrics, overrideable: () => hdrpSettings.supportVolumetrics);
            area.Add(FrameSettingsField.LightLayers, overrideable: () => hdrpSettings.supportLightLayers);
            area.Draw(withOverride);
        }

        static void Drawer_SectionAsyncComputeSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableFrameSettingsArea area = new OverridableFrameSettingsArea(4, defaultFrameSettings, serialized);
            area.Add(FrameSettingsField.AsyncCompute);
            area.Add(FrameSettingsField.LightListAsync);
            area.Add(FrameSettingsField.SSRAsync);
            area.Add(FrameSettingsField.SSAOAsync);
            area.Add(FrameSettingsField.ContactShadowsAsync);
            area.Add(FrameSettingsField.VolumeVoxelizationsAsync);
            area.Draw(withOverride);
        }

        static void Drawer_SectionLightLoopSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableFrameSettingsArea area = new OverridableFrameSettingsArea(6, defaultFrameSettings, serialized);

            // Uncomment if you re-enable LIGHTLOOP_SINGLE_PASS multi_compile in lit*.shader
            //area.Add(FrameSettingsField.TileAndCluster);
            //and add dependencies in FrameSettingsField if needed

            area.Add(FrameSettingsField.FptlForForwardOpaque);
            area.Add(FrameSettingsField.BigTilePrepass);
            area.Add(FrameSettingsField.ComputeLightEvaluation);
            area.Add(FrameSettingsField.ComputeLightVariants);
            area.Add(FrameSettingsField.ComputeMaterialVariants);
            area.Draw(withOverride);
        }
    }
}
