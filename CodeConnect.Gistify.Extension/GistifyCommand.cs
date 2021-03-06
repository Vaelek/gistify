﻿//------------------------------------------------------------------------------
// <copyright file="GistifyCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using CodeConnect.Gistify.Extension.Integrations;
using CodeConnect.Gistify.Engine;

namespace CodeConnect.Gistify.Extension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GistifyCommand
    {
        #region Boilerplate

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;
        public const int ContextCommandId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("21c9423a-0f42-456a-a54d-7e37d4c01e9f");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GistifyCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private GistifyCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this._package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
                var contextMenuCommandID = new CommandID(CommandSet, ContextCommandId);
                var contextMenuItem = new MenuCommand(this.MenuItemCallback, contextMenuCommandID);
                commandService.AddCommand(contextMenuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GistifyCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this._package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new GistifyCommand(package);
        }

        #endregion

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            IVsTextManager textManager = (IVsTextManager)ServiceProvider.GetService(typeof(SVsTextManager));
            int startPosition, endPosition;
            string filePath;
            if (textManager.TryFindDocumentAndPosition(out filePath, out startPosition, out endPosition))
            {
                if (endPosition == startPosition)
                {
                    StatusBar.ShowStatus($"To gistify, select a snippet of C# code first.");
                    return;
                }
                Microsoft.CodeAnalysis.Document document;
                try
                {
                    document = VSIntegration.GetDocument(filePath);
                }
                catch (NullReferenceException ex)
                {
                    StatusBar.ShowStatus($"Error accessing the document. Try building the solution.");
                    return;
                }
                var augmentedSnippet = CodeAnalyzer.AugmentSelection(document, startPosition, endPosition);
                handleAugmentedSnippet(augmentedSnippet, System.IO.Path.GetFileName(filePath));
            }
            else
            {
                StatusBar.ShowStatus("To gistify, select a snippet of C# code first.");
            }
        }

        private void handleAugmentedSnippet(string augmentedSnippet, string fileName)
        {
            switch (Options.SavedOptions.Instance.DefaultActionValue)
            {
                case Options.SavedOptions.DefaultAction.CopyToClipboard:
                    ClipboardIntegration.HandleAugmentedSnippet(augmentedSnippet);
                    break;
                case Options.SavedOptions.DefaultAction.UploadAsGist:
                    GitHubIntegration.HandleAugmentedSnippet(augmentedSnippet, fileName);
                    break;
            }
        }
    }
}
