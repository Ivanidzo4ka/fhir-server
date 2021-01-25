// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Core.Features.Validation.Profiles
{
    public interface IProfileValidator
    {
        /// <summary>
        /// Validate element to profile, and throw <see cref="ProfileValidationFailedException"/> if element is not valid.
        /// </summary>
        /// <param name="element">Element to validate.</param>
        /// <param name="profileUrl">Profile url to check. If <see langword="null"/>> we will validate according to meta profiles in element.</param>
        void Validate(ITypedElement element, string profileUrl = null);
    }
}
