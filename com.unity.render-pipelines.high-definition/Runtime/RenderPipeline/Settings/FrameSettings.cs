using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum LitShaderMode
    {
        Forward,
        Deferred
    }

    public enum FrameSettingsField
    {
        //rendering pass from 20 to 39
        [FrameSettingsField(0)]
        TransparentPrepass = 0,
        [FrameSettingsField(0)]
        TransparentPostpass = 1,
        [FrameSettingsField(0)]
        MotionVectors = 2,
        [FrameSettingsField(0)]
        ObjectMotionVectors = 3,
        [FrameSettingsField(0)]
        Decals = 4,
        [FrameSettingsField(0)]
        RoughRefraction = 5,
        [FrameSettingsField(0)]
        Distortion = 6,
        [FrameSettingsField(0)]
        Postprocess = 7,

        //lighting settings from 0 to 19 (grouped in same scope in DebugMenu/Inspector)
        [FrameSettingsField(0)]
        Shadow = 20,
        [FrameSettingsField(0)]
        ContactShadows = 21,
        [FrameSettingsField(0)]
        ShadowMask = 22,
        [FrameSettingsField(0)]
        SSR = 23,
        [FrameSettingsField(0)]
        SSAO = 24,
        [FrameSettingsField(0)]
        SubsurfaceScattering = 25,
        [FrameSettingsField(0)]
        Transmission = 26,
        [FrameSettingsField(0)]
        AtmosphericScattering = 27,
        [FrameSettingsField(0)]
        Volumetrics = 28,
        [FrameSettingsField(0)]
        ReprojectionForVolumetrics = 29,
        [FrameSettingsField(0)]
        LightLayers = 30,
        [FrameSettingsField(0)]
        MSAA = 31,
        
        //rendering settings from 40 to 59
        [FrameSettingsField(1, type: FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup, targetType: typeof(LitShaderMode))]
        ShaderLitMode = 40,
        [FrameSettingsField(1)]
        DepthPrepassWithDeferredRendering = 41,
        [FrameSettingsField(1)]
        OpaqueObjects = 42,
        [FrameSettingsField(1)]
        TransparentObjects = 43,
        [FrameSettingsField(1)]
        RealtimePlanarReflection = 44,

        //async settings from 60 to 79
        [FrameSettingsField(2)]
        AsyncCompute = 60,
        [FrameSettingsField(2)]
        LightListAsync = 61,
        [FrameSettingsField(2)]
        SSRAsync = 62,
        [FrameSettingsField(2)]
        SSAOAsync = 63,
        [FrameSettingsField(2)]
        ContactShadowsAsync = 64,
        [FrameSettingsField(2)]
        VolumeVoxelizationsAsync = 65,

        //from 80 to 119 : space for new scopes

        //lightLoop settings from 120 to 127
        [FrameSettingsField(3)]
        FptlForForwardOpaque = 120,
        [FrameSettingsField(3)]
        BigTilePrepass = 121,
        [FrameSettingsField(3)]
        ComputeLightEvaluation = 122,
        [FrameSettingsField(3)]
        ComputeLightVariants = 123,
        [FrameSettingsField(3)]
        ComputeMaterialVariants = 124,
        [FrameSettingsField(3)]
        TileAndCluster = 125,
        Reflection = 126, //set by engine, not for DebugMenu/Inspector

        //only 128 booleans saved. For more, change the CheapBitArray used
    }

    [Serializable]
    [System.Diagnostics.DebuggerDisplay("FrameSettings overriding {mask.humanizedData}")]
    public struct FrameSettingsOverrideMask
    {
        [SerializeField]
        public BitArray128 mask;
    }
    
    // The settings here are per frame settings.
    // Each camera must have its own per frame settings
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("FrameSettings overriding {bitDatas.humanizedData}")]
    public partial struct FrameSettings
    {
        public static readonly FrameSettings defaultCamera = new FrameSettings()
        {
            bitDatas = new BitArray128(new uint[] {
                (uint)FrameSettingsField.Shadow,
                (uint)FrameSettingsField.ContactShadows,
                (uint)FrameSettingsField.ShadowMask,
                (uint)FrameSettingsField.SSAO,
                (uint)FrameSettingsField.SubsurfaceScattering,
                (uint)FrameSettingsField.Transmission,   // Caution: this is only for debug, it doesn't save the cost of Transmission execution
                (uint)FrameSettingsField.AtmosphericScattering,
                (uint)FrameSettingsField.Volumetrics,
                (uint)FrameSettingsField.ReprojectionForVolumetrics,
                (uint)FrameSettingsField.LightLayers,
                (uint)FrameSettingsField.ShaderLitMode, //deffered ; enum with only two value saved as a bool
                (uint)FrameSettingsField.TransparentPrepass,
                (uint)FrameSettingsField.TransparentPostpass,
                (uint)FrameSettingsField.MotionVectors, // Enable/disable whole motion vectors pass (Camera + Object).
                (uint)FrameSettingsField.ObjectMotionVectors,
                (uint)FrameSettingsField.Decals,
                (uint)FrameSettingsField.RoughRefraction, // Depends on DepthPyramid - If not enable, just do a copy of the scene color (?) - how to disable rough refraction ?
                (uint)FrameSettingsField.Distortion,
                (uint)FrameSettingsField.Postprocess,
                (uint)FrameSettingsField.OpaqueObjects,
                (uint)FrameSettingsField.TransparentObjects,
                (uint)FrameSettingsField.RealtimePlanarReflection,
                (uint)FrameSettingsField.AsyncCompute,
                (uint)FrameSettingsField.LightListAsync,
                (uint)FrameSettingsField.SSRAsync,
                (uint)FrameSettingsField.SSRAsync,
                (uint)FrameSettingsField.SSAOAsync,
                (uint)FrameSettingsField.ContactShadowsAsync,
                (uint)FrameSettingsField.VolumeVoxelizationsAsync,
                (uint)FrameSettingsField.TileAndCluster,
                (uint)FrameSettingsField.ComputeLightEvaluation,
                (uint)FrameSettingsField.ComputeLightVariants,
                (uint)FrameSettingsField.ComputeMaterialVariants,
                (uint)FrameSettingsField.FptlForForwardOpaque,
                (uint)FrameSettingsField.BigTilePrepass,
            })
        };
        public static readonly FrameSettings defaultRealtimeReflectionProbe = new FrameSettings()
        {
            bitDatas = new BitArray128(new uint[] {
                (uint)FrameSettingsField.Shadow,
                //(uint)FrameSettingsField.ContactShadow,
                //(uint)FrameSettingsField.ShadowMask,
                //(uint)FrameSettingsField.SSAO,
                (uint)FrameSettingsField.SubsurfaceScattering,
                (uint)FrameSettingsField.Transmission,   // Caution: this is only for debug, it doesn't save the cost of Transmission execution
                //(uint)FrameSettingsField.AtmosphericScaterring,
                (uint)FrameSettingsField.Volumetrics,
                (uint)FrameSettingsField.ReprojectionForVolumetrics,
                (uint)FrameSettingsField.LightLayers,
                (uint)FrameSettingsField.ShaderLitMode, //deffered ; enum with only two value saved as a bool
                (uint)FrameSettingsField.TransparentPrepass,
                (uint)FrameSettingsField.TransparentPostpass,
                (uint)FrameSettingsField.MotionVectors, // Enable/disable whole motion vectors pass (Camera + Object).
                (uint)FrameSettingsField.ObjectMotionVectors,
                (uint)FrameSettingsField.Decals,
                //(uint)FrameSettingsField.RoughRefraction, // Depends on DepthPyramid - If not enable, just do a copy of the scene color (?) - how to disable rough refraction ?
                //(uint)FrameSettingsField.Distortion,
                //(uint)FrameSettingsField.Postprocess,
                (uint)FrameSettingsField.OpaqueObjects,
                (uint)FrameSettingsField.TransparentObjects,
                (uint)FrameSettingsField.RealtimePlanarReflection,
                (uint)FrameSettingsField.AsyncCompute,
                (uint)FrameSettingsField.LightListAsync,
                (uint)FrameSettingsField.SSRAsync,
                (uint)FrameSettingsField.SSRAsync,
                (uint)FrameSettingsField.SSAOAsync,
                (uint)FrameSettingsField.ContactShadowsAsync,
                (uint)FrameSettingsField.VolumeVoxelizationsAsync,
                (uint)FrameSettingsField.TileAndCluster,
                (uint)FrameSettingsField.ComputeLightEvaluation,
                (uint)FrameSettingsField.ComputeLightVariants,
                (uint)FrameSettingsField.ComputeMaterialVariants,
                (uint)FrameSettingsField.FptlForForwardOpaque,
                (uint)FrameSettingsField.BigTilePrepass,
            })
        };
        public static readonly FrameSettings defaultCustomOrBakeReflectionProbe = defaultCamera;

        [SerializeField]
        BitArray128 bitDatas;
        
        public LitShaderMode litShaderMode
        {
            get => bitDatas[(uint)FrameSettingsField.ShaderLitMode] ? LitShaderMode.Deferred : LitShaderMode.Forward;
            set => bitDatas[(uint)FrameSettingsField.ShaderLitMode] = value == LitShaderMode.Deferred;
        }

        public bool IsEnable(FrameSettingsField field) => bitDatas[(uint)field];
        public void SetEnable(FrameSettingsField field, bool value) => bitDatas[(uint)field] = value;
        
        public bool fptl => litShaderMode == LitShaderMode.Deferred || bitDatas[(int)FrameSettingsField.FptlForForwardOpaque];
        public float specularGlobalDimmer => bitDatas[(int)FrameSettingsField.Reflection] ? 1f : 0f;
        
        public bool BuildLightListRunsAsync() => SystemInfo.supportsAsyncCompute && bitDatas[(int)FrameSettingsField.AsyncCompute] && bitDatas[(int)FrameSettingsField.LightListAsync];
        public bool SSRRunsAsync() => SystemInfo.supportsAsyncCompute && bitDatas[(int)FrameSettingsField.AsyncCompute] && bitDatas[(int)FrameSettingsField.SSRAsync];
        public bool SSAORunsAsync() => SystemInfo.supportsAsyncCompute && bitDatas[(int)FrameSettingsField.AsyncCompute] && bitDatas[(int)FrameSettingsField.SSAOAsync];
        public bool ContactShadowsRunAsync() => SystemInfo.supportsAsyncCompute && bitDatas[(int)FrameSettingsField.AsyncCompute] && bitDatas[(int)FrameSettingsField.ContactShadowsAsync];
        public bool VolumeVoxelizationRunsAsync() => SystemInfo.supportsAsyncCompute && bitDatas[(int)FrameSettingsField.AsyncCompute] && bitDatas[(int)FrameSettingsField.VolumeVoxelizationsAsync];

        public static void Override(ref FrameSettings overridedFrameSettings, FrameSettings overridingFrameSettings, FrameSettingsOverrideMask frameSettingsOverideMask)
        {
            //quick override of all booleans
            overridedFrameSettings.bitDatas = (overridingFrameSettings.bitDatas & frameSettingsOverideMask.mask) | (~frameSettingsOverideMask.mask & overridedFrameSettings.bitDatas);

            //override remaining values here if needed
        }
        
        public static void Sanitize(ref FrameSettings sanitazedFrameSettings, Camera camera, RenderPipelineSettings renderPipelineSettings)
        {
            bool reflection = camera.cameraType == CameraType.Reflection;
            bool preview = HDUtils.IsRegularPreviewCamera(camera);
            bool sceneViewFog = CoreUtils.IsSceneViewFogEnabled(camera);
            bool stereo = camera.stereoEnabled;

            // When rendering reflection probe we disable specular as it is view dependent
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.Reflection] = !reflection;

            // We have to fall back to forward-only rendering when scene view is using wireframe rendering mode
            // as rendering everything in wireframe + deferred do not play well together
            if (GL.wireframe || stereo) //force forward mode for wireframe
            {
                // Stereo deferred rendering still has the following problems:
                // VR TODO: Dispatch tile light-list compute per-eye
                // VR TODO: Update compute lighting shaders for stereo
                sanitazedFrameSettings.litShaderMode = LitShaderMode.Forward;
            }
            else
            {
                switch (renderPipelineSettings.supportedLitShaderMode)
                {
                    case RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly:
                        sanitazedFrameSettings.litShaderMode = LitShaderMode.Forward;
                        break;
                    case RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly:
                        sanitazedFrameSettings.litShaderMode = LitShaderMode.Deferred;
                        break;
                    case RenderPipelineSettings.SupportedLitShaderMode.Both:
                        //nothing to do: keep previous value
                        break;
                }
            }

            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.Shadow] &= !preview;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.ShadowMask] &= renderPipelineSettings.supportShadowMask && !preview;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.ContactShadows] &= !preview;

            //MSAA only supported in forward
            // TODO: The work will be implemented piecemeal to support all passes
            bool msaa = sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.MSAA] &= renderPipelineSettings.supportMSAA && sanitazedFrameSettings.litShaderMode == LitShaderMode.Forward;

            // VR TODO: The work will be implemented piecemeal to support all passes
            // No recursive reflections
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.SSR] &= !reflection && renderPipelineSettings.supportSSR && !msaa && !preview && stereo;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.SSAO] &= renderPipelineSettings.supportSSAO && !preview;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.SubsurfaceScattering] &= !reflection && renderPipelineSettings.supportSubsurfaceScattering;

            // We must take care of the scene view fog flags in the editor
            bool atmosphericScattering = sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.AtmosphericScattering] &= !sceneViewFog && !preview;

            // Volumetric are disabled if there is no atmospheric scattering
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.Volumetrics] &= renderPipelineSettings.supportVolumetrics && atmosphericScattering; //&& !preview induced by atmospheric scattering
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.ReprojectionForVolumetrics] &= !preview;

            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.LightLayers] &= renderPipelineSettings.supportLightLayers && !preview;

            // Planar and real time cubemap doesn't need post process and render in FP16
            bool postprocess = sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.Postprocess] &= !reflection && !preview;

            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.TransparentPrepass] &= renderPipelineSettings.supportTransparentDepthPrepass && !preview;

            // VR TODO: The work will be implemented piecemeal to support all passes
            // VR TODO: check why '=' and not '&=' and if we can merge these lines
            bool motionVector;
            if (stereo)
                motionVector = sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.MotionVectors] = postprocess && !msaa && !preview;
            else
                motionVector = sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.MotionVectors] &= !reflection && renderPipelineSettings.supportMotionVectors && !preview;

            // Object motion vector are disabled if motion vector are disabled
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.ObjectMotionVectors] &= motionVector && !preview;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.Decals] &= renderPipelineSettings.supportDecals && !preview;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.TransparentPostpass] &= renderPipelineSettings.supportTransparentDepthPostpass && !preview;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.Distortion] &= !reflection && renderPipelineSettings.supportDistortion && !msaa && !preview;

            bool async = sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.AsyncCompute] &= SystemInfo.supportsAsyncCompute;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.LightListAsync] &= async;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.SSRAsync] &= async;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.SSAOAsync] &= async;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.ContactShadowsAsync] &= async;
            sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.VolumeVoxelizationsAsync] &= async;

            // Deferred opaque are always using Fptl. Forward opaque can use Fptl or Cluster, transparent use cluster.
            // When MSAA is enabled we disable Fptl as it become expensive compare to cluster
            // In HD, MSAA is only supported for forward only rendering, no MSAA in deferred mode (for code complexity reasons)
            // Disable FPTL for stereo for now
            bool fptlForwardOpaque = sanitazedFrameSettings.bitDatas[(int)FrameSettingsField.FptlForForwardOpaque] &= !msaa && !XRGraphics.enabled;
        }
        
        public static void AggregateFrameSettings(ref FrameSettings aggregatedFrameSettings, Camera camera, HDAdditionalCameraData additionalData, HDRenderPipelineAsset hdrpAsset)
        {
            aggregatedFrameSettings = hdrpAsset.GetDefaultFrameSettings(additionalData.defaultFrameSettings);
            if (additionalData && additionalData.customRenderingSettings)
                Override(ref aggregatedFrameSettings, additionalData.renderingPathCustomFrameSettings, additionalData.renderingPathCustomOverrideFrameSettings);
            Sanitize(ref aggregatedFrameSettings, camera, hdrpAsset.GetRenderPipelineSettings());
        }
        
        public static bool operator ==(FrameSettings a, FrameSettings b) => a.bitDatas == b.bitDatas;
        public static bool operator !=(FrameSettings a, FrameSettings b) => a.bitDatas != b.bitDatas;
        public override bool Equals(object obj) => (obj is FrameSettings) && bitDatas.Equals((FrameSettings)obj);
        public override int GetHashCode() => -1690259335 + bitDatas.GetHashCode();
    }
}
