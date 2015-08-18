﻿using CodeConnect.Gistify.Extension.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeConnect.Gistify.Extension
{
    class GistifyEngine
    {
        internal static string PrepareGist(string filePath, int startPosition, int endPosition)
        {
            var gist = CodeAnalyzer.GetGistFromSnippet(startPosition, endPosition, filePath);
            var gistUrl = Task.Run(() =>
            {
                var github = new ThirdParties.GitHubConnection();
                return github.CreateGist(gist);
            }).Result;
            return gistUrl;
        }

        internal static void GoToGist(string target)
        {
            if (String.IsNullOrWhiteSpace(target))
            {
                StatusBar.ShowStatus($"No link provided");
                return;
            }
            try
            {
                System.Diagnostics.Process.Start(target);
                StatusBar.ShowStatus($"Gist saved in {target}");
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                {
                    StatusBar.ShowStatus($"Unable to open a web browser.");
                }
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
            }
            catch
            {
                StatusBar.ShowStatus($"Gistify ran into an error.");
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif
            }
        }
    }
}
