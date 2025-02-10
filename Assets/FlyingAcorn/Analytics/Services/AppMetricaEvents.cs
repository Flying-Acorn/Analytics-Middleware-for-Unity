using System.Collections.Generic;
using Io.AppMetrica;
using Io.AppMetrica.Profile;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using Attribute = Io.AppMetrica.Profile.Attribute;

namespace FlyingAcorn.Analytics.Services
{
    [UsedImplicitly]
    public class AppMetricaEvents : IAnalytics
    {
        [SerializeField] public string AppKey;

        private static bool IsFirstLaunch()
        {
            return PlayerPrefs.GetInt("MetricaIsFirstLaunch", 0) == 0;
        }
        
        public int EventLengthLimit => -1;
        public int EventStepLengthLimit => -1;
        public bool IsInitialized { get; private set; }
        public string EventSeparator => "_";

        public void Initialize()
        {
            AppMetrica.OnActivation += _ => IsInitialized = true;
            AppMetrica.Activate(new AppMetricaConfig(AppKey)
            {
                FirstActivationAsUpdate = !IsFirstLaunch(),
            });
            PlayerPrefs.SetInt("MetricaIsFirstLaunch", 1);
        }

        // ATTENTION: DO NOT USE MYDEBUG HERE
        public void ErrorEvent(Constants.ErrorSeverity.FlyingAcornErrorSeverity severity, string message)
        {
            if (!IsInitialized) return;
            if (severity < Constants.ErrorSeverity.FlyingAcornErrorSeverity.ErrorSeverity)
            {
                return;
            }

            AppMetrica.ReportError(message);
        }

        public void UserSegmentation(string name, string property, int dimension = -1)
        {
            if (!IsInitialized) return;
            var profile = new UserProfile();
            profile.Apply(Attribute.CustomString(name).WithValue(property));
            AppMetrica.ReportUserProfile(profile);
        }

        public void ResourceEvent(Constants.ResourceFlowType.FlyingAcornResourceFlowType flowType, string currency,
            float amount, string itemType, string itemId)
        {
            if (!IsInitialized) return;
            var info = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "currency", currency },
                { "value", amount },
                { "itemType", itemType },
                { "itemId", itemId }
            });
            AppMetrica.ReportEvent(flowType.ToString(), info);
        }

        public void SetUserIdentifier(string userId)
        {
            if (!IsInitialized) return;
            AppMetrica.SetUserProfileID(userId);
        }

        public void SetConsents()
        {
            if (!IsInitialized) return;
            MyDebug.Verbose("SetConsents is not implemented for AppMetrica");
        }

        public void BusinessEvent(string currency, decimal amount, string itemType, string itemId, string cartType,
            StoreType storeType, string receipt = null)
        {
            if (!IsInitialized) return;
            BusinessEvent(currency, amount, itemType, itemId, cartType, storeType, receipt, null);
        }

        public void BusinessEvent(string currency, decimal amount, string itemType, string itemId, string cartType,
            StoreType storeType, string receipt, Dictionary<string, object> customData)
        {
            if (!IsInitialized) return;
            if (AppMetrica.ActivationConfig != null && AppMetrica.ActivationConfig.RevenueAutoTrackingEnabled != null)
            {
                if (AppMetrica.ActivationConfig.RevenueAutoTrackingEnabled.Value)
                {
                    if (storeType is StoreType.AppStore or StoreType.GooglePlay)
                    {
                        MyDebug.Info(
                            "AppMetrica revenue auto tracking is enabled, skipping manual revenue tracking");
                        return;
                    }
                }
            }

            // 990000 (equivalent to 0.99 in real currency)
            var priceMicros = (long)(amount * 1000000);
            MyDebug.Verbose($"Sending business event to analytics: {currency} " +
                            $"with priceMicros: {priceMicros} with itemType: {itemType}");
            var yandexAppMetricaRevenue = new Revenue(priceMicros, currency);
            AppMetrica.ReportRevenue(yandexAppMetricaRevenue);
        }

        public void DesignEvent(params string[] eventSteps)
        {
            if (!IsInitialized) return;
            AppMetrica.ReportEvent(this.GetEventName(eventSteps));
        }

        public void DesignEvent(Dictionary<string, object> customFields, params string[] eventSteps)
        {
            if (!IsInitialized) return;
            AppMetrica.ReportEvent(this.GetEventName(eventSteps), JsonConvert.SerializeObject(customFields));
        }

        public void DesignEvent(float value, params string[] eventSteps)
        {
            if (!IsInitialized) return;
            var finalCustomFields = new Dictionary<string, object> { { "value", value } };
            AppMetrica.ReportEvent(this.GetEventName(eventSteps), JsonConvert.SerializeObject(finalCustomFields));
        }

        public void DesignEvent(float value, Dictionary<string, object> customFields, params string[] eventSteps)
        {
            if (!IsInitialized) return;
            var finalCustomFields = new Dictionary<string, object>(customFields) { { "value", value } };
            AppMetrica.ReportEvent(this.GetEventName(eventSteps), JsonConvert.SerializeObject(finalCustomFields));
        }

        public void ProgressionEvent(Constants.ProgressionStatus.FlyingAcornProgressionStatus progressionStatus,
            string levelType, string levelNumber)
        {
            if (!IsInitialized) return;
            AppMetrica.ReportEvent($"{levelType}_{progressionStatus}_{levelNumber}");
        }

        public void ProgressionEvent(Constants.ProgressionStatus.FlyingAcornProgressionStatus progressionStatus,
            string levelType, string levelNumber, int score)
        {
            if (!IsInitialized) return;
            var finalCustomFields = new Dictionary<string, object> { { "score", score } };
            AppMetrica.ReportEvent($"{levelType}_{progressionStatus}_{levelNumber}",
                JsonConvert.SerializeObject(finalCustomFields));
        }

        public void ProgressionEvent(Constants.ProgressionStatus.FlyingAcornProgressionStatus progressionStatus,
            string levelType, string levelNumber, int score,
            Dictionary<string, object> customFields)
        {
            if (!IsInitialized) return;
            var finalCustomFields = new Dictionary<string, object>(customFields) { { "score", score } };
            AppMetrica.ReportEvent($"{levelType}_{progressionStatus}_{levelNumber}",
                JsonConvert.SerializeObject(finalCustomFields));
        }

        public void NonLevelProgressionEvent(Constants.ProgressionStatus.FlyingAcornNonLevelStatus progressionStatus,
            string progressionType)
        {
            if (!IsInitialized) return;
            MyDebug.Verbose("NonLevelProgressionEvent is not implemented for AppMetrica");
        }
    }
}