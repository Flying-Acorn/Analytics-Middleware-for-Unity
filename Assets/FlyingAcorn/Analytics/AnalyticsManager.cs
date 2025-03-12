using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace FlyingAcorn.Analytics
{
    [Serializable]
    public class AnalyticsManager
    {
        protected static AnalyticsManager Instance;

        protected AnalyticServiceProvider AnalyticServiceProvider;
        protected internal static bool InitCalled;
        private static bool _started;

        private AnalyticsManager(AnalyticServiceProvider analyticServiceProvider)
        {
            if (_started) return;
            AnalyticServiceProvider = analyticServiceProvider;
            _started = true;
            if (AnalyticsPlayerPrefs.SessionCount <= 0)
            {
                AnalyticsPlayerPrefs.InstallationVersion = Application.version;
                // AnalyticsPlayerPrefs.InstallationBuild = GetUserBuildNumber(); TODO: Implement this
                AnalyticServiceProvider?.DesignEvent("FA_session", "first");
            }

            AnalyticsPlayerPrefs.SessionCount++;
            AnalyticServiceProvider?.DesignEvent(AnalyticsPlayerPrefs.SessionCount, "FA_session", "start");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!_started)
                return;
            
            var eventName = pauseStatus ? "pause" : "unpause";
            AnalyticServiceProvider?.DesignEvent("FA_session", eventName);
        }

        ~AnalyticsManager()
        {
            if (!_started)
                return;
            AnalyticServiceProvider?.DesignEvent("FA_session", "end");
        }

        public virtual void SetConsents()
        {
            Debug.Log("SetConsents not implemented");
        }

        public static void SaveUserIdentifier(string playerId)
        {
            MyDebug.Info($"Saving user identifier: {playerId}");
            AnalyticsPlayerPrefs.CustomUserId = playerId;
        }

        protected static void SetAnalyticsConsents()
        {
            if (Instance?.AnalyticServiceProvider == null)
            {
                MyDebug.LogWarning("Analytics not initialized");
                return;
            }

            Instance.SetConsents();
        }

        public static void BusinessEvent(string currency, decimal amount, string itemType, string itemId,
            string cartType,
            StoreType storeType, string receipt, Dictionary<string, object> customData)
        {
            if (Instance?.AnalyticServiceProvider == null)
            {
                MyDebug.LogWarning("Analytics not initialized");
                return;
            }

            Instance.AnalyticServiceProvider.BusinessEvent(currency, amount, itemType, itemId, cartType, storeType,
                receipt, customData);
        }
        
        public static void DesignEvent(Dictionary<string, object> customFields, params string[] eventSteps)
        {
            if (Instance?.AnalyticServiceProvider == null)
            {
                MyDebug.LogWarning("Analytics not initialized");
                return;
            }

            Instance.AnalyticServiceProvider.DesignEvent(customFields, eventSteps);
        }

        // ATTENTION: DO NOT USE MYDEBUG HERE
        public static void ErrorEvent(Constants.ErrorSeverity.FlyingAcornErrorSeverity severity, string message)
        {
            Instance?.AnalyticServiceProvider?.ErrorEvent(severity, message);
        }

        public static void UserSegmentation(string name, string value, int dimension=-1)
        {
            if (Instance?.AnalyticServiceProvider == null)
            {
                MyDebug.LogWarning("Analytics not initialized");
                return;
            }

            Instance.AnalyticServiceProvider.UserSegmentation(name, value, dimension);
        }

        public static void Initialize(List<IAnalytics> services)
        {
            if (InitCalled)
            {
                MyDebug.LogWarning("Init already called");
                return;
            }

            Instance = new AnalyticsManager(new AnalyticServiceProvider(services));
            Instance.Init();
        }

        // ReSharper disable once MemberCanBePrivate.Global
        protected virtual void Init()
        {
            if (InitCalled)
            {
                MyDebug.LogWarning("Init already called");
                return;
            }
            
            InitCalled = true;
            AnalyticServiceProvider.Initialize();
        }

        public static IAnalytics GetRunningService([NotNull] Type type)
        {
            return Instance?.AnalyticServiceProvider?.GetServices().Find(s => s.GetType() == type);
        }

        public IAnalytics GetService([NotNull] Type type)
        {
            return Instance.AnalyticServiceProvider.GetServices().Find(s => s.GetType() == type);
        }

        public static void SetDebugMode(bool debugMode)
        {
            MyDebug.Info($"Debug mode set to {debugMode}");
            AnalyticsPlayerPrefs.UserDebugMode = debugMode;
        }
    }
}