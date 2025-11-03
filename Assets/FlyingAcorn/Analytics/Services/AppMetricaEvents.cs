using System.Collections.Generic;
using Io.AppMetrica;
using Io.AppMetrica.Profile;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using Attribute = Io.AppMetrica.Profile.Attribute;
using static FlyingAcorn.Analytics.BuildData.Constants;

namespace FlyingAcorn.Analytics.Services
{
    [UsedImplicitly]
    public class AppMetricaEvents : IAnalytics
    {
        private readonly string _appKey;

        public AppMetricaEvents(string appKey)
        {
            _appKey = appKey;
        }

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
            AppMetrica.OnActivation -= OnInitialized;
            AppMetrica.OnActivation += OnInitialized;
            var config = new AppMetricaConfig(_appKey)
            {
                FirstActivationAsUpdate = !IsFirstLaunch(),
            };
            AppMetrica.Activate(config);
            PlayerPrefs.SetInt("MetricaIsFirstLaunch", 1);
        }

        private void OnInitialized(AppMetricaConfig config)
        {
            IsInitialized = true;
            SetUserIdentifier();
        }

        // ATTENTION: DO NOT USE MYDEBUG HERE
        public void ErrorEvent(Constants.ErrorSeverity.FlyingAcornErrorSeverity severity, string message)
        {
            if (!IsInitialized) return;
            if (severity < Constants.ErrorSeverity.FlyingAcornErrorSeverity.WarningSeverity)
            {
                return;
            }

            try
            {
                AppMetrica.ReportError(message);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to report AppMetrica error: {ex.Message}");
            }
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

        public void SetUserIdentifier()
        {
            if (string.IsNullOrEmpty(AnalyticsPlayerPrefs.CustomUserId)) return;
            AppMetrica.SetUserProfileID(AnalyticsPlayerPrefs.CustomUserId);
        }

        public void SetConsents()
        {
            if (!IsInitialized) return;
            MyDebug.Verbose("SetConsents is not implemented for AppMetrica");
        }

        public void BusinessEvent(string currency, decimal amount, string itemType, string itemId, string cartType,
            Store Store, string receipt = null)
        {
            if (!IsInitialized) return;
            BusinessEvent(currency, amount, itemType, itemId, cartType, Store, receipt, null);
        }

        public void BusinessEvent(string currency, decimal amount, string itemType, string itemId, string cartType,
            Store Store, string receipt, Dictionary<string, object> customData)
        {
            if (!IsInitialized) return;

            // Validate required parameters
            if (string.IsNullOrEmpty(currency) || string.IsNullOrEmpty(itemId) || amount <= 0)
            {
                MyDebug.LogWarning("BusinessEvent: Invalid parameters - currency and itemId cannot be null/empty, amount must be > 0");
                return;
            }

            if (AppMetrica.ActivationConfig != null && AppMetrica.ActivationConfig.RevenueAutoTrackingEnabled != null)
            {
                if (AppMetrica.ActivationConfig.RevenueAutoTrackingEnabled.Value)
                {
                    if (Store is Store.AppStore or Store.GooglePlay)
                    {
                        MyDebug.Info(
                            "AppMetrica revenue auto tracking is enabled, skipping manual revenue tracking");
                        return;
                    }
                }
            }

            // Safe conversion to micros with overflow protection
            decimal microsDecimal = amount * 1000000m;
            if (microsDecimal > long.MaxValue || microsDecimal < long.MinValue)
            {
                MyDebug.LogWarning($"BusinessEvent: Amount {amount} would cause overflow in micros conversion");
                return;
            }

            var priceMicros = (long)microsDecimal;

            if (AnalyticsPlayerPrefs.UserDebugMode)
            {
                MyDebug.Info($"[AppMetrica] Sending BusinessEvent - Currency: {currency}, Amount: {amount} ({priceMicros} micros), ItemType: {itemType}, ItemId: {itemId}, CartType: {cartType}, Store: {Store}, Receipt: {receipt ?? "null"}");
                return;
            }

            try
            {
                var yandexAppMetricaRevenue = new Revenue(priceMicros, currency);
                AppMetrica.ReportRevenue(yandexAppMetricaRevenue);
            }
            catch (System.Exception ex)
            {
                MyDebug.LogWarning($"Failed to report AppMetrica revenue: {ex.Message}");
            }
        }

        public void DesignEvent(params string[] eventSteps)
        {
            if (!IsInitialized) return;
            try
            {
                AppMetrica.ReportEvent(this.GetEventName(eventSteps));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to report AppMetrica design event: {ex.Message}");
            }
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
            ProgressionEventInternal(progressionStatus, levelType, levelNumber, null, null);
        }

        public void ProgressionEvent(Constants.ProgressionStatus.FlyingAcornProgressionStatus progressionStatus,
            string levelType, string levelNumber, int score)
        {
            ProgressionEventInternal(progressionStatus, levelType, levelNumber, score, null);
        }

        public void ProgressionEvent(Constants.ProgressionStatus.FlyingAcornProgressionStatus progressionStatus,
            string levelType, string levelNumber, int score,
            Dictionary<string, object> customFields)
        {
            ProgressionEventInternal(progressionStatus, levelType, levelNumber, score, customFields);
        }

        private void ProgressionEventInternal(Constants.ProgressionStatus.FlyingAcornProgressionStatus progressionStatus,
            string levelType, string levelNumber, int? score, Dictionary<string, object> customFields)
        {
            if (!IsInitialized) return;

            var data = new Dictionary<string, object>
            {
                { "levelType", levelType },
                { "levelNumber", levelNumber },
                { "status", progressionStatus.ToString() }
            };

            // Add score if provided
            if (score.HasValue)
            {
                data["score"] = score.Value;
            }

            // Add custom fields if provided
            if (customFields != null)
            {
                foreach (var kvp in customFields)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            var eventName = progressionStatus switch
            {
                Constants.ProgressionStatus.FlyingAcornProgressionStatus.StartLevel => "FA_level_start",
                Constants.ProgressionStatus.FlyingAcornProgressionStatus.CompleteLevel => "FA_level_complete",
                Constants.ProgressionStatus.FlyingAcornProgressionStatus.FailLevel => "FA_level_fail",
                _ => throw new System.ArgumentOutOfRangeException(nameof(progressionStatus), progressionStatus, null)
            };

            DesignEvent(data, eventName);
        }


        public void SignUpEvent(string method, Dictionary<string, object> extraFields = null)
        {
            if (!IsInitialized) return;

            var eventName = "FA_sign_up";
            extraFields ??= new Dictionary<string, object>();
            extraFields["method"] = method;
            DesignEvent(extraFields, eventName);
        }


        public void NonLevelProgressionEvent(Constants.ProgressionStatus.FlyingAcornNonLevelStatus progressionStatus,
            string progressionType)
        {
            if (!IsInitialized) return;
            MyDebug.Verbose("NonLevelProgressionEvent is not implemented for AppMetrica");
        }
    }
}