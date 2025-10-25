using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
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
        [UsedImplicitly] public FirebaseApp App { get; private set; }
        [UsedImplicitly] public bool IsInitialized => App != null;

        public int EventLengthLimit => 40;
        public int EventStepLengthLimit => -1;
        public string EventSeparator => "_";
        public static readonly UnityEvent OnInitialized = new();

        private const string GOOGLE_UMP_CONSENT_SETUP_FAILED = "Google UMP consent setup failed";
        private const string FIREBASE_CONSENT_SETUP_FAILED = "Firebase consent setup also failed";
        private const string CONTINUING_WITHOUT_CONSENT_SETUP = "Continuing without consent setup";
        private static TaskCompletionSource<bool> _initializationTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private static int _initializeCalled;

        public void Initialize()
        {
            if (Interlocked.Exchange(ref _initializeCalled, 1) == 1)
            {
                return;
            }

            TaskScheduler scheduler;
            try
            {
                scheduler = SynchronizationContext.Current != null
                    ? TaskScheduler.FromCurrentSynchronizationContext()
                    : TaskScheduler.Current;
            }
            catch
            {
                scheduler = TaskScheduler.Current;
            }

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var dependencyStatus = task.Result;
                    if (dependencyStatus == DependencyStatus.Available)
                    {
                        App = FirebaseApp.DefaultInstance;
                        Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                        Crashlytics.IsCrashlyticsCollectionEnabled = true;

                        SetUserIdentifier();

                        _initializationTcs.TrySetResult(true);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                        _initializationTcs.TrySetResult(false);
                    }
                }
                else if (task.IsFaulted)
                {
                    Debug.LogWarning($"Firebase initialization failed: {task.Exception}");
                    _initializationTcs.TrySetException(task.Exception!);
                }
                else if (task.IsCanceled)
                {
                    Debug.LogWarning("Firebase dependency check was canceled.");
                    _initializationTcs.TrySetCanceled();
                }

                OnInitialized?.Invoke();
            }, scheduler);
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
            if (!IsInitialized) return;
            var userId = AnalyticsPlayerPrefs.CustomUserId;
            TrySetUserIdentifier(userId);
        }

        private void TrySetUserIdentifier(string userId)
        {
            try
            {
                FirebaseAnalytics.SetUserId(userId);
            }
            catch (Exception e)
            {
                MyDebug.LogWarning($"Failed to set Firebase user ID: {e.Message}");
            }

            try
            {
                Crashlytics.SetUserId(userId);
            }
            catch (Exception e)
            {
                MyDebug.LogWarning($"Failed to set Crashlytics user ID: {e.Message}");
            }
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

            if (string.IsNullOrEmpty(currency) || string.IsNullOrEmpty(itemId) || amount <= 0)
            {
                MyDebug.LogWarning("BusinessEvent: Invalid parameters provided - currency, itemId cannot be null/empty and amount must be > 0");
                return;
            }

            var purchaseParameters = new List<Parameter>
            {
                new(FirebaseAnalytics.ParameterValue, (double)amount),
                new(FirebaseAnalytics.ParameterCurrency, currency),
                new(FirebaseAnalytics.ParameterTransactionID, receipt ?? $"manual_{DateTime.UtcNow.Ticks}"),
                new(FirebaseAnalytics.ParameterItemID, itemId),
                new(FirebaseAnalytics.ParameterStartDate, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
            };

            if (!string.IsNullOrEmpty(itemType))
            {
                purchaseParameters.Add(new Parameter("item_type", itemType));
            }

            if (!string.IsNullOrEmpty(cartType))
            {
                purchaseParameters.Add(new Parameter("cart_type", cartType));
            }


            if (AnalyticsPlayerPrefs.UserDebugMode)
            {
                string parameters = $"Value: {(double)amount}, Currency: {currency}, TransactionID: {receipt ?? $"manual_{DateTime.UtcNow.Ticks}"}, ItemID: {itemId}, StartDate: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}";
                if (!string.IsNullOrEmpty(itemType)) parameters += $", ItemType: {itemType}";
                if (!string.IsNullOrEmpty(cartType)) parameters += $", CartType: {cartType}";
                MyDebug.Info($"[Firebase] Sending BusinessEvent - Parameters: {{{parameters}}}");
                return;
            }

            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPurchase, purchaseParameters.ToArray());
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
            var allFields = new Dictionary<string, object>(customFields)
            {
                { "FloatValue", value }
            };
            var parameters = ConvertFirebaseAnalyticTypes.MakeParameters(allFields);
            FirebaseAnalytics.LogEvent(this.GetEventName(eventSteps), parameters.ToArray());
        }

        public void SignUpEvent(string method, Dictionary<string, object> extraFields = null)
        {
            if (!IsInitialized) return;
            var eventName = FirebaseAnalytics.EventSignUp;
            extraFields ??= new Dictionary<string, object>();
            extraFields[FirebaseAnalytics.ParameterMethod] = method;
            var parameters = ConvertFirebaseAnalyticTypes.MakeParameters(extraFields);
            FirebaseAnalytics.LogEvent(eventName, parameters.ToArray());
        }

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

            try
            {
                var firebaseConsentMap = CreateFirebaseConsentMap();
                ApplyFirebaseConsents(firebaseConsentMap);
            }
            catch (Exception ex)
            {
                HandleConsentSetupFailure(ex);
            }
        }

        private Dictionary<ConsentType, ConsentStatus> CreateFirebaseConsentMap()
        {
            var consentMap = new Dictionary<ConsentType, ConsentStatus>();

            if (GoogleUserMessagingPlatform.IsGDPRRequired())
            {
                ConfigureGDPRConsents(consentMap);
            }
            else
            {
                ConfigureNonGDPRConsents(consentMap);
            }

            return consentMap;
        }

        private void ConfigureGDPRConsents(Dictionary<ConsentType, ConsentStatus> consentMap)
        {
            var isGoogleVendorAuthorized = GoogleUserMessagingPlatform.GetConsentForVendor(VendorsIds.Google);

            if (isGoogleVendorAuthorized)
            {
                ConfigureAuthorizedVendorConsents(consentMap);
            }
            else
            {
                ConfigureUnauthorizedVendorConsents(consentMap);
                MyDebug.Info("Google vendor not consented");
            }
        }

        private void ConfigureAuthorizedVendorConsents(Dictionary<ConsentType, ConsentStatus> consentMap)
        {
            var adStorageEnabled = GoogleUserMessagingPlatform.GetFirebaseAdStorage();
            var adPersonalizationEnabled = GoogleUserMessagingPlatform.GetFirebaseAdPersonalization();
            var adUserDataEnabled = GoogleUserMessagingPlatform.GetFirebaseAdUserData();

            MyDebug.Info($"Firebase consent: adStorage:{adStorageEnabled} adPerso:{adPersonalizationEnabled} adUserData:{adUserDataEnabled}");

            consentMap.Add(ConsentType.AnalyticsStorage, adStorageEnabled ? ConsentStatus.Granted : ConsentStatus.Denied);
            consentMap.Add(ConsentType.AdPersonalization, adPersonalizationEnabled ? ConsentStatus.Granted : ConsentStatus.Denied);
            consentMap.Add(ConsentType.AdStorage, adStorageEnabled ? ConsentStatus.Granted : ConsentStatus.Denied);
            consentMap.Add(ConsentType.AdUserData, adUserDataEnabled ? ConsentStatus.Granted : ConsentStatus.Denied);

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(adStorageEnabled || adPersonalizationEnabled || adUserDataEnabled);
        }

        private void ConfigureUnauthorizedVendorConsents(Dictionary<ConsentType, ConsentStatus> consentMap)
        {
            consentMap.Add(ConsentType.AnalyticsStorage, ConsentStatus.Denied);
            consentMap.Add(ConsentType.AdPersonalization, ConsentStatus.Denied);
            consentMap.Add(ConsentType.AdStorage, ConsentStatus.Denied);
            consentMap.Add(ConsentType.AdUserData, ConsentStatus.Denied);

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(false);
        }

        private void ConfigureNonGDPRConsents(Dictionary<ConsentType, ConsentStatus> consentMap)
        {
            consentMap.Add(ConsentType.AnalyticsStorage, ConsentStatus.Granted);
            consentMap.Add(ConsentType.AdPersonalization, ConsentStatus.Granted);
            consentMap.Add(ConsentType.AdStorage, ConsentStatus.Granted);
            consentMap.Add(ConsentType.AdUserData, ConsentStatus.Granted);

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        }

        private void ApplyFirebaseConsents(Dictionary<ConsentType, ConsentStatus> consentMap)
        {
            FirebaseAnalytics.SetConsent(consentMap);
        }

        private void HandleConsentSetupFailure(Exception originalException)
        {
            MyDebug.LogWarning($"{GOOGLE_UMP_CONSENT_SETUP_FAILED}: {originalException.Message}. Setting default Firebase consents.");

            var defaultConsentMap = CreateDefaultConsentMap();

            try
            {
                ApplyFirebaseConsents(defaultConsentMap);
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            }
            catch (Exception firebaseException)
            {
                MyDebug.LogWarning($"{FIREBASE_CONSENT_SETUP_FAILED}: {firebaseException.Message}. {CONTINUING_WITHOUT_CONSENT_SETUP}.");
            }
        }

        private Dictionary<ConsentType, ConsentStatus> CreateDefaultConsentMap()
        {
            return new Dictionary<ConsentType, ConsentStatus>
            {
                { ConsentType.AnalyticsStorage, ConsentStatus.Granted },
                { ConsentType.AdPersonalization, ConsentStatus.Granted },
                { ConsentType.AdStorage, ConsentStatus.Granted },
                { ConsentType.AdUserData, ConsentStatus.Granted }
            };
        }
    }
}