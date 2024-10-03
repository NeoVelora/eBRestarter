using eBRestarter.Classes.Cache;
using eBRestarter.Classes.OperatingSystem;
using System.Diagnostics;

namespace eBRestarter.Classes.EBTask
{
    public sealed class EBTask
    {

        private readonly WindowsOS windows = new();

        private bool _restartTrigger = false;

        public void RestartClient()
        {
            ThreadStart @start = RestartClientTask;

            var RestartClientThread = new Thread(@start)
            {
                Name = "Restart Client",
                IsBackground = true
            };

            RestartClientThread.Start();
        }

        private void RestartClientTask()
        {
            while (true)
            {

                string restartComputerSetting = SettingsCache.Position["RestartDate"];


                if (!restartComputerSetting.Equals("false"))
                {
                    _restartTrigger = false;

                    while (_restartTrigger == false)
                    {

                        Thread.Sleep(1000);

                        string dateOfRestartFile = SettingsCache.Position["RestartDate"];

                        string restartComputerSetting2 = SettingsCache.Position["RestartComputer"];


                        if (restartComputerSetting2.Equals("false") || dateOfRestartFile.Equals("false"))
                        {
                            _restartTrigger = true;
                        }

                        while (dateOfRestartFile.Equals(DateTime.Today.ToShortDateString()) && _restartTrigger == false)
                        {
                            Thread.Sleep(1000);


                            string restartComputerSetting3 = SettingsCache.Position["RestartComputer"];


                            string clock = SettingsCache.Position["RestartTime"];


                            if (clock.Length == 1 && DateTime.Now.ToString("t").Equals("0" + clock + ":00"))
                            {
                                Thread.Sleep(3000);

                                Process.Start("shutdown", "/r /f /t 0");

                                Environment.Exit(0);
                            }

                            else if (clock.Length == 2 && DateTime.Now.ToString("t").Equals(clock + ":00"))
                            {
                                Thread.Sleep(3000);


                                Process.Start("shutdown", "/r /f /t 0");
                                Environment.Exit(0);
                            }

                            else if (restartComputerSetting3.Equals("false"))
                            {
                                _restartTrigger = true;
                            }
                        }
                    }
                }

                Thread.Sleep(1000);
            }
        }
    }
}
