namespace eBRestarter.Classes.EBScheduler
{
    public sealed class CountdownVisuals
    {

        private delegate string? ShowCurrentCountdownTimerHourMinutesSeconds(int hoursLeft, int minutesLeft, int seconds_left);
        private delegate string? ShowCurrentCountdownTimerMinutesSeconds(int minutesLeft, int seconds_left);
        private delegate string? ShowCurrentCountdownTimerSeconds(int seconds_left);

        private readonly ShowCurrentCountdownTimerHourMinutesSeconds ShowHourMinutesSecondsCount = (hoursLeft, minutesLeft, seconds_left) => hoursLeft + "h: " + minutesLeft + "m: " + seconds_left + "s";
        private readonly ShowCurrentCountdownTimerMinutesSeconds ShowMinutesSecondsCount = (minutesLeft, seconds_left) => minutesLeft + "m: " + seconds_left + "s";
        private readonly ShowCurrentCountdownTimerSeconds ShowSecondsCount = (seconds_left) => seconds_left + "s";

        private delegate int CalculateTime(int seconds, int mod);

        private readonly CalculateTime CalculateTimeSequences = (seconds, mod) => seconds % mod;

        private int _seconds;

        public int SetSeconds { set => _seconds = value; }
        public int GetSeconds { get => _seconds; }


        public string ShowCurrentTime()
        {
            if (GetSeconds >= 0 && GetSeconds <= 60)
            {
                return GetTimeSeconds();

            }
            else if (GetSeconds >= 61 && GetSeconds <= 3600)
            {
                return GetTimeMinutesAndSeconds();

            }
            else
            {
                return GetTimeHourAndMinutesAndSeconds();
            }
        }


        public string GetTimeHourAndMinutesAndSeconds()
        {

            int seconds_left = CalculateTimeSequences(GetSeconds, 60);

            int minutes = (GetSeconds - seconds_left) / 60;


            int minutesLeft = CalculateTimeSequences(minutes, 60);

            int hours = GetSeconds / 60 / 60;


            int hoursLeft = CalculateTimeSequences(hours, 60);


            --_seconds;

            return ShowHourMinutesSecondsCount(hoursLeft, minutesLeft, seconds_left)!;
        }

        public string GetTimeMinutesAndSeconds()
        {
            int seconds_left = CalculateTimeSequences(GetSeconds, 60);

            int minutes = (GetSeconds - seconds_left) / 60;
            int minutesLeft = CalculateTimeSequences(minutes, 60);

            --_seconds;

            return ShowMinutesSecondsCount(minutesLeft, seconds_left)!;
        }

        public string GetTimeSeconds()
        {
            int seconds_left = CalculateTimeSequences(GetSeconds, 60);

            --_seconds;

            return ShowSecondsCount(seconds_left)!;
        }
    }
}
