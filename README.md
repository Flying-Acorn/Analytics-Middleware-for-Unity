## Dependencies:
### Core
* [newtonsoft-json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html)
### Per Adapter
#### Firebase
* [GoogleExternalDependencyManager](https://github.com/googlesamples/unity-jar-resolver)
* [GoogleUserMessagingPlatform](https://github.com/binouze/GoogleUserMessagingPlatform)
* [FirebaseCore](https://firebase.google.com/docs/unity/setup)
* [FirebaseAnalytics](https://firebase.google.com/docs/unity/setup)
* [FirebaseCrashlytics](https://firebase.google.com/docs/unity/setup)
#### AppMetrica
* [Yandex AppMetrica](https://appmetrica.yandex.com/docs/en/sdk/unity/analytics/quick-start)
#### GameAnalytics
* [GameAnalytics](https://docs.gameanalytics.com/integrations/sdk/unity/)


## Installation
After adding each adapter from the Release section, you need to add pass it to the AnalyticsManager in the Initialize method.
Here is an example of how to add FirebaseAnalytics:
```csharp 
// DemoInitCall.cs
AnalyticsManager.SetDebugMode(true); // Optional, turn off in production
AnalyticsManager.SaveUserIdentifier(customUserId); // Optional
AnalyticsManager.Initialize(new List<IAnalytics> // Mandatory
{
    new GameAnalyticsEvents(),
    new FirebaseEvents(),
    new AppMetricaEvents("KEY")
});
```
See DemoInitCall scene within Demo folder for more details.