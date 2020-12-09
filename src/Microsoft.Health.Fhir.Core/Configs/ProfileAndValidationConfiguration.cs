// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Health.Fhir.Core.Features.Validation.Profiles;

namespace Microsoft.Health.Fhir.Core.Configs
{
    /*
     *    "Enabled": true,
            "ValidateOnCreate": true,
            "ValidateOnUpdate": true,
            "ValidateOnDelete": true,
            "DefaultProfiles": [
                {
                    "ResourceType": "Location",
                    "StructureDefinition": "http://hl7.org/fhir/us/core/StructureDefinition/us-core-location"
                },
                {
                    "ResourceType": "Organization",
                    "StructureDefinition": "http://hl7.org/fhir/us/core/StructureDefinition/us-core-organization"
                }
            ],
            "ProfilesLocationPath": "D:/profile/",
    */
    public class ProfileAndValidationConfiguration
    {
        public bool Enabled { get; set; } = false;

        public bool ValidateOnCreate { get; set; } = false;

        public bool ValidateOnUpdate { get; set; } = false;

        public bool ValidateOnDelete { get; set; } = false;

        public IReadOnlyList<DefaultResourceProfile> DefaultProfiles { get; set; } = ImmutableArray<DefaultResourceProfile>.Empty;

        public string ProfilesLocationPath { get; set; } = "./profiles";
    }
}
