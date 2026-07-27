# Contributing

Thanks for helping out. Issues and pull requests are both welcome.

## Getting set up

1. Clone the repo and open it with **Unity 2022.3.62f3** (or a newer 2022.3 patch).
2. Packages resolve from `Packages/manifest.json`; Firebase comes from the local `.tgz` files in `GooglePackages/`.
3. Open `Assets/FlyingAcorn/Analytics/Demo/DemoInitCall.unity` and press Play to verify events flow.

## Making changes

- Match the surrounding code style: `FlyingAcorn.Analytics` namespaces, XML doc comments on public API.
- Adapter code belongs in `Assets/FlyingAcorn/Analytics/Services/`; anything cross-provider goes in the core.
- Never call `MyDebug` from `AnalyticsManager.ErrorEvent` or inside an adapter's error path — it recurses.
- Adapters must not throw out of an event call. If a provider can fail, catch it and log with `UnityEngine.Debug`.
- If you add a public API to `IAnalytics`, implement it in **all** adapters and in `AnalyticServiceProvider`.

## Adding a new provider

Implement `IAnalytics`, set `EventSeparator` / `EventLengthLimit` / `EventStepLengthLimit` to your SDK's real limits,
use `Utils.GetEventName(this, steps)` to build names, and flip `IsInitialized` only once the SDK is ready.

## Pull requests

- One logical change per PR, with a short description of what and why.
- Confirm the demo scene still runs and the project compiles for Android and iOS.
- Note any change to event names or payload shape — those break existing dashboards.

## Reporting issues

Include your Unity version, the adapters in use, the target platform/store, and the relevant console output.
