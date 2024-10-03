using Serilog;
using System.Text.RegularExpressions;

namespace eBRestarter.Classes.EBFiles
{
    public sealed class Datainfo
    {


        public static string GetInfoFromFile(string filePath, string matchString, string replaceString)
        {
            string line;
            string matchedString = "";

            try
            {


                using StreamReader file = new(filePath);
                while ((line = file.ReadLine()!) != null)
                {
                    if (Regex.IsMatch(line, matchString))
                    {
                        matchedString = Regex.Replace(line, replaceString, "");
                    }
                }

                file.Close();

                AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromMilliseconds(100));

                return matchedString;
            }
            catch (FileNotFoundException e)
            {
                Log.Error(e, "Es ist eine Ausnahme im Datainfobereich aufgetreten. Logging...");
                return "";
            }
            catch (DirectoryNotFoundException e)
            {
                Log.Error(e, "Es ist eine Ausnahme im Datainfobereich aufgetreten. Logging...");
                return "";
            }
        }
    }
}
