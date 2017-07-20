using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
namespace SebWindowsClient.ConfigurationUtils
{
    public class SEBURLFilterRegexExpression
    {
        string scheme;
        string user;
        string password;
        string host;
        Int16 port;
        string path;
        string query;
        string fragment;

    }

+ (SEBURLFilterRegexExpression*) regexFilterExpressionWithString:(NSString*) filterExpressionString error:(NSError**) error
    {
        SEBURLFilterRegexExpression *filterExpression = [SEBURLFilterRegexExpression new];
        SEBURLFilterExpression *URLFromString = [SEBURLFilterExpression filterExpressionWithString:filterExpressionString];

        filterExpression.scheme = [self regexForFilterString:URLFromString.scheme error:error];
        filterExpression.user = [self regexForFilterString:URLFromString.user error:error];
        filterExpression.password = [self regexForFilterString:URLFromString.password error:error];
        filterExpression.host = [self regexForHostFilterString:URLFromString.host error:error];
        filterExpression.port = URLFromString.port;
        filterExpression.path = [self regexForPathFilterString:URLFromString.path error:error];
        filterExpression.query = [self regexForFilterString:URLFromString.query error:error];
        filterExpression.fragment = [self regexForFilterString:URLFromString.fragment error:error];
    
    return filterExpression;
    }


+ (NSRegularExpression*) regexForFilterString:(NSString*) filterString error:(NSError**) error
    {
    if (filterString.length == 0) {

            return nil;

        } else {
            NSString* regexString = [NSRegularExpression escapedPatternForString: filterString];
            regexString = [regexString stringByReplacingOccurrencesOfString: @"\\*" withString: @".*?"];
            // Add regex command characters for matching at start and end of a line (part)
            regexString = [NSString stringWithFormat: @"^%@$", regexString];
            NSRegularExpression* regex = [NSRegularExpression regularExpressionWithPattern: regexString options: NSRegularExpressionCaseInsensitive | NSRegularExpressionAnchorsMatchLines error: error];
            return regex;
        }
    }


+ (NSRegularExpression*) regexForHostFilterString:(NSString*) filterString error:(NSError**) error
    {
    if (filterString.length == 0) {

            return nil;

        } else {
            // Check if host string has a dot "." prefix to disable subdomain matching
            if (filterString.length > 1 && [filterString hasPrefix: @"."])
            {
                // Get host string without the "." prefix
                filterString = [filterString substringFromIndex: 1];
                // Get regex for host <*://example.com> (without possible subdomains)
                return [self regexForFilterString: filterString error: error];
            }
            // Allow subdomain matching: Create combined regex for <example.com> and <*.example.com>
            NSString* regexString = [NSRegularExpression escapedPatternForString: filterString];
            regexString = [regexString stringByReplacingOccurrencesOfString: @"\\*" withString: @".*?"];
            // Add regex command characters for matching at start and end of a line (part)
            regexString = [NSString stringWithFormat: @"^((%@)|(.*?\\.%@))$", regexString, regexString];
            NSRegularExpression* regex = [NSRegularExpression regularExpressionWithPattern: regexString options: NSRegularExpressionCaseInsensitive | NSRegularExpressionAnchorsMatchLines error: error];
            return regex;
        }
    }


+ (NSRegularExpression*) regexForPathFilterString:(NSString*) filterString error:(NSError**) error
    {
        // Trim a possible trailing slash "/", we will instead add a rule to also match paths to directories without trailing slash
        filterString = [filterString stringByTrimmingCharactersInSet:[NSCharacterSet characterSetWithCharactersInString:@"/"]];
    
    if (filterString.length == 0) {

            return nil;

        } else {
            // Check if path string ends with a "/*" for matching contents of a directory
            if ([filterString hasSuffix: @"/*"])
            {
                // As the path filter string matches for a directory, we need to add a string to match directories without trailing slash

                // Get path string without the "/*" suffix
                NSString* filterStringDirectory = [filterString substringToIndex: filterString.length - 2];

                NSString* regexString = [NSRegularExpression escapedPatternForString: filterString];
                regexString = [regexString stringByReplacingOccurrencesOfString: @"\\*" withString: @".*?"];

                NSString* regexStringDir = [NSRegularExpression escapedPatternForString: filterStringDirectory];
                regexStringDir = [regexStringDir stringByReplacingOccurrencesOfString: @"\\*" withString: @".*?"];

                // Add regex command characters for matching at start and end of a line (part)
                regexString = [NSString stringWithFormat: @"^((%@)|(%@))$", regexString, regexStringDir];

                NSRegularExpression* regex = [NSRegularExpression regularExpressionWithPattern: regexString options: NSRegularExpressionCaseInsensitive | NSRegularExpressionAnchorsMatchLines error: error];
                return regex;
            }
            else
            {
                return [self regexForFilterString: filterString error: error];
            }
        }
    }


- (NSString*) string
{
    NSMutableString* expressionString = [NSMutableString new];
    NSString* part;
    [expressionString appendString:@"^"];
    
    /// Scheme
    if (_scheme) {
        // If there is a regex filter for scheme
        // get stripped regex pattern
        part = [self stringForRegexFilter:_scheme];
    } else {
        // otherwise use the regex wildcard pattern for scheme
        part = @".*?";
    }
    [expressionString appendFormat:@"%@:\\/\\/", part];

    /// User/Password
    if (_user) {
        part = [self stringForRegexFilter:_user];

        [expressionString appendString:part];
        
        if (_password) {
            [expressionString appendFormat:@":%@@", [self stringForRegexFilter:_password]];
        } else {
            [expressionString appendString:@"@"];
        }
    }
    
    /// Host
    NSString* hostPort = @"";
    if (_host) {
        hostPort = [self stringForRegexFilter:_host];
    }
    
    /// Port
    if (_port && (_port.integerValue > 0) && (_port.integerValue <= 65535)) {
        hostPort = [NSString stringWithFormat:@"%@:%@", hostPort, _port.stringValue];
    }
    
    // When there is a host, but no path
    if (_host && !_path) {
        hostPort = [NSString stringWithFormat:@"((%@)|(%@\\/.*?))", hostPort, hostPort];
    }
    
    [expressionString appendString:hostPort];

    /// Path
    if (_path) {
        NSString* path = [self stringForRegexFilter: _path];
        if ([path hasPrefix:@"\\/"]) {
            [expressionString appendString:path];
        } else {
            [expressionString appendFormat:@"\\/%@", path];
        }
    }
    
    /// Query
    if (_query) {
        [expressionString appendFormat:@"\\?%@", [self stringForRegexFilter:_query]];
    }
    
    /// Fragment
    if (_fragment) {
        [expressionString appendFormat:@"#%@", [self stringForRegexFilter:_fragment]];
    }
    
    [expressionString appendString:@"$"];

    return expressionString;
}


- (NSString*) stringForRegexFilter:(NSRegularExpression*) regexFilter
{
    // Get pattern string from regular expression
    NSString *regexPattern = [regexFilter pattern];
    if (regexPattern.length <= 2) {
        return @"";
    }
    // Remove the regex command characters for matching at start and end of a line
    regexPattern = [regexPattern substringWithRange:NSMakeRange(1, regexPattern.length - 2)];
    return regexPattern;
}


}
*/