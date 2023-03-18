using System;
using System.Linq;
using Microsoft.Windows.ApplicationModel.Resources;
using Wavee.UI.Interfaces.Services;

namespace Wavee.UI.WinUI.Services
{
    internal sealed class WinUIResourcesStringLocalizer : IStringLocalizer
    {
        private readonly ResourceMap _resourceLoader;

        public WinUIResourcesStringLocalizer(ResourceMap resourceManager)
        {
            _resourceLoader = resourceManager;
        }

        public string GetValue(string key)
        {
            //keys can be nested, so we need to split them and get the value like /
            bool found = false;
            ResourceMap? resourceMap;
            while (!found)
            {
                //so for example the key might be /Shell/Title
                //Let's first check if the key exists as the whole string
                ResourceCandidate rc = default;
                if ((rc = _resourceLoader.TryGetValue(key)) != null)
                {
                    return rc.ValueAsString;
                }

                //if not we probably have a nested key, so let's split it and try again, and try to get the first subtree
                var splitKey = key.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .ToArray();
                if (splitKey.Length > 1)
                {
                    //if we have a nested key, let's try to get the first subtree
                    var firstSubTree = splitKey[0];
                    if ((resourceMap = _resourceLoader.TryGetSubtree(firstSubTree)) != null)
                    {
                        //if we have a subtree, let's try to get the rest of the key
                        var restOfKey = key.Substring(firstSubTree.Length + 1);
                        if ((rc = resourceMap.TryGetValue(restOfKey.Replace("/", string.Empty))) != null)
                        {
                            return rc.ValueAsString;
                        }
                        else
                        {
                            //if we can't get the rest of the key, let's try to get the next subtree
                            key = restOfKey;
                            continue;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            return null;
        }
    }
}