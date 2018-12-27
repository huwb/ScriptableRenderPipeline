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
                CED.FoldoutGroup(renderingPassesHeaderContent, Expandable.RenderingPasses, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(200, (serialized, owner) => Drawer_SectionRenderingPasses(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(renderingSettingsHeaderContent, Expandable.RenderingSettings, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(250, (serialized, owner) => Drawer_SectionRenderingSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(lightSettingsHeaderContent, Expandable.LightingSettings, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(250, (serialized, owner) => Drawer_SectionLightingSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(asyncComputeSettingsHeaderContent, Expandable.AsynComputeSettings, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(250, (serialized, owner) => Drawer_SectionAsyncComputeSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(lightLoopSettingsHeaderContent, Expandable.LightLoop, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(250, (serialized, owner) => Drawer_SectionLightLoopSettings(serialized, owner, withOverride))
                    )
                );

        internal static HDRenderPipelineAsset GetHDRPAssetFor(Editor owner)
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

        internal static FrameSettings GetDefaultFrameSettingsFor(Editor owner)
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

        static void Drawer_SectionRenderingPasses(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).renderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableSettingsArea area = new OverridableSettingsArea(8);
            area.Add(serialized.transparentPrepass, transparentPrepassContent, () => serialized.overridesTransparentPrepass, a => serialized.overridesTransparentPrepass = a, defaultValue: defaultFrameSettings.transparentPrepass);
            area.Add(serialized.transparentPostpass, transparentPostpassContent, () => serialized.overridesTransparentPostpass, a => serialized.overridesTransparentPostpass = a, defaultValue: defaultFrameSettings.transparentPostpass);
            area.Add(serialized.motionVectors, motionVectorContent, () => serialized.overridesMotionVectors, a => serialized.overridesMotionVectors = a, () => hdrpSettings.supportMotionVectors, defaultValue: defaultFrameSettings.motionVectors);
            area.Add(serialized.objectMotionVectors, objectMotionVectorsContent, () => serialized.overridesObjectMotionVectors, a => serialized.overridesObjectMotionVectors = a, () => hdrpSettings.supportMotionVectors && serialized.motionVectors.boolValue, defaultValue: defaultFrameSettings.objectMotionVectors, indent: 1);
            area.Add(serialized.decals, decalsContent, () => serialized.overridesDecals, a => serialized.overridesDecals = a, () => hdrpSettings.supportDecals, defaultValue: defaultFrameSettings.decals);
            area.Add(serialized.roughRefraction, roughRefractionContent, () => serialized.overridesRoughRefraction, a => serialized.overridesRoughRefraction = a, defaultValue: defaultFrameSettings.roughRefraction);
            area.Add(serialized.distortion, distortionContent, () => serialized.overridesDistortion, a => serialized.overridesDistortion = a, () => hdrpSettings.supportDistortion, defaultValue: defaultFrameSettings.distortion);
            area.Add(serialized.postprocess, postprocessContent, () => serialized.overridesPostprocess, a => serialized.overridesPostprocess = a, defaultValue: defaultFrameSettings.postprocess);
            area.Draw(withOverride);
        }

        static void Drawer_SectionRenderingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).renderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableSettingsArea area = new OverridableSettingsArea(6, defaultFrameSettings);
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

            area.Add(serialized.litShaderMode, litShaderModeContent, () => serialized.overridesShaderLitMode, a => serialized.overridesShaderLitMode = a,
                () => !GL.wireframe && hdrpSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.Both,
                defaultValue: defaultShaderLitMode);

            bool assetAllowMSAA = hdrpSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly && hdrpSettings.supportMSAA;
            bool frameSettingsAllowMSAA = serialized.litShaderMode.enumValueIndex == (int)LitShaderMode.Forward && serialized.overridesShaderLitMode || !serialized.overridesShaderLitMode && defaultShaderLitMode == LitShaderMode.Forward;
            area.Add(serialized.msaa, msaaContent, () => serialized.overridesMSAA, a => serialized.overridesMSAA = a,
                () => !GL.wireframe
                && assetAllowMSAA && frameSettingsAllowMSAA,
                defaultValue: defaultFrameSettings.msaa && hdrpSettings.supportMSAA && !GL.wireframe && (hdrpSettings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly) != 0 && (serialized.overridesShaderLitMode && serialized.litShaderMode.enumValueIndex == (int)LitShaderMode.Forward || !serialized.overridesShaderLitMode && defaultFrameSettings.litShaderMode == (int)LitShaderMode.Forward));
            area.Add(serialized.depthPrepassWithDeferredRendering, depthPrepassWithDeferredRenderingContent, () => serialized.overridesDepthPrepassWithDeferredRendering, a => serialized.overridesDepthPrepassWithDeferredRendering = a,
                () => (defaultFrameSettings.litShaderMode == LitShaderMode.Deferred && !serialized.overridesShaderLitMode || serialized.overridesShaderLitMode && serialized.litShaderMode.enumValueIndex == (int)LitShaderMode.Deferred) && (hdrpSettings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly) != 0,
                defaultValue: defaultFrameSettings.depthPrepassWithDeferredRendering && (hdrpSettings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly) != 0 && serialized.litShaderMode.enumValueIndex == (int)LitShaderMode.Deferred);
            area.Add(serialized.opaqueObjects, opaqueObjectsContent, () => serialized.overridesOpaqueObjects, a => serialized.overridesOpaqueObjects = a, defaultValue: defaultFrameSettings.opaqueObjects);
            area.Add(serialized.transparentObjects, transparentObjectsContent, () => serialized.overridesTransparentObjects, a => serialized.overridesTransparentObjects = a, defaultValue: defaultFrameSettings.transparentObjects);
            area.Add(serialized.realtimePlanarReflection, realtimePlanarReflectionContent, () => serialized.overridesRealtimePlanarReflection, a => serialized.overridesRealtimePlanarReflection = a, defaultValue: defaultFrameSettings.realtimePlanarReflection);
            area.Draw(withOverride);
        }

        static void Drawer_SectionLightingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).renderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableSettingsArea area = new OverridableSettingsArea(10);
            area.Add(serialized.shadow, shadowContent, () => serialized.overridesShadow, a => serialized.overridesShadow = a, defaultValue: defaultFrameSettings.shadow);
            area.Add(serialized.contactShadow, contactShadowContent, () => serialized.overridesContactShadow, a => serialized.overridesContactShadow = a, defaultValue: defaultFrameSettings.contactShadows);
            area.Add(serialized.shadowMask, shadowMaskContent, () => serialized.overridesShadowMask, a => serialized.overridesShadowMask = a, () => hdrpSettings.supportShadowMask, defaultValue: defaultFrameSettings.shadowMask);
            area.Add(serialized.ssr, ssrContent, () => serialized.overridesSSR, a => serialized.overridesSSR = a, () => hdrpSettings.supportSSR, defaultValue: defaultFrameSettings.ssr);
            area.Add(serialized.ssao, ssaoContent, () => serialized.overridesSSAO, a => serialized.overridesSSAO = a, () => hdrpSettings.supportSSAO, defaultValue: defaultFrameSettings.ssao);
            area.Add(serialized.subsurfaceScattering, subsurfaceScatteringContent, () => serialized.overridesSubsurfaceScattering, a => serialized.overridesSubsurfaceScattering = a, () => hdrpSettings.supportSubsurfaceScattering, defaultValue: defaultFrameSettings.subsurfaceScattering);
            area.Add(serialized.transmission, transmissionContent, () => serialized.overridesTransmission, a => serialized.overridesTransmission = a, defaultValue: defaultFrameSettings.transmission);
            area.Add(serialized.atmosphericScattering, atmosphericScatteringContent, () => serialized.overridesAtmosphericScaterring, a => serialized.overridesAtmosphericScaterring = a, defaultValue: defaultFrameSettings.atmosphericScattering);
            area.Add(serialized.volumetrics, volumetricContent, () => serialized.overridesVolumetrics, a => serialized.overridesVolumetrics = a, () => hdrpSettings.supportVolumetrics && serialized.atmosphericScattering.boolValue, defaultValue: defaultFrameSettings.atmosphericScattering, indent: 1);
            area.Add(serialized.reprojectionForVolumetrics, reprojectionForVolumetricsContent, () => serialized.overridesProjectionForVolumetrics, a => serialized.overridesProjectionForVolumetrics = a, () => hdrpSettings.supportVolumetrics && serialized.atmosphericScattering.boolValue, defaultValue: defaultFrameSettings.volumetrics, indent: 1);
            area.Add(serialized.lightLayers, lightLayerContent, () => serialized.overridesLightLayers, a => serialized.overridesLightLayers = a, () => hdrpSettings.supportLightLayers, defaultValue: defaultFrameSettings.lightLayers);
            area.Draw(withOverride);
        }

        static void Drawer_SectionAsyncComputeSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            OverridableSettingsArea area = new OverridableSettingsArea(4);
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            area.Add(serialized.asyncCompute, asyncComputeContent, () => serialized.overridesAsyncCompute, a => serialized.overridesAsyncCompute = a, defaultValue: defaultFrameSettings.asyncCompute);
            area.Add(serialized.buildLightListAsync, lightListAsyncContent, () => serialized.overridesLightListAsync, a => serialized.overridesLightListAsync = a, () => serialized.asyncCompute.boolValue, defaultValue: defaultFrameSettings.lightListAsync, indent: 1);
            area.Add(serialized.ssrAsync, SSRAsyncContent, () => serialized.overridesSSRAsync, a => serialized.overridesSSRAsync = a, () => serialized.asyncCompute.boolValue, defaultValue: defaultFrameSettings.ssrAsync, indent: 1);
            area.Add(serialized.ssaoAsync, SSAOAsyncContent, () => serialized.overridesSSAOAsync, a => serialized.overridesSSAOAsync = a, () => serialized.asyncCompute.boolValue, defaultValue: defaultFrameSettings.ssaoAsync, indent: 1);
            area.Add(serialized.contactShadowsAsync, contactShadowsAsyncContent, () => serialized.overridesContactShadowsAsync, a => serialized.overridesContactShadowsAsync = a, () => serialized.asyncCompute.boolValue, defaultValue: defaultFrameSettings.contactShadowsAsync, indent: 1);
            area.Add(serialized.volumeVoxelizationAsync, volumeVoxelizationAsyncContent, () => serialized.overridesVolumeVoxelizationAsync, a => serialized.overridesVolumeVoxelizationAsync = a, () => serialized.asyncCompute.boolValue, defaultValue: defaultFrameSettings.volumeVoxelizationAsync, indent: 1);
            area.Draw(withOverride);
        }

        static void Drawer_SectionLightLoopSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            //disable temporarily as FrameSettings are not supported for Baked probe at the moment
            using (new EditorGUI.DisabledScope((owner is IHDProbeEditor) && (owner as IHDProbeEditor).GetTarget(owner.target).mode != ProbeSettings.Mode.Realtime || (owner is HDRenderPipelineEditor) && HDRenderPipelineUI.selectedFrameSettings == HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection))
            {
                //RenderPipelineSettings hdrpSettings = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineSettings;
                OverridableSettingsArea area = new OverridableSettingsArea(6);
                FrameSettings defaultFrameSettings = FrameSettingsUI.GetDefaultFrameSettingsFor(owner);

                // Uncomment if you re-enable LIGHTLOOP_SINGLE_PASS multi_compile in lit*.shader
                //area.Add(p.enableTileAndCluster, tileAndClusterContent, a => p.overridesTileAndCluster = a, () => p.overridesTileAndCluster);
                //and add indent:1 or indent:2 regarding indentation you want
                
                using (new EditorGUI.DisabledScope(!serialized.tileAndCluster.boolValue))
                {
                    area.Add(serialized.fptlForForwardOpaque, fptlForForwardOpaqueContent, () => serialized.overridesFptlForForwardOpaque, a => serialized.overridesFptlForForwardOpaque = a, defaultValue: defaultFrameSettings.fptlForForwardOpaque);
                    area.Add(serialized.bigTilePrepass, bigTilePrepassContent, () => serialized.overridesBigTilePrepass, a => serialized.overridesBigTilePrepass = a, defaultValue: defaultFrameSettings.bigTilePrepass);
                    area.Add(serialized.computeLightEvaluation, computeLightEvaluationContent, () => serialized.overridesComputeLightEvaluation, a => serialized.overridesComputeLightEvaluation = a, defaultValue: defaultFrameSettings.computeLightEvaluation);
                    using (new EditorGUI.DisabledScope(!serialized.computeLightEvaluation.boolValue))
                    {
                        area.Add(serialized.computeLightVariants, computeLightVariantsContent, () => serialized.overridesComputeLightVariants, a => serialized.overridesComputeLightVariants = a, defaultValue: defaultFrameSettings.computeLightVariants, indent: 1);
                        area.Add(serialized.computeMaterialVariants, computeMaterialVariantsContent, () => serialized.overridesComputeMaterialVariants, a => serialized.overridesComputeMaterialVariants = a, defaultValue: defaultFrameSettings.computeMaterialVariants, indent: 1);
                    }
                }

                area.Draw(withOverride);
            }
        }
    }
}
