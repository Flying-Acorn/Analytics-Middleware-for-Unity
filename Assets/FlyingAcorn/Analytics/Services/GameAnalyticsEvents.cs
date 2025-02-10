using System.Collections.Generic;
using GameAnalyticsSDK;
using JetBrains.Annotations;
using static FlyingAcorn.Analytics.Constants.ProgressionStatus;
using static FlyingAcorn.Analytics.Constants.ResourceFlowType;
using static FlyingAcorn.Analytics.Constants.ErrorSeverity;

namespace FlyingAcorn.Analytics.Services
{
    [UsedImplicitly]
    public class GameAnalyticsEvents : IAnalytics
    {
        public bool IsInitialized { get; private set; }

        #region methods

        public int EventLengthLimit => 5 * EventStepLengthLimit + 4; // 4 separators and 5 segments
        public int EventStepLengthLimit => 32;
        public string EventSeparator => ":";

        public void Initialize()
        {
            GameAnalytics.Initialize();
            IsInitialized = true;
        }

        // ATTENTION: DO NOT USE MYDEBUG HERE
        public void ErrorEvent(FlyingAcornErrorSeverity severity, string message)
        {
            if (!IsInitialized) return;
            if (severity < FlyingAcornErrorSeverity.WarningSeverity)
                return;

            var status = ConvertGameAnalyticsTypes.ConvertorErrors(severity);

            if (status != GAErrorSeverity.Undefined)
            {
                GameAnalytics.NewErrorEvent(ConvertGameAnalyticsTypes.ConvertorErrors(severity), message);
                return;
            }

            UnityEngine.Debug.Log("<color=red> ********* GameAnalytics Implementation error event" +
                                  ":: undefined argument   </color>");
        }


        public void UserSegmentation(string name, string property, int dimension = -1)
        {
            if (!IsInitialized) return;
            switch (dimension)
            {
                case 1:
                    GameAnalytics.SetCustomDimension01(property);
                    break;
                case 2:
                    GameAnalytics.SetCustomDimension02(property);
                    break;
                case 3:
                    GameAnalytics.SetCustomDimension03(property);
                    break;
            }
        }

        public void ResourceEvent(FlyingAcornResourceFlowType flowType, string currency, float amount, string itemType,
            string itemID)
        {
            if (!IsInitialized) return;
            var status = ConvertGameAnalyticsTypes.ConvertorFlowType(flowType);

            if (status != GAResourceFlowType.Undefined)
            {
                GameAnalytics.NewResourceEvent(status, currency, amount, itemType, itemID);
                return;
            }

            MyDebug.Verbose("<color=red> ********* GameAnalytics Implementation resource event" +
                            ":: undefined argument   </color>");
        }

        public void SetUserIdentifier(string userId)
        {
            if (!IsInitialized) return;
            GameAnalytics.SetCustomId(userId);
        }

        public void SetConsents()
        {
            if (!IsInitialized) return;
        }

        public void BusinessEvent(string currency, decimal amount, string itemType, string itemId, string cartType,
            StoreType storeType, string receipt = null)
        {
            BusinessEvent(currency, amount, itemType, itemId, cartType, storeType, receipt,
                new Dictionary<string, object>());
        }

        public void BusinessEvent(string currency, decimal amount, string itemType, string itemId, string cartType,
            StoreType storeType, string receipt, Dictionary<string, object> customData)
        {
            if (!IsInitialized) return;
            var GAAmount = decimal.ToInt32(amount * 100);
            MyDebug.Verbose($"Sending business event to analytics: {currency} " +
                            $"with amount: {GAAmount} with itemType: {itemType} " +
                            $"with itemID: {itemId} with cartType: {cartType} " +
                            $"with receipt: {receipt} for these services: GameAnalytics");

            if (storeType is StoreType.AppStore or StoreType.GooglePlay)
            {
#if (UNITY_ANDROID)
                GameAnalytics.NewBusinessEventGooglePlay(currency, GAAmount, itemType, itemId, cartType, receipt, null,
                    customData);
#endif
#if (UNITY_IOS)
                if (string.IsNullOrEmpty(receipt))
                {
                    GameAnalytics.NewBusinessEventIOS(currency, GAAmount, itemType, itemId, cartType, receipt,
                        customData);
                }
                else
                {
                    GameAnalytics.NewBusinessEventIOSAutoFetchReceipt(currency, GAAmount, itemType, itemId, cartType,
                        customData);
                }
#endif
            }
            else
            {
                GameAnalytics.NewBusinessEvent(currency, GAAmount, itemType, itemId, cartType, customData);
            }
        }

        public void DesignEvent(params string[] eventSteps)
        {
            if (!IsInitialized) return;
            GameAnalytics.NewDesignEvent(this.GetEventName(eventSteps));
        }

        public void DesignEvent(Dictionary<string, object> customData, params string[] eventSteps)
        {
            if (!IsInitialized) return;
            GameAnalytics.NewDesignEvent(this.GetEventName(eventSteps), customData);
        }

        public void DesignEvent(float value, params string[] eventSteps)
        {
            if (!IsInitialized) return;
            GameAnalytics.NewDesignEvent(this.GetEventName(eventSteps), value);
        }

        public void DesignEvent(float value, Dictionary<string, object> customData, params string[] eventSteps)
        {
            if (!IsInitialized) return;
            GameAnalytics.NewDesignEvent(this.GetEventName(eventSteps), value, customData);
        }

        public void ProgressionEvent(FlyingAcornProgressionStatus progressionStatus, string levelType,
            string levelNumber)
        {
            if (!IsInitialized) return;
            var status = ConvertGameAnalyticsTypes.ConvertorProgression(progressionStatus);
            GameAnalytics.NewProgressionEvent(status, levelType, levelNumber);
        }

        public void ProgressionEvent(FlyingAcornProgressionStatus progressionStatus, string levelType,
            string levelNumber, int score)
        {
            if (!IsInitialized) return;
            var status = ConvertGameAnalyticsTypes.ConvertorProgression(progressionStatus);
            GameAnalytics.NewProgressionEvent(status, levelType, levelNumber, score);
        }

        public void ProgressionEvent(FlyingAcornProgressionStatus progressionStatus, string levelType,
            string levelNumber, int score, Dictionary<string, object> customFields)
        {
            if (!IsInitialized) return;
            var status = ConvertGameAnalyticsTypes.ConvertorProgression(progressionStatus);
            GameAnalytics.NewProgressionEvent(status, levelType, levelNumber, score, customFields);
        }

        public void NonLevelProgressionEvent(FlyingAcornNonLevelStatus progressionStatus, string progressionType)
        {
            if (!IsInitialized) return;
            var status = ConvertGameAnalyticsTypes.ConvertorNoneLevelProgression(progressionStatus);
            GameAnalytics.NewProgressionEvent(status, progressionType);
        }

        #endregion
    }
}