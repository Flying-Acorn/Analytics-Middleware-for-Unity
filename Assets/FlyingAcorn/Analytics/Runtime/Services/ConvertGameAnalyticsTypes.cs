using System.Collections.Generic;
using GameAnalyticsSDK;
using static FlyingAcorn.Analytics.Constants.ProgressionStatus;
using static FlyingAcorn.Analytics.Constants.ResourceFlowType;
using static FlyingAcorn.Analytics.Constants.ErrorSeverity;

namespace FlyingAcorn.Analytics.Services
{
    public static class ConvertGameAnalyticsTypes
    {
        private static Dictionary<FlyingAcornResourceFlowType, GAResourceFlowType> GameAnalyticsFlowType =
            new Dictionary<FlyingAcornResourceFlowType, GAResourceFlowType>
            {
                { FlyingAcornResourceFlowType.SourceFlow, GAResourceFlowType.Source },
                { FlyingAcornResourceFlowType.SinkFlow, GAResourceFlowType.Sink },
                { FlyingAcornResourceFlowType.UndefinedFlow, GAResourceFlowType.Undefined },
            };

        private static Dictionary<FlyingAcornProgressionStatus, GAProgressionStatus> GameAnalyticsProgression =
            new Dictionary<FlyingAcornProgressionStatus, GAProgressionStatus>
            {
                { FlyingAcornProgressionStatus.UndefinedLevel, GAProgressionStatus.Undefined },
                { FlyingAcornProgressionStatus.StartLevel, GAProgressionStatus.Start },
                { FlyingAcornProgressionStatus.CompleteLevel, GAProgressionStatus.Complete },
                { FlyingAcornProgressionStatus.FailLevel, GAProgressionStatus.Fail }
            };

        private static Dictionary<FlyingAcornNonLevelStatus, GAProgressionStatus> GameAnalyticsNonLevelProgression =
            new Dictionary<FlyingAcornNonLevelStatus, GAProgressionStatus>()
            {
                { FlyingAcornNonLevelStatus.Undefined, GAProgressionStatus.Undefined },
                { FlyingAcornNonLevelStatus.Start, GAProgressionStatus.Start },
                { FlyingAcornNonLevelStatus.Complete, GAProgressionStatus.Complete },
                { FlyingAcornNonLevelStatus.Fail, GAProgressionStatus.Fail },
            };

        private static Dictionary<FlyingAcornErrorSeverity, GAErrorSeverity> GameAnalyticsErrors =
            new Dictionary<FlyingAcornErrorSeverity, GAErrorSeverity>
            {
                { FlyingAcornErrorSeverity.UndefinedSeverity, GAErrorSeverity.Undefined },
                { FlyingAcornErrorSeverity.InfoSeverity, GAErrorSeverity.Info },
                { FlyingAcornErrorSeverity.DebugSeverity, GAErrorSeverity.Debug },
                { FlyingAcornErrorSeverity.WarningSeverity, GAErrorSeverity.Warning },
                { FlyingAcornErrorSeverity.ErrorSeverity, GAErrorSeverity.Error },
                { FlyingAcornErrorSeverity.CriticalSeverity, GAErrorSeverity.Critical }
            };

        public static GAResourceFlowType ConvertorFlowType(FlyingAcornResourceFlowType eventName)
        {
            if (GameAnalyticsFlowType.ContainsKey(eventName))
                return GameAnalyticsFlowType[eventName];
            return GAResourceFlowType.Undefined;
        }

        public static GAProgressionStatus ConvertorProgression(FlyingAcornProgressionStatus eventName)
        {
            if (GameAnalyticsProgression.ContainsKey(eventName))
                return GameAnalyticsProgression[eventName];
            return GAProgressionStatus.Undefined;
        }

        public static GAErrorSeverity ConvertorErrors(FlyingAcornErrorSeverity eventName)
        {
            if (GameAnalyticsErrors.ContainsKey(eventName))
                return GameAnalyticsErrors[eventName];
            return GAErrorSeverity.Undefined;
        }

        public static GAProgressionStatus ConvertorNoneLevelProgression(FlyingAcornNonLevelStatus eventName)
        {
            if (GameAnalyticsNonLevelProgression.ContainsKey(eventName))
                return GameAnalyticsNonLevelProgression[eventName];
            return GAProgressionStatus.Undefined;
        }
    }
}