# Analytics Middleware for Unity

**One analytics API for your Unity game — send an event once, and Firebase, AppMetrica, and GameAnalytics all receive it.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Unity 2022.3+](https://img.shields.io/badge/Unity-2022.3%2B-black.svg?logo=unity)](https://unity.com/)
[![Latest release](https://img.shields.io/github/v/release/Flying-Acorn/Analytics-Middleware-for-Unity?label=release)](https://github.com/Flying-Acorn/Analytics-Middleware-for-Unity/releases)

Stop writing the same event three times. This middleware wraps
[Firebase Analytics](https://firebase.google.com/docs/unity/setup),
[Yandex AppMetrica](https://appmetrica.yandex.com/docs/en/sdk/unity/analytics/quick-start), and
[GameAnalytics](https://docs.gameanalytics.com/integrations/sdk/unity/) behind a single static API, and normalizes
the differences between them (event-name length limits, separators, enum names, revenue tracking rules).

## Why

| | Without middleware | With this package |
|---|---|---|
| Sending one event | Three SDK calls, three formats | `AnalyticsManager.DesignEvent("level", "start", "1")` |
| Name limits | GameAnalytics truncates at 32 chars/step, Firebase at 40 | Handled per adapter automatically |
| A dead SDK | One exception kills the rest of your tracking | Each adapter is isolated; failures are logged, others keep working |
| Swapping providers | Refactor every call site | Change one list at init |
| QA sessions | Test purchases pollute production revenue | Debug mode drops business/resource events |

## Features

- **Unified API** — design, progression, business, resource, error, sign-up, and user-segmentation events.
- **Pluggable adapters** — pass any list of `IAnalytics` implementations; implement the interface to add your own SDK.
- **Fault isolation** — a provider that throws (missing Play Services, unavailable network SDK) never blocks the others.
- **Automatic session events** — `FA_session` first/start/pause/unpause/end, plus session counting.
- **GDPR consent + custom user IDs** — set once, propagated to every adapter.
- **Store awareness** — Google Play, App Store, Cafe Bazaar, Myket, GitHub, and more, enforced at build time.
- **Build metadata** — build number, scripting backend, and build time captured into a `Resources` asset and attached to events.
- **Iran-store friendly** — manual revenue tracking for Bazaar/Myket, automatic for official stores.

## Requirements

- Unity **2022.3** or newer
- [Newtonsoft JSON for Unity](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html) (`com.unity.nuget.newtonsoft-json`)
- At least one provider SDK (see [Adapters](#adapters))

## Install

Download the `.unitypackage` for your setup from the [Releases page](https://github.com/Flying-Acorn/Analytics-Middleware-for-Unity/releases) and import it:

| Package | Contains |
|---|---|
| `…-Base` | Core middleware only — bring your own adapter |
| `…-Firebase` | Core + Firebase adapter |
| `…-AppMetrica` | Core + AppMetrica adapter |
| `…-GameAnalytics` | Core + GameAnalytics adapter |

Then install that provider's SDK (below). Importing an adapter without its SDK will not compile.

## Adapters

<details>
<summary><b>Firebase</b></summary>

- [Google External Dependency Manager](https://github.com/googlesamples/unity-jar-resolver)
- [Google User Messaging Platform](https://github.com/binouze/GoogleUserMessagingPlatform) (consent UI)
- [Firebase Core, Analytics, and Crashlytics](https://firebase.google.com/docs/unity/setup)

Event names are truncated to 40 characters; steps are joined with `_`.
</details>

<details>
<summary><b>AppMetrica</b></summary>

- [Yandex AppMetrica for Unity](https://appmetrica.yandex.com/docs/en/sdk/unity/analytics/quick-start) (`io.appmetrica.analytics` via OpenUPM)

Requires your AppMetrica API key at construction. Steps are joined with `_`, no length limit.
</details>

<details>
<summary><b>GameAnalytics</b></summary>

- [GameAnalytics Unity SDK](https://docs.gameanalytics.com/integrations/sdk/unity/) (`com.gameanalytics.sdk` via OpenUPM)

Steps are joined with `:`, each step truncated to 32 characters, max 5 steps.
</details>

## Quick start

```csharp
using System.Collections.Generic;
using FlyingAcorn.Analytics;

// Optional — call all of these BEFORE Initialize
AnalyticsManager.SetDebugMode(true);                 // verbose logs; drops business/resource events
AnalyticsManager.SaveUserIdentifier("custom_user_id");
AnalyticsManager.SetGDPRConsent(true);
AnalyticsManager.SetStore(BuildData.Constants.Store.GooglePlay);

AnalyticsManager.Initialize(new List<IAnalytics>
{
    new Services.GameAnalyticsEvents(),
    new Services.FirebaseEvents(),
    new Services.AppMetricaEvents("YOUR_APPMETRICA_API_KEY"),
});
```

`Initialize` creates a persistent `AnalyticsManager` GameObject, fires the session events, and is safe to call once
per app launch — subsequent calls are ignored. Subscribe to `AnalyticsManager.OnInitCalled` if other systems need to
wait for it.

A working scene lives in `Assets/FlyingAcorn/Analytics/Demo/DemoInitCall.unity`.

## Sending events

```csharp
// Design — 1 to 5 steps, joined per provider ("level:start:1" or "level_start_1")
AnalyticsManager.DesignEvent(new[] { "level", "start", "1" });
AnalyticsManager.DesignEvent(new Dictionary<string, object> { ["mode"] = "hard" }, "level", "start", "1");

// Progression
AnalyticsManager.ProgressionEvent(
    Constants.ProgressionStatus.FlyingAcornProgressionStatus.CompleteLevel, "world_1", "12", score: 4200);

// Resource — soft/hard currency in and out
AnalyticsManager.ResourceEvent(
    Constants.ResourceFlowType.FlyingAcornResourceFlowType.SinkFlow, "coins", 50f, "continue", "shop");

// Business — real-money purchase
AnalyticsManager.BusinessEvent("USD", 4.99m, "gems", "gem_pack_1", "shop",
    Constants.PaymentSDK.CafeBazaar, receipt: null, customData: null);

// Errors, sign-ups, segmentation
AnalyticsManager.ErrorEvent(Constants.ErrorSeverity.FlyingAcornErrorSeverity.WarningSeverity, "save failed");
AnalyticsManager.SignUpEvent("google");
AnalyticsManager.UserSegmentation("spender_tier", "whale", dimension: 1);
```

**Revenue note:** for Google Play and the App Store, prefer each SDK's automatic purchase tracking — adapters skip
manual business events when automatic tracking is on, to avoid double counting. Alternative stores (Cafe Bazaar,
Myket, and other `PaymentSDK.Other` flows) are always tracked manually.

## Build settings

Open **FlyingAcorn → Build Settings → Open or Create** to create `Assets/Resources/FA_Build_Settings.asset`.

| Field | Meaning |
|---|---|
| `StoreName` | Target store baked into the build |
| `EnforceStoreOnBuild` | Apply `StoreName` automatically at build time (recommended over calling `SetStore`) |
| `PreserveStoreAfterBuild` | Keep the enforced store in project settings after the build finishes |

Build number, scripting backend, and build time are filled in automatically and are readable at runtime through
`BuildDataUtils`.

## Debug mode

`AnalyticsManager.SetDebugMode(true)` lowers the log level to verbose **and** suppresses business and resource
events, so QA sessions don't contaminate revenue and economy dashboards. Turn it off for production builds.

Log level alone can be tuned with `MyDebug.SetLogLevel(...)`.

## Writing your own adapter

Implement `IAnalytics`, declare your provider's `EventSeparator`, `EventLengthLimit`, and `EventStepLengthLimit`, then
pass an instance to `Initialize`. `Utils.GetEventName(this, steps)` applies your limits for you. Set `IsInitialized`
to `true` once your SDK is ready — the provider only forwards events to initialized adapters.

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) © Flying Acorn
