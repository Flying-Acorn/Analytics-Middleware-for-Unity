using System.Collections.Generic;
using System.Linq;
using Firebase.Analytics;
using FirebaseAnalytics = Firebase.Analytics.FirebaseAnalytics;
using static FlyingAcorn.Analytics.Constants.ProgressionStatus;
using static FlyingAcorn.Analytics.Constants.ResourceFlowType;

namespace FlyingAcorn.Analytics.Services
{
    public static class ConvertFirebaseAnalyticTypes
    {
        private static readonly Dictionary<FlyingAcornResourceFlowType, string> FirebaseResourceEventName =
            new Dictionary<FlyingAcornResourceFlowType, string>
            {
                { FlyingAcornResourceFlowType.SourceFlow, FirebaseAnalytics.EventEarnVirtualCurrency },
                { FlyingAcornResourceFlowType.SinkFlow, FirebaseAnalytics.EventSpendVirtualCurrency },
                { FlyingAcornResourceFlowType.UndefinedFlow, FlyingAcornResourceFlowType.UndefinedFlow.ToString() },
            };

        private static readonly Dictionary<FlyingAcornProgressionStatus, string> FirebaseProgressionEventName =
            new Dictionary<FlyingAcornProgressionStatus, string>
            {
                { FlyingAcornProgressionStatus.UndefinedLevel, FlyingAcornProgressionStatus.UndefinedLevel.ToString() },
                { FlyingAcornProgressionStatus.StartLevel, FirebaseAnalytics.EventLevelStart },
                { FlyingAcornProgressionStatus.CompleteLevel, FirebaseAnalytics.EventLevelEnd },
                { FlyingAcornProgressionStatus.FailLevel, FirebaseAnalytics.EventLevelEnd }
            };

        internal static List<Parameter> CreateProgressionParameters(FlyingAcornProgressionStatus progressionStatus,
            string levelType, string levelNumber, int score)
        {
            var data = CreateProgressionParameters(progressionStatus, levelType, levelNumber);
            data.Add(new Parameter(FirebaseAnalytics.ParameterScore, score));
            return data;
        }

        internal static List<Parameter> CreateProgressionParameters(FlyingAcornProgressionStatus progressionStatus,
            string levelType, string levelNumber)
        {
            var data = new List<Parameter>
            {
                new Parameter(FirebaseAnalytics.ParameterLevel, levelNumber),
                new Parameter(FirebaseAnalytics.ParameterSuccess,
                    progressionStatus == FlyingAcornProgressionStatus.CompleteLevel ? 1 : 0),
                new Parameter(FirebaseAnalytics.ParameterContentType, levelType)
            };
            return data;
        }

        internal static List<Parameter> CreateResourceParameters(string currency, float amount, string itemType,
            string itemId)
        {
            var data = new List<Parameter>
            {
                new Parameter(FirebaseAnalytics.ParameterVirtualCurrencyName, currency),
                new Parameter(FirebaseAnalytics.ParameterValue, amount),
                new Parameter(FirebaseAnalytics.ParameterItemCategory, itemType),
                new Parameter(FirebaseAnalytics.ParameterItemID, itemId)
            };
            return data;
        }

        internal static List<Parameter> MakeParameters(Dictionary<string, object> customFields)
        {
            return customFields.Select(item => new Parameter(item.Key, item.Value.ToString())).ToList();
        }

        internal static string ProgressionNameConvertor(FlyingAcornProgressionStatus progressionStatus)
        {
            return FirebaseProgressionEventName.TryGetValue(progressionStatus, out var name)
                ? name
                : FlyingAcornProgressionStatus.UndefinedLevel.ToString();
        }

        internal static string ResourceEventNameConvertor(FlyingAcornResourceFlowType eventName)
        {
            return FirebaseResourceEventName.TryGetValue(eventName, out var name)
                ? name
                : FlyingAcornResourceFlowType.UndefinedFlow.ToString();
        }
    }

    public static class IEnumerableExtension
    {
        public static T PickRandom<T>(this IEnumerable<T> list)
        {
            return list.ElementAt(UnityEngine.Random.Range(0, list.Count()));
        }

        public static Parameter[] ConvertToFirebaseParameters(this Dictionary<string, object> dictionary)
        {
            var parameters = new Parameter[dictionary.Count];
            var i = 0;
            foreach (var item in dictionary)
            {
                var value = item.Value;
                parameters[i] = value switch
                {
                    int value1 => new Parameter(item.Key, value1),
                    double d => new Parameter(item.Key, d),
                    string s => new Parameter(item.Key, s),
                    long l => new Parameter(item.Key, l),
                    _ => new Parameter(item.Key, item.Value.ToString())
                };

                i++;
            }

            return parameters;
        }
    }
}