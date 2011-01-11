using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

// based on
//http://www.codeproject.com/KB/recipes/command_line.aspx

namespace CommandLine.Utility
{
    /// <summary>

    /// Arguments class

    /// </summary>

    public class Arguments
    {
        // Variables

        private StringDictionary Named;
        private StringCollection Unnamed;

        // Constructor

        public Arguments(string[] Args)
        {

            Named = new StringDictionary();
            Unnamed = new StringCollection();

            Regex Spliter = new Regex(@"^-{1,2}|^/|=|:",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            Regex Remover = new Regex(@"^['""]?(.*?)['""]?$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            string Parameter = null;
            string[] Parts;

            // Valid parameters forms:

            // {-,/,--}param{ ,=,:}((",')value(",'))

            // Examples: 

            // value1 --param2 /param3:"Test-:-work" 

            //   /param4=happy -param5 '--=nice=--'

            foreach (string Txt in Args)
            {
                // Look for new parameters (-,/ or --) and a

                // possible enclosed value (=,:)

                Parts = Spliter.Split(Txt, 3);

                switch (Parts.Length)
                {
                    case 1: // Only annamed value
                        // Remove possible enclosing characters (",')
                        Parameter = Remover.Replace(Parts[0], "$1");
                        if (!Named.ContainsKey(Parameter))
                        {
                            Named.Add(Parameter, Parameter);
                            Unnamed.Add(Parameter);
                        }

                        Parameter = null;

                        break;

                    case 2: // With no value, set it to true.

                        if (!Named.ContainsKey(Parts[1]))
                            Named.Add(Parts[1], "true");
                        
                        break;

                    case 3: // Parameter name with value

                        if (!Named.ContainsKey(Parts[1]))
                        {
                            // Remove possible enclosing characters (",')
                            Parts[2] = Remover.Replace(Parts[2], "$1");
                            Named.Add(Parts[1], Parts[2]);
                        }

                        break;
                }
            }
        }

        // Retrieve a parameter value if it exists 

        // (overriding C# indexer property)

        public string this[string Param]
        {
            get
            {
                return (Named[Param]);
            }
        }
        
        public string this[int NParam]
        {
            get
            {
                if (NParam < Unnamed.Count)
                    return (Unnamed[NParam]);
                else
                    return "";
            }
        }

    }
}
