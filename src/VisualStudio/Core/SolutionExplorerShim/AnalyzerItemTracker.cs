// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    /// <summary>
    /// This class listens to selection change events, and tracks which, if any, of our
    /// <see cref="AnalyzerItem"/> or <see cref="AnalyzersFolderItem"/> is selected.
    /// </summary>
    [Export]
    internal class AnalyzerItemsTracker : IVsSelectionEvents
    {
        private IVsMonitorSelection _vsMonitorSelection = null;
        private uint _selectionEventsCookie = 0;

        public event EventHandler SelectedDiagnosticItemsChanged;
        public event EventHandler SelectedHierarchyItemChanged;

        [ImportingConstructor]
        public AnalyzerItemsTracker(SVsServiceProvider serviceProvider)
        {
            _vsMonitorSelection = serviceProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
        }

        public void Register()
        {
            if (_vsMonitorSelection != null)
            {
                _vsMonitorSelection.AdviseSelectionEvents(this, out _selectionEventsCookie);
            }
        }

        public void Unregister()
        {
            if (_vsMonitorSelection != null)
            {
                _vsMonitorSelection.UnadviseSelectionEvents(_selectionEventsCookie);
            }
        }
        public IVsHierarchy SelectedHierarchy { get; private set; }
        public uint SelectedItemId { get; private set; } = VSConstants.VSITEMID_NIL;
        public AnalyzersFolderItem SelectedFolder { get; private set; }
        public ImmutableArray<AnalyzerItem> SelectedAnalyzerItems { get; private set; } = ImmutableArray<AnalyzerItem>.Empty;
        public ImmutableArray<DiagnosticItem> SelectedDiagnosticItems { get; private set; } = ImmutableArray<DiagnosticItem>.Empty;

        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            return VSConstants.S_OK;
        }
        int IVsSelectionEvents.OnSelectionChanged(
            IVsHierarchy pHierOld,
            uint itemidOld,
            IVsMultiItemSelect pMISOld,
            ISelectionContainer pSCOld,
            IVsHierarchy pHierNew,
            uint itemidNew,
            IVsMultiItemSelect pMISNew,
            ISelectionContainer pSCNew)
        {
            IVsHierarchy oldSelectedHierarchy = this.SelectedHierarchy;
            uint oldSelectedItemId = this.SelectedItemId;

            this.SelectedHierarchy = pHierNew;
            this.SelectedItemId = itemidNew;

            var selectedObjects = GetSelectedObjects(pSCNew);

            this.SelectedAnalyzerItems = selectedObjects
                                         .OfType<AnalyzerItem.BrowseObject>()
                                         .Select(b => b.AnalyzerItem)
                                         .ToImmutableArray();

            this.SelectedFolder = selectedObjects
                                  .OfType<AnalyzersFolderItem.BrowseObject>()
                                  .Select(b => b.Folder)
                                  .FirstOrDefault();

            var oldSelectedDiagnosticItems = this.SelectedDiagnosticItems;
            this.SelectedDiagnosticItems = selectedObjects
                                           .OfType<DiagnosticItem.BrowseObject>()
                                           .Select(b => b.DiagnosticItem)
                                           .ToImmutableArray();

            if (!object.ReferenceEquals(oldSelectedHierarchy, this.SelectedHierarchy) ||
                oldSelectedItemId != this.SelectedItemId)
            {
                this.SelectedHierarchyItemChanged?.Invoke(this, EventArgs.Empty);
            }

            if (oldSelectedDiagnosticItems != this.SelectedDiagnosticItems)
            {
                this.SelectedDiagnosticItemsChanged?.Invoke(this, EventArgs.Empty);
            }

            return VSConstants.S_OK;
        }

        private object[] GetSelectedObjects(ISelectionContainer selectionContainer)
        {
            if (selectionContainer == null)
            {
                return SpecializedCollections.EmptyArray<object>();
            }

            uint selectedObjectCount = 0;
            if (selectionContainer.CountObjects((uint)Constants.GETOBJS_SELECTED, out selectedObjectCount) < 0 || selectedObjectCount == 0)
            {
                return SpecializedCollections.EmptyArray<object>();
            }

            object[] selectedObjects = new object[selectedObjectCount];
            if (selectionContainer.GetObjects((uint)Constants.GETOBJS_SELECTED, selectedObjectCount, selectedObjects) < 0)
            {
                return SpecializedCollections.EmptyArray<object>();
            }

            return selectedObjects;
        }
    }
}
