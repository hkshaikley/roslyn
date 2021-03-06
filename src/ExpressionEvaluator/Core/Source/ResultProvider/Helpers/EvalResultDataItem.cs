// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal enum ExpansionKind
    {
        Default,
        Error,
        NonPublicMembers,
        PointerDereference,
        RawView,
        ResultsView,
        StaticMembers,
        TypeVariables
    }

    /// <summary>
    /// A pair of DkmClrValue and Expansion, used to store
    /// state on a DkmEvaluationResult.  Also computes the
    /// full name of the DkmClrValue.
    /// </summary>
    /// <remarks>
    /// The DkmClrValue is included here rather than directly
    /// on the Expansion so that the DkmClrValue is not kept
    /// alive by the Expansion.
    /// </remarks>
    internal sealed class EvalResultDataItem : DkmDataItem
    {
        public readonly ExpansionKind Kind;
        public readonly string Name;
        public readonly Type TypeDeclaringMember;
        public readonly Type DeclaredType;
        public readonly EvalResultDataItem Parent;
        public readonly DkmClrValue Value;
        public readonly string DisplayValue; // overrides the "Value" text displayed for certain kinds of DataItems (errors, invalid pointer dereferences, etc)...not to be confused with DebuggerDisplayAttribute Value...
        public readonly Expansion Expansion;
        public readonly bool ChildShouldParenthesize;
        public readonly string FullNameWithoutFormatSpecifiers;
        public readonly ReadOnlyCollection<string> FormatSpecifiers;
        public readonly string ChildFullNamePrefix;
        public readonly DkmEvaluationResultCategory Category;
        public readonly DkmEvaluationResultFlags Flags;
        public readonly string EditableValue;

        public string FullName
        {
            get
            {
                var name = this.FullNameWithoutFormatSpecifiers;
                if (name != null)
                {
                    foreach (var formatSpecifier in this.FormatSpecifiers)
                    {
                        name += ", " + formatSpecifier;
                    }
                }
                return name;
            }
        }

        public EvalResultDataItem(string name, string errorMessage)
            : this(
                ExpansionKind.Error,
                inspectionContext: null,
                name: name,
                typeDeclaringMember: null,
                declaredType: null,
                parent: null,
                value: null,
                displayValue: errorMessage,
                expansion: null,
                childShouldParenthesize: false,
                fullName: null,
                childFullNamePrefixOpt: null,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: DkmEvaluationResultFlags.None,
                editableValue: null)
        {
        }

        public EvalResultDataItem(
            ExpansionKind kind,
            string name,
            Type typeDeclaringMember,
            Type declaredType,
            EvalResultDataItem parent,
            DkmClrValue value,
            string displayValue,
            Expansion expansion,
            bool childShouldParenthesize,
            string fullName,
            string childFullNamePrefixOpt,
            ReadOnlyCollection<string> formatSpecifiers,
            DkmEvaluationResultCategory category,
            DkmEvaluationResultFlags flags,
            string editableValue,
            DkmInspectionContext inspectionContext)
        {
            Debug.Assert(name != null);
            Debug.Assert(formatSpecifiers != null);
            Debug.Assert((flags & DkmEvaluationResultFlags.Expandable) == 0);

            this.Kind = kind;
            this.Name = name;
            this.TypeDeclaringMember = typeDeclaringMember;
            this.DeclaredType = declaredType;
            this.Parent = parent;
            this.Value = value;
            this.DisplayValue = displayValue;
            this.ChildShouldParenthesize = childShouldParenthesize;
            this.FullNameWithoutFormatSpecifiers = fullName;
            this.ChildFullNamePrefix = childFullNamePrefixOpt;
            this.FormatSpecifiers = formatSpecifiers;
            this.Category = category;
            this.EditableValue = editableValue;
            this.Flags = flags | GetFlags(value, inspectionContext) | ((expansion == null) ? DkmEvaluationResultFlags.None : DkmEvaluationResultFlags.Expandable);
            this.Expansion = expansion;
        }

        private static DkmEvaluationResultFlags GetFlags(DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            if (value == null)
            {
                return DkmEvaluationResultFlags.None;
            }

            var resultFlags = value.EvalFlags;
            var type = value.Type.GetLmrType();

            if (type.IsBoolean())
            {
                resultFlags |= DkmEvaluationResultFlags.Boolean;
                if (true.Equals(value.HostObjectValue))
                {
                    resultFlags |= DkmEvaluationResultFlags.BooleanTrue;
                }
            }

            if (!value.IsError() && value.HasUnderlyingString(inspectionContext))
            {
                resultFlags |= DkmEvaluationResultFlags.RawString;
            }

            return resultFlags;
        }

        protected override void OnClose()
        {
            Value.Close();
        }
    }
}
