// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Fhir.Core.Features.Validation.Profiles
{
    public class DefaultResourceProfile
    {
        public DefaultResourceProfile(string resourceType, string profile)
        {
            EnsureArg.IsNotNull(resourceType);

            // TODO: check is this valid resourceType?
            EnsureArg.IsNotNull(profile);

            ResourceType = resourceType;
            Profile = profile;
        }

        public string ResourceType { get; }

        public string Profile { get; }
    }
}
