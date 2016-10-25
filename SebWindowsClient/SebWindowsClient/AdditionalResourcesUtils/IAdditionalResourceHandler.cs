using System.Collections.Generic;

namespace SebWindowsClient.AdditionalResourcesUtils
{
    public interface IAdditionalResourceHandler
    {
        void OpenAdditionalResourceById(string id);
        void OpenAdditionalResource(Dictionary<string, object> resource);
    }
}
