using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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
            // Check if filter expression contains a scheme
            string newScheme = "";
            Uri URLFromString = null;

            if (!string.IsNullOrEmpty(filterExpressionString))
            {
                int schemeDelimiter = filterExpressionString.IndexOf("://");

                if (schemeDelimiter != -1)
                {
                    // Filter expression contains a scheme: save it and replace it with http
                    // (in case scheme contains a wildcard)
                    newScheme = filterExpressionString.Substring(0, schemeDelimiter);
                    filterExpressionString = "http" + filterExpressionString.Substring(schemeDelimiter);
                    // Convert filter expression string to a NSURL
                    if (!Uri.TryCreate(filterExpressionString, UriKind.RelativeOrAbsolute, out URLFromString))
                    {
                        return;
                    }
                }
                else
                {
                    // Filter expression doesn't contain a scheme followed by an authority part,
                    // check for scheme followed by only a path (like about:blank or data:...)
                    // Convert filter expression string to a NSURL
                    if (!Uri.TryCreate(filterExpressionString, UriKind.RelativeOrAbsolute, out URLFromString))
                    {
                        return;
                    }
                    try
                    {
                        newScheme = URLFromString.Scheme;
                    }
                    catch (Exception)
                    {
                        // Probably a relative URI without scheme
                        // Temporary prefix it with a http:// scheme
                        filterExpressionString = "http" + filterExpressionString;
                        // Convert filter expression string to a NSURL
                        if (!Uri.TryCreate(filterExpressionString, UriKind.RelativeOrAbsolute, out URLFromString))
                        {
                            return;
                        }
                    }
                }

                /// Convert NSURL to a SEBURLFilterExpression
                // Use the saved scheme instead of the temporary http://

                this.scheme = newScheme;
                string userInfo = URLFromString.UserInfo;
                if (!string.IsNullOrEmpty(userInfo))
                {
                    int userPasswordSeparator = userInfo.IndexOf(":");
                    if (userPasswordSeparator == -1)
                    {
                        this.user = userInfo;
                    }
                    else
                    {
                        if (userPasswordSeparator != 0)
                        {
                            this.user = userInfo.Substring(0, userPasswordSeparator);
                        }
                        if (userPasswordSeparator < userInfo.Length - 1)
                        {
                            this.password = userInfo.Substring(userPasswordSeparator + 1, userInfo.Length - 1 - userPasswordSeparator);
                        }
                    }
                }
                this.host = URLFromString.Host;
                this.port = URLFromString.Port;
                this.path = URLFromString.AbsolutePath.TrimEnd(new char[] { '/' });
                this.query = URLFromString.Query;
                this.fragment = URLFromString.Fragment;
            }
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

        

        public string String()
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
