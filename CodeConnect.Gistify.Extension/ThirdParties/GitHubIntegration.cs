﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using System.ComponentModel;

namespace CodeConnect.Gistify.Extension.ThirdParties
{
    static class GitHubIntegration
    {
        public static void HandleAugmentedSnippet(string snippet)
        {
            Task.Run(async  () =>
            {
                var gistUrl = await createGistAsync(snippet);
                goToGist(gistUrl);
            });
        }

        private static void goToGist(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                StatusBar.ShowStatus($"No link provided");
                return;
            }
            try
            {
                System.Diagnostics.Process.Start(url);
                StatusBar.ShowStatus($"Gist saved in {url}");
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

        private static async Task<string> createGistAsync(string snippet)
        {
            try
            {
                string password = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER", "gitPassword", "invalid").ToString();
                var credentials = new Octokit.Credentials("git@amadeusw.com", password);

                var connection = new Connection(new ProductHeaderValue("Whatever"))
                {
                    Credentials = credentials
                };
                var github = new GitHubClient(connection);
                var newGist = new NewGist()
                {
                    Description = "Generated by Code Connect's Gistify",
                    Public = false,
                };
                newGist.Files.Add("fragment.cs", snippet);
                var gist = await github.Gist.Create(newGist).ConfigureAwait(false);
                return gist.HtmlUrl;
            }
            catch (Exception)
            {
                StatusBar.ShowStatus("Gistify ran into a problem creating the gist.");
                return String.Empty;
            }
        }
    }
}