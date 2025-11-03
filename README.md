# Flying Acorn Analytics Middleware

A unified analytics middleware for Unity supporting Firebase, AppMetrica, and GameAnalytics with GDPR compliance and cross-platform support.

## Installation

### Via OpenUPM (Recommended)
The package is available on the [OpenUPM registry](https://openupm.com/packages/com.flyingacorn.analytics-middleware/).

```bash
# Install via OpenUPM CLI
openupm add com.flyingacorn.analytics-middleware
```

Or add `https://package.openupm.com` as a scoped registry in Unity, then search for `com.flyingacorn.analytics-middleware`.

### Via Git URL
Add the following Git URL in Unity's Package Manager:

```
https://github.com/Flying-Acorn/Analytics-Middleware-for-Unity.git?path=Assets/FlyingAcorn/Analytics
```

## Dependencies

### Core
* [Newtonsoft JSON](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html) (automatically included)

### Per Adapter
#### Firebase
* [Google External Dependency Manager](https://github.com/googlesamples/unity-jar-resolver)
* [Google User Messaging Platform](https://github.com/binouze/GoogleUserMessagingPlatform)
* [Firebase Core](https://firebase.google.com/docs/unity/setup)
* [Firebase Analytics](https://firebase.google.com/docs/unity/setup)
* [Firebase Crashlytics](https://firebase.google.com/docs/unity/setup)

#### AppMetrica
* [Yandex AppMetrica](https://appmetrica.yandex.com/docs/en/sdk/unity/analytics/quick-start)

#### GameAnalytics
* [GameAnalytics](https://docs.gameanalytics.com/integrations/sdk/unity/)

## Usage

After installing the package and adding your chosen analytics SDKs, initialize the middleware in your code:

```csharp
using FlyingAcorn.Analytics;
using System.Collections.Generic;

// Optional: Enable debug mode (disable in production)
AnalyticsManager.SetDebugMode(true);

// Optional: Set user identifier
AnalyticsManager.SaveUserIdentifier("custom_user_id");

// Optional: Set GDPR consent
AnalyticsManager.SetGDPRConsent(true);

// Optional: Set target store
AnalyticsManager.SetStore(BuildData.Constants.Store.GooglePlay);

// Initialize with your chosen analytics services
AnalyticsManager.Initialize(new List<IAnalytics>
{
    new GameAnalyticsEvents(),
    new FirebaseEvents(),
    new AppMetricaEvents("YOUR_APPMETRICA_API_KEY")
});
```

See the included Demo scene (`Samples~/DemoInitCall`) for a complete example.