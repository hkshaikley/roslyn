﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Provides services for reading and writing options.
    /// </summary>
    internal interface IOptionService : IWorkspaceService
    {
        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T GetOption<T>(Option<T> option);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T GetOption<T>(PerLanguageOption<T> option, string languageName);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        object GetOption(OptionKey optionKey);

        /// <summary>
        /// Fetches an immutable set of all current options.
        /// </summary>
        OptionSet GetOptions();

        /// <summary>
        /// Applies a set of options.
        /// </summary>
        void SetOptions(OptionSet optionSet);

        /// <summary>
        /// Returns the set of all registered options.
        /// </summary>
        IEnumerable<IOption> GetRegisteredOptions();

        event EventHandler<OptionChangedEventArgs> OptionChanged;
    }
}
