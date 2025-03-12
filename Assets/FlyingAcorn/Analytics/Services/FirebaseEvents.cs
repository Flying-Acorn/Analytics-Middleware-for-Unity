using System;
using System.Collections.Generic;
using com.binouze;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using FirebaseAnalytics = Firebase.Analytics.FirebaseAnalytics;
using static FlyingAcorn.Analytics.Constants.ErrorSeverity;
using static FlyingAcorn.Analytics.Constants.ProgressionStatus;
using static FlyingAcorn.Analytics.Constants.ResourceFlowType;
using ConsentStatus = Firebase.Analytics.ConsentStatus;

namespace FlyingAcorn.Analytics.Services
{
    [UsedImplicitly]
    public class FirebaseEvents : IAnalytics
    {
        [UsedImplicitly] public static FirebaseApp App { get; private set; }
        [UsedImplicitly] public bool IsInitialized => App != null;

        public int EventLengthLimit => 40;
        public int EventStepLengthLimit => -1;
        public string EventSeparator => "_";
        public static readonly UnityEvent OnInitialized = new();

        public FirebaseEvents()
        {
        }

        public void Initialize()
        {
            SetUserIdentifier(); // Prevent unreasonable crash
            Crashlytics.IsCrashlyticsCollectionEnabled = true;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    App = FirebaseApp.DefaultInstance;
                    Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                }
                else
                    Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                OnInitialized?.Invoke();
            });
        }

        public void UserSegmentation(string name, string property, int dimension = -1)
        {
            if (!IsInitialized) return;
            FirebaseAnalytics.SetUserProperty(name, property);
        }

        public void ResourceEvent(FlyingAcornResourceFlowType flowType, string currency, float amount, string itemType,
            string itemId)
        {
            if (!IsInitialized) return;
            var eventName = ConvertFirebaseAnalyticTypes.ResourceEventNameConvertor(flowType);
            var customData = ConvertFirebaseAnalyticTypes.CreateResourceParameters(currency, amount, itemType, itemId);
            FirebaseAnalytics.LogEvent(eventName, customData.ToArray());
        }

        public void SetUserIdentifier()
        {
            var userId = AnalyticsPlayerPrefs.CustomUserId;
            FirebaseAnalytics.SetUserId(userId);
            Crashlytics.SetUserId(userId);
        }

        public void BusinessEvent(string currency, decimal amount, string itemType, string itemId, string cartType,
            StoreType storeType, string receipt = null)
        {
            if (!IsInitialized) return;
            BusinessEvent(currency, amount, itemType, itemId, cartType, storeType, receipt,
                new Dictionary<string, object>());
        }

        public void BusinessEvent(string currency, decimal amount, string itemType, string itemId, string cartType,
            StoreType storeType, string receipt, Dictionary<string, object> customData)
        {
            if (!IsInitialized) return;
            if (storeType is StoreType.GooglePlay or StoreType.AppStore)
            {
                MyDebug.Info("Ignoring manual purchase event for Google Play or App Store for Firebase");
                return;
            }

            MyDebug.Verbose($"Sending Firebase purchase event: {currency} " +
                            $"with amount: {amount} with itemType: {itemType} " +
                            $"with itemID: {itemId} with cartType: {cartType} " +
                            $"with receipt: {receipt}");

            Parameter[] purchaseParameters =
            {
                new(FirebaseAnalytics.ParameterValue, decimal.ToDouble(amount)),
                new(FirebaseAnalytics.ParameterCurrency, currency),
                new(FirebaseAnalytics.ParameterTransactionID, receipt ?? "N/A"),
                new(FirebaseAnalytics.ParameterItemID, itemId),
                new(FirebaseAnalytics.ParameterStartDate, DateTime.Now.ToString())
            };
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPurchase, purchaseParameters);
        }

        public void DesignEvent(params string[] eventSteps)
        {
            if (!IsInitialized) return;
            FirebaseAnalytics.LogEvent(this.GetEventName(eventSteps));
        }

        public void DesignEvent(Dictionary<string, object> customFields, params string[] eventSteps)
        {
            if (!IsInitialized) return;
            var parameters = ConvertFirebaseAnalyticTypes.MakeParameters(customFields);
            FirebaseAnalytics.LogEvent(this.GetEventName(eventSteps), parameters.ToArray());
        }

        public void DesignEvent(float value, params string[] eventSteps)
        {
            if (!IsInitialized) return;
            FirebaseAnalytics.LogEvent(this.GetEventName(eventSteps), "FloatValue", value);
        }

        public void DesignEvent(float value, Dictionary<string, object> customFields, params string[] eventSteps)
        {
            if (!IsInitialized) return;
            var allFields = new Dictionary<string, object>(customFields);
            allFields.Add("FloatValue", value);
            var parameters = ConvertFirebaseAnalyticTypes.MakeParameters(allFields);
            FirebaseAnalytics.LogEvent(this.GetEventName(eventSteps), parameters.ToArray());
        }

        // ATTENTION: DO NOT USE MYDEBUG HERE
        public void ErrorEvent(FlyingAcornErrorSeverity severity, string message)
        {
            if (!IsInitialized) return;
            switch (severity)
            {
                case <= FlyingAcornErrorSeverity.DebugSeverity:
                    return;
                case FlyingAcornErrorSeverity.CriticalSeverity:
                    Crashlytics.LogException(new Exception(message));
                    break;
                default:
                    Crashlytics.Log(message);
                    break;
            }
        }

        public void ProgressionEvent(FlyingAcornProgressionStatus progressionStatus, string levelType,
            string levelNumber)
        {
            if (!IsInitialized) return;
            var eventName = ConvertFirebaseAnalyticTypes.ProgressionNameConvertor(progressionStatus);
            var customData =
                ConvertFirebaseAnalyticTypes.CreateProgressionParameters(progressionStatus, levelType, levelNumber);
            FirebaseAnalytics.LogEvent(eventName, customData.ToArray());
        }

        public void ProgressionEvent(FlyingAcornProgressionStatus progressionStatus, string levelType,
            string levelNumber, int score)
        {
            if (!IsInitialized) return;
            var eventName = ConvertFirebaseAnalyticTypes.ProgressionNameConvertor(progressionStatus);
            var customData =
                ConvertFirebaseAnalyticTypes.CreateProgressionParameters(progressionStatus, levelType, levelNumber,
                    score);
            FirebaseAnalytics.LogEvent(eventName, customData.ToArray());
        }

        public void ProgressionEvent(FlyingAcornProgressionStatus progressionStatus, string levelType,
            string levelNumber, int score, Dictionary<string, object> customFields)
        {
            if (!IsInitialized) return;
            var eventName = ConvertFirebaseAnalyticTypes.ProgressionNameConvertor(progressionStatus);
            var customData =
                ConvertFirebaseAnalyticTypes.CreateProgressionParameters(progressionStatus, levelType, levelNumber,
                    score);
            customData.AddRange(ConvertFirebaseAnalyticTypes.MakeParameters(customFields));
            FirebaseAnalytics.LogEvent(eventName, customData.ToArray());
        }

        public void NonLevelProgressionEvent(FlyingAcornNonLevelStatus progressionStatus, string progressionType)
        {
        }

        public void SetConsents()
        {
            MyDebug.Verbose("Setting Firebase consents");
            var firebaseConsent = new Dictionary<ConsentType, ConsentStatus>();
            if (GoogleUserMessagingPlatform.IsGDPRRequired())
            {
                var vendorAutorized = GoogleUserMessagingPlatform.GetConsentForVendor(VendorsIds.Google);
                if (vendorAutorized)
                {
                    // GoogleVendor consent OK

                    var adStorage = GoogleUserMessagingPlatform.GetFirebaseAdStorage();
                    var adPerso = GoogleUserMessagingPlatform.GetFirebaseAdPersonalization();
                    var adUserData = GoogleUserMessagingPlatform.GetFirebaseAdUserData();

                    MyDebug.Info(
                        $"Maj Firebase consent: adStorage:{adStorage} adPerso:{adPerso} adUserData:{adUserData}");

                    firebaseConsent.Add(ConsentType.AnalyticsStorage,
                        adStorage ? ConsentStatus.Granted : ConsentStatus.Denied);
                    firebaseConsent.Add(ConsentType.AdPersonalization,
                        adPerso ? ConsentStatus.Granted : ConsentStatus.Denied);
                    firebaseConsent.Add(ConsentType.AdStorage,
                        adStorage ? ConsentStatus.Granted : ConsentStatus.Denied);
                    firebaseConsent.Add(ConsentType.AdUserData,
                        adUserData ? ConsentStatus.Granted : ConsentStatus.Denied);

                    // or maybe true as vendor is consented
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(adStorage || adPerso || adUserData);
                }
                else
                {
                    // GoogleVendor NOT CONSENTED

                    MyDebug.Info(" Google vendor not consented");

                    firebaseConsent.Add(ConsentType.AnalyticsStorage, ConsentStatus.Denied);
                    firebaseConsent.Add(ConsentType.AdPersonalization, ConsentStatus.Denied);
                    firebaseConsent.Add(ConsentType.AdStorage, ConsentStatus.Denied);
                    firebaseConsent.Add(ConsentType.AdUserData, ConsentStatus.Denied);

                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(false);
                }
            }
            else
            {
                // GDPR NOT APPLICABLE

                firebaseConsent.Add(ConsentType.AnalyticsStorage, ConsentStatus.Granted);
                firebaseConsent.Add(ConsentType.AdPersonalization, ConsentStatus.Granted);
                firebaseConsent.Add(ConsentType.AdStorage, ConsentStatus.Granted);
                firebaseConsent.Add(ConsentType.AdUserData, ConsentStatus.Granted);

                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            }

            FirebaseAnalytics.SetConsent(firebaseConsent);
        }
    }
}