using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedFrameSettings
    {
        public SerializedProperty rootData;
        public SerializedProperty rootOveride;
        
        public LitShaderMode litShaderMode
        {
            get => IsEnable(FrameSettingsField.LitShaderMode) ? LitShaderMode.Deferred : LitShaderMode.Forward;
            set => SetEnable(FrameSettingsField.LitShaderMode, value == LitShaderMode.Deferred);
        }

        public bool IsEnable(FrameSettingsField field) => rootData.GetBitArrayAt((uint)field);
        public void SetEnable(FrameSettingsField field, bool value) => rootData.SetBitArrayAt((uint)field, value);

        public bool GetOverrides(FrameSettingsField field) => rootOveride.GetBitArrayAt((uint)field);
        public void SetOverrides(FrameSettingsField field, bool value) => rootOveride.SetBitArrayAt((uint)field, value);

        public SerializedFrameSettings(SerializedProperty rootData, SerializedProperty rootOverride)
        {
            this.rootData = rootData.FindPropertyRelative("bitDatas");
            this.rootOveride = rootOverride.FindPropertyRelative("mask");
        }
    }
}
