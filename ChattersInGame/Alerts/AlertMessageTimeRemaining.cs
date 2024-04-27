using System;

namespace ChattersInGame.Alerts
{
    public class AlertMessageTimeRemaining : AlertMessage
    {
        public readonly string FutureFormat;
        public readonly string PastFormat;

        public readonly TimeStamp Time;

        public AlertMessageTimeRemaining(string inFutureFormat, string inPastFormat, TimeStamp time)
        {
            FutureFormat = inFutureFormat;
            PastFormat = inPastFormat;
            Time = time;
        }

        public override string ConstructAlertString()
        {
            TimeSpan timeRemaining = Time.TimeUntil;

            bool timeHasPassed = timeRemaining < TimeSpan.Zero;
            if (timeHasPassed)
                timeRemaining = timeRemaining.Negate();

            string timeRemainingString;
            if (timeRemaining.TotalDays >= 1)
            {
                timeRemainingString = $"{timeRemaining.TotalDays:0.0} day(s)";
            }
            else if (timeRemaining.TotalHours >= 1)
            {
                timeRemainingString = $"{timeRemaining.TotalHours:0.0} hour(s)";
            }
            else if (timeRemaining.TotalMinutes >= 1)
            {
                timeRemainingString = $"{timeRemaining.TotalMinutes:0.0} minute(s)";
            }
            else
            {
                timeRemainingString = $"{timeRemaining.TotalSeconds} second(s)";
            }

            return string.Format(timeHasPassed ? PastFormat : FutureFormat, timeRemainingString);
        }
    }
}
