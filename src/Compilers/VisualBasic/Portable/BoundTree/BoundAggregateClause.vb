﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax


Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundAggregateClause
        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return UnderlyingExpression.ExpressionSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ResultKind As LookupResultKind
            Get
                Return UnderlyingExpression.ResultKind
            End Get
        End Property
    End Class

End Namespace
