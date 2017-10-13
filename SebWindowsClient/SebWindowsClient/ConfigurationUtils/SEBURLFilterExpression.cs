using SebWindowsClient.DiagnosticsUtils;
using System;
using System.Text;
using System.Text.RegularExpressions;


namespace SebWindowsClient.ConfigurationUtils
{
    class SEBURLFilterExpression
    {
        public string scheme;
        public string user;
        public string password;
        public string host;
        public int? port;
        public string path;
        public string query;
        public string fragment;


        public SEBURLFilterExpression(string filterExpressionString)
        {
            if (!string.IsNullOrEmpty(filterExpressionString))
            {
                //int schemeDelimiter = filterExpressionString.IndexOf("://");

                //if (schemeDelimiter != -1)
                //{
                //    // Filter expression contains a scheme: save it and replace it with http
                //    // (in case scheme contains a wildcard)
                //    newScheme = filterExpressionString.Substring(0, schemeDelimiter);
                //    filterExpressionString = "http" + filterExpressionString.Substring(schemeDelimiter);
                //}
                //else
                //{
                //    // Filter expression doesn't contain a scheme followed by an authority part,
                //    // Convert filter expression string to a Uri

                //    // check for scheme followed by only a path (like about:blank or data:...)
                //    // Convert filter expression string to a Uri
                //    Uri URLFromString;
                //    if (!Uri.TryCreate(filterExpressionString, UriKind.Absolute, out URLFromString))
                //    {
                //        // Probably a relative URI without scheme
                //        // Temporary prefix it with a http:// scheme
                //        filterExpressionString = "http://" + filterExpressionString;
                //        newScheme = "";
                //    }
                //    // Convert filter expression string to a Uri
                //    if (!Uri.TryCreate(filterExpressionString, UriKind.Absolute, out URLFromString))
                //    {
                //        return;
                //    }
                //    /*
                //    try
                //    {
                //        newScheme = URLFromString.Scheme;
                //    }
                //    catch (Exception)
                //    {
                //        // Probably a relative URI without scheme
                //        // Temporary prefix it with a http:// scheme
                //        filterExpressionString = "http://" + filterExpressionString;
                //        newScheme = "";
                //    }*/

                //}

                /// Convert Uri to a SEBURLFilterExpression
                string splitURLRegexPattern = @"(?:([^\:]*)\:\/\/)?(?:([^\:\@]*)(?:\:([^\@]*))?\@)?(?:([^\/‌​\:]*))?(?:\:([0-9]*))?([^\?#]*)?(?:\?([^#]*))?(?:#(.*))?";
                Regex splitURLRegex = new Regex(splitURLRegexPattern);
                Match regexMatch = splitURLRegex.Match(filterExpressionString);
                if (regexMatch.Success == false)
                {
                    return;
                }

                /*
                Console.WriteLine(regexMatch.Groups[0].Value + "  (Full URL)<br>");
                Console.WriteLine(regexMatch.Groups[1].Value + "  (Scheme)<br>");
                Console.WriteLine(regexMatch.Groups[2].Value + "  (User)<br>");
                Console.WriteLine(regexMatch.Groups[3].Value + "  (Password)<br>");
                Console.WriteLine(regexMatch.Groups[4].Value + "  (Domain)<br>");
                Console.WriteLine(regexMatch.Groups[5].Value + "  (Port)<br>");
                Console.WriteLine(regexMatch.Groups[6].Value + "  (Path)<br>");
                Console.WriteLine(regexMatch.Groups[7].Value + "  (Query)<br>");
                Console.WriteLine(regexMatch.Groups[8].Value + "  (Fragment)<br>");
                */

                this.scheme = regexMatch.Groups[1].Value;
                this.user = regexMatch.Groups[2].Value;
                this.password = regexMatch.Groups[3].Value;
                this.host = regexMatch.Groups[4].Value;
                string portNumber = regexMatch.Groups[5].Value;

                // We only want a port if the filter expression string explicitely defines one!
                if (portNumber.Length == 0)
                {
                    this.port = null;
                }
                else
                {
                    this.port = UInt16.Parse(portNumber);
                }

                this.path = regexMatch.Groups[6].Value.Trim(new char[] { '/' });
                this.query = regexMatch.Groups[7].Value;
                this.fragment = regexMatch.Groups[8].Value;
            }
        }

        public static string User(string userInfo)
        {
            string user = "";
            if (!string.IsNullOrEmpty(userInfo))
            {
                int userPasswordSeparator = userInfo.IndexOf(":");
                if (userPasswordSeparator == -1)
                {
                    user = userInfo;
                }
                else
                {
                    if (userPasswordSeparator != 0)
                    {
                        user = userInfo.Substring(0, userPasswordSeparator);
                    }
                }
            }
            return user;
        }

        public static string Password(string userInfo)
        {
            string password = "";
            if (!string.IsNullOrEmpty(userInfo))
            {
                int userPasswordSeparator = userInfo.IndexOf(":");
                if (userPasswordSeparator != -1)
                {
                    if (userPasswordSeparator < userInfo.Length - 1)
                    {
                        password = userInfo.Substring(userPasswordSeparator + 1, userInfo.Length - 1 - userPasswordSeparator);
                    }
                }
            }
            return password;
        }

        public SEBURLFilterExpression(string scheme, string user, string password, string host, int port, string path, string query, string fragment)
        {
            this.scheme = scheme;
            this.user = user;
            this.password = password;
            this.host = host;
            this.port = port;
            this.path = path;
            this.query = query;
            this.fragment = fragment;
        }

        

        public override string ToString()
        {
            StringBuilder expressionString = new StringBuilder();
            if (!string.IsNullOrEmpty(this.scheme)) {
                if (!string.IsNullOrEmpty(this.host)) {
                    expressionString.AppendFormat("{0}://", this.scheme);
                } else {
                    expressionString.AppendFormat("{0}:", this.scheme);
                }
            }
            if (!string.IsNullOrEmpty(this.user)) {
                expressionString.Append(this.user);

                if (!string.IsNullOrEmpty(this.password)) {
                    expressionString.AppendFormat(":{0}@", this.password);
                } else {
                    expressionString.Append("@");
                }
            }
            if (!string.IsNullOrEmpty(this.host)) {
                expressionString.Append(this.host);
            }
            if (this.port != null && this.port > 0 && this.port <= 65535) {
                expressionString.AppendFormat(":{0}", this.port);
            }
            if (!string.IsNullOrEmpty(this.path)) {
                if (this.path.StartsWith("/")) {
                    expressionString.Append(this.path);
                } else {
                    expressionString.AppendFormat("/{0}", this.path);
                }
            }
            if (!string.IsNullOrEmpty(this.query)) {
                expressionString.AppendFormat("?{0}", this.query);
            }
            if (!string.IsNullOrEmpty(this.fragment)) {
                expressionString.AppendFormat("#{0}", this.fragment);
            }

            return expressionString.ToString();
        }

    }
}
