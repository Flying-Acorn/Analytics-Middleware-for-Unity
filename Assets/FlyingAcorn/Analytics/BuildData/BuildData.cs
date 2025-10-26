using System;
using UnityEditor;
using UnityEngine;

namespace FlyingAcorn.Analytics.BuildData
{
    public class BuildData : ScriptableObject
    {
        public Constants.Store StoreName;
        public bool EnforceStoreOnBuild = false;
        public bool PreserveStoreAfterBuild = false;
        [HideInInspector] public string BuildNumber;
        [HideInInspector] public string LastBuildTime;
        [HideInInspector] public string ScriptingBackend;


#if UNITY_EDITOR

        private void OnEnable()
        {
            FillCurrentSettings();
        }

        public void EditorRefreshScriptingBackend(BuildTarget buildTarget)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);

            ScriptingBackend = PlayerSettings.GetScriptingBackend(group).ToString();
        }

        public void FillCurrentSettings()
        {
            LastBuildTime = DateTime.Now.ToString("yyyy/MM/dd-HH:mm:ss");
            EditorRefreshScriptingBackend(EditorUserBuildSettings.activeBuildTarget);
#if UNITY_IOS
            BuildNumber = PlayerSettings.iOS.buildNumber;
#elif UNITY_ANDROID
            BuildNumber = PlayerSettings.Android.bundleVersionCode.ToString();
#else
            Debug.LogWarning("Unsupported platform for BuildData BuildNumber retrieval.");
#endif
#if UNITY_CLOUD_BUILD
            RepositoryVersion += "-cloud";
#endif
        }
#endif

    }
}
