// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Validation.Profiles;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.Shared.Core.Operations.Validate;

// TODO: Move me to Feature.Validation folder.
namespace Microsoft.Health.Fhir.Core.Features.Operations.Validate
{
    public class ProfileValidator : IProfileValidator
    {
        private readonly IResourceResolver _resolver;
        private readonly ProfileAndValidationConfiguration _configuration;
        private readonly Dictionary<string, string> defaultProfiles = new Dictionary<string, string>();

        public ProfileValidator(IOptions<ProfileAndValidationConfiguration> configuration)
        {
            EnsureArg.IsNotNull(configuration?.Value, nameof(configuration));
            _configuration = configuration.Value;
            if (_configuration.Enabled)
            {
                foreach (var profile in _configuration.DefaultProfiles)
                {
                    // ignore dups for now.
                    defaultProfiles.TryAdd(profile.ResourceType, profile.Profile);
                }

                try
                {
                    _resolver = new CachedResolver(new MultiResolver(
                         new DirectorySource(_configuration.ProfilesLocationPath),
                         ZipSource.CreateValidationSource()));
                }
                catch (Exception)
                {
                    // Something went wrong during profile loading, what should we do?
                    throw;
                }
            }
        }

        private Validator GetValidator()
        {
            var ctx = new ValidationSettings()
            {
                ResourceResolver = _resolver,
                GenerateSnapshot = true,
                Trace = false,
                ResolveExternalReferences = false,
            };

            var validator = new Validator(ctx);

            return validator;
        }

        public void Validate(ITypedElement instance, string profileUrl = null)
        {
            if (!_configuration.Enabled)
            {
                throw new NotSupportedException("Validation shouldn't be invoked if it's disabled in configuration");
            }

            var validator = GetValidator();
            OperationOutcome result;
            if (!string.IsNullOrWhiteSpace(profileUrl))
            {
                result = validator.Validate(instance, profileUrl);
            }
            else
            {
                if (defaultProfiles.TryGetValue(instance.InstanceType, out string profile))
                {
                    result = validator.Validate(instance, profile);
                }
                else
                {
                    result = validator.Validate(instance);
                }
            }

            if (result.Success)
            {
                return;
            }
            else
            {
                var outcomeIssues = new OperationOutcomeIssue[result.Issue.Count];
                var index = 0;
                foreach (var issue in result.Issue)
                {
                    outcomeIssues[index++] = new OperationOutcomeIssue(
                        issue.Severity?.ToString(),
                        issue.Code.ToString(),
                        diagnostics: issue.Diagnostics,
                        detailsText: issue.Details.Text,
                        detailsCodes: new CodableConceptInfo(issue.Details.Coding.Select(x => new Hl7.Fhir.Model.Primitives.Coding(x.System, x.Code, x.Display))),
                        location: issue.Location.ToArray());
                }

                throw new ProfileValidationFailedException(outcomeIssues);
            }
        }
    }
}
