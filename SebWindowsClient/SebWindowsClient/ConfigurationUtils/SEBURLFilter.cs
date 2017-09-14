using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ListObj = System.Collections.Generic.List<object>;
using DictObj = System.Collections.Generic.Dictionary<string, object>;

namespace SebWindowsClient.ConfigurationUtils
{
    class SEBURLFilter
    {
        public Boolean enableURLFilter;
        public Boolean enableContentFilter;
        public ListObj permittedList = new ListObj();
        public ListObj prohibitedList = new ListObj();

        // Updates filter rule arrays with current settings (UserDefaults)
        public void updateFilterRules()
        {
            if (prohibitedList.Count != 0)
            {
                prohibitedList.Clear();
            }

            if (permittedList.Count != 0)
            {
                permittedList.Clear();
            }


            enableURLFilter = (Boolean)SEBSettings.settingsCurrent[SEBSettings.KeyURLFilterEnable];
            enableContentFilter = (Boolean)SEBSettings.settingsCurrent[SEBSettings.KeyURLFilterEnableContentFilter];

            ListObj URLFilterRules = (ListObj)SEBSettings.settingsCurrent[SEBSettings.KeyURLFilterRules];

            foreach (DictObj URLFilterRule in URLFilterRules) {

                if ((Boolean)URLFilterRule["active"] == true)
                {

                    String expressionString = (String)URLFilterRule["expression"];
                    if (String.IsNullOrEmpty(expressionString))
                    {
                        Object expression;

                        Boolean regex = (Boolean)URLFilterRule["regex"];
                        try
                        {
                            if (regex)
                            {
                                expression = new Regex(expressionString, RegexOptions.IgnoreCase);
                            }
                            else
                            {
                                expression = new SEBURLFilterRegexExpression(expressionString);
                            }
                        }
                        catch (Exception)
                        {
                            prohibitedList.Clear();
                            permittedList.Clear();
                            throw;
                        }

                        int action = (int)URLFilterRule["action"];
                        switch (action) {

                            case (int)URLFilterRuleActions.block:

                                prohibitedList.Add(expression);
                                break;


                            case (int)URLFilterRuleActions.allow:

                                permittedList.Add(expression);
                                break;
                }
            }
        }
    }
    
    // Check if Start URL gets allowed by current filter rules and if not add a rule for the Start URL
    String* startURLString = [preferences secureStringForKey: @"org_safeexambrowser_SEB_startURL"];
NSURL* startURL = [NSURL URLWithString: startURLString];
    if ([self testURLAllowed:startURL] != URLFilterActionAllow) {
        // If Start URL is not allowed: Create one using the full Start URL
        id expression = [SEBURLFilterRegexExpression regexFilterExpressionWithString: startURLString error: &error];
        if (error) {
            [self.prohibitedList removeAllObjects];
            [self.permittedList removeAllObjects];
            return error;
        }
        // Add this Start URL filter expression to the permitted filter list
        [self.permittedList addObject:expression];
    }
    
    // Convert these rules and add them to the XULRunner seb keys
    [self createSebRuleLists];
    
}

    // Convert these rules and add them to the XULRunner seb keys
- (void) createSebRuleLists
{
    NSUserDefaults *preferences = [NSUserDefaults standardUserDefaults];

    // Set prohibited rules
    NSString *sebRuleString = [self sebRuleStringForSEBURLFilterRuleList:self.prohibitedList];
    [preferences setSecureString:sebRuleString forKey:@"org_safeexambrowser_SEB_blacklistURLFilter"];

    // Set permitted rules
    sebRuleString = [self sebRuleStringForSEBURLFilterRuleList:self.permittedList];
    [preferences setSecureString:sebRuleString forKey:@"org_safeexambrowser_SEB_whitelistURLFilter"];

    // All rules are regex
    [preferences setSecureBool:YES forKey:@"org_safeexambrowser_SEB_urlFilterRegex"];

    // Set if content filter is enabled
    [preferences setSecureBool:[preferences secureBoolForKey:@"org_safeexambrowser_SEB_URLFilterEnableContentFilter"]
    forKey:@"org_safeexambrowser_SEB_urlFilterTrustedContent"];
}


- (NSString*) sebRuleStringForSEBURLFilterRuleList:(NSMutableArray*) filterRuleList
{
    if (filterRuleList.count == 0) {
        // No rules defined
        return @"";
    }

    id expression;
    NSMutableString *sebRuleString = [NSMutableString new];
    for (expression in filterRuleList) {
        if (expression)
        {

            if ([expression isKindOfClass:[NSRegularExpression class]]) {
                if (sebRuleString.length == 0) {
                    [sebRuleString appendString:[expression pattern]];
                } else {
                    [sebRuleString appendFormat:@";%@", [expression pattern]];
                }
            }
            
            if ([expression isKindOfClass:[SEBURLFilterRegexExpression class]]) {
                if (sebRuleString.length == 0) {
                    [sebRuleString appendString:[expression string]];
                } else {
                    [sebRuleString appendFormat:@";%@", [expression string]];
                }
            }
        }
    }
    
    return [NSString stringWithString:sebRuleString];
}


    }
}
