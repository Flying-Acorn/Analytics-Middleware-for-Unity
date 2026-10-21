using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace FlyingAcorn.Analytics
{
    public static class Utils
    {
        public static Analytics.BuildData.Constants.Store GetStore()
        {
            var store = AnalyticsPlayerPrefs.Store;
            if (store != Analytics.BuildData.Constants.Store.Unknown)
                return store;
            return Analytics.BuildData.BuildDataUtils.GetBuildStore();
        }

        [UsedImplicitly]
        public static void StartAndroidActivity(string uri, Constants.Intent action, string package)
        {
            if (string.IsNullOrEmpty(package))
                package = Application.identifier;
            if (!Constants.IntentActions.ContainsKey(action))
            {
                MyDebug.LogError($"Intent action {action} is not supported");
                return;
            }

#if UNITY_ANDROID || UNITY_EDITOR
            var intentClass = new AndroidJavaClass("android.content.Intent");
            var intentObject = new AndroidJavaObject("android.content.Intent");

            var uriClass = new AndroidJavaClass("android.net.Uri");

            intentObject.Call<AndroidJavaObject>("setAction",
                intentClass.GetStatic<string>(Constants.IntentActions[action]));
            intentObject.Call<AndroidJavaObject>("setData",
                uriClass.CallStatic<AndroidJavaObject>("parse", uri));
            intentObject.Call<AndroidJavaObject>("setPackage", package);

            var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            currentActivity.Call("startActivity", intentObject);
#endif
        }

        public static string GetEventName(this IAnalytics service, params string[] eventSteps)
        {
            var validatedEventSteps = eventSteps.Clone() as string[];
            if (service.EventStepLengthLimit >= 0)
            {
                for (var index = 0; index < eventSteps.Length; index++)
                {
                    var eventStep = eventSteps[index];
                    if (eventStep.Length > service.EventStepLengthLimit)
                    {
                        WarnCut($"Event step is too long for this provider: {eventStep}, slicing it", eventStep);
                        eventStep = eventStep[..(service.EventStepLengthLimit - 1)];
                    }

                    if (validatedEventSteps != null) validatedEventSteps[index] = eventStep;
                }
            }

            if (validatedEventSteps == null) return null;
            var eventName = string.Join(service.EventSeparator, validatedEventSteps);
            if (service.EventLengthLimit >= 0)
            {
                if (eventName.Length <= service.EventLengthLimit) return eventName;
                // One shorter than the limit allows; cutting at the limit would rename every event cut so far.
                var slicedEventName = eventName[..(service.EventLengthLimit - 1)];
                WarnCut($"Event is too long for this provider: {eventName}, slicing it to {slicedEventName}", eventName);
                return slicedEventName;
            }

            MyDebug.Verbose($"Sending event: {eventName} to {service.GetType().Name}");
            return eventName;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly HashSet<string> WarnedCuts = new();
#endif

        /// <summary>
        /// A cut name can merge with other events into one, so the Editor and development builds warn, once per name;
        /// a release build only logs (a warning there would also go out as an analytics error event).
        /// </summary>
        private static void WarnCut(string message, string name)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (WarnedCuts.Add(name))
                Debug.LogWarning($"[FlyingAcorn Analytics] {message}");
#endif
            MyDebug.Info(message);
        }

        public static void Shuffle<T>(this IList<T> list, int? seed = null)
        {
            var rng = seed != null ? new System.Random((int)seed) : new System.Random();
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}