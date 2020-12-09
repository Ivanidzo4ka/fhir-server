// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Validation.Profiles
{
    public interface IProfileValidator
    {
        bool TryValidate(ITypedElement element, bool resolveReferences, string profileUrl, out OperationOutcomeIssue[] outcomeIssues);
    }
}
