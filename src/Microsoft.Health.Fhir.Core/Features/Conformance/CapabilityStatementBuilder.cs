﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Core.Data;
using Microsoft.Health.Fhir.Core.Features.Conformance.Models;
using Microsoft.Health.Fhir.Core.Features.Conformance.Serialization;
using Microsoft.Health.Fhir.Core.Features.Definition;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Features.Validation;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.ValueSets;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Health.Fhir.Core.Features.Conformance
{
    internal class CapabilityStatementBuilder : ICapabilityStatementBuilder
    {
        private readonly ListedCapabilityStatement _statement;
        private readonly IModelInfoProvider _modelInfoProvider;
        private readonly ISearchParameterDefinitionManager _searchParameterDefinitionManager;
        private readonly IKnowSupportedProfiles _supportedProfiles;

        private CapabilityStatementBuilder(
            ListedCapabilityStatement statement,
            IModelInfoProvider modelInfoProvider,
            ISearchParameterDefinitionManager searchParameterDefinitionManager,
            IKnowSupportedProfiles supportedProfiles)
        {
            EnsureArg.IsNotNull(statement, nameof(statement));
            EnsureArg.IsNotNull(modelInfoProvider, nameof(modelInfoProvider));
            EnsureArg.IsNotNull(searchParameterDefinitionManager, nameof(searchParameterDefinitionManager));
            EnsureArg.IsNotNull(supportedProfiles, nameof(supportedProfiles));

            _statement = statement;
            _modelInfoProvider = modelInfoProvider;
            _searchParameterDefinitionManager = searchParameterDefinitionManager;
            _supportedProfiles = supportedProfiles;
        }

        public static ICapabilityStatementBuilder Create(IModelInfoProvider modelInfoProvider, ISearchParameterDefinitionManager searchParameterDefinitionManager, IKnowSupportedProfiles supportedProfiles)
        {
            EnsureArg.IsNotNull(modelInfoProvider, nameof(modelInfoProvider));
            EnsureArg.IsNotNull(searchParameterDefinitionManager, nameof(searchParameterDefinitionManager));

            using Stream resourceStream = modelInfoProvider.OpenVersionedFileStream("BaseCapabilities.json");
            using var reader = new StreamReader(resourceStream);
            var statement = JsonConvert.DeserializeObject<ListedCapabilityStatement>(reader.ReadToEnd());
            return new CapabilityStatementBuilder(statement, modelInfoProvider, searchParameterDefinitionManager, supportedProfiles);
        }

        public ICapabilityStatementBuilder Update(Action<ListedCapabilityStatement> action)
        {
            EnsureArg.IsNotNull(action, nameof(action));

            action(_statement);

            return this;
        }

        public ICapabilityStatementBuilder UpdateRestResourceComponent(string resourceType, Action<ListedResourceComponent> action)
        {
            EnsureArg.IsNotNullOrEmpty(resourceType, nameof(resourceType));
            EnsureArg.IsNotNull(action, nameof(action));
            EnsureArg.IsTrue(_modelInfoProvider.IsKnownResource(resourceType), nameof(resourceType), x => GenerateTypeErrorMessage(x, resourceType));

            ListedRestComponent listedRestComponent = _statement.Rest.Server();
            ListedResourceComponent resourceComponent = listedRestComponent.Resource.SingleOrDefault(x => string.Equals(x.Type, resourceType, StringComparison.OrdinalIgnoreCase));

            if (resourceComponent == null)
            {
                resourceComponent = new ListedResourceComponent
                {
                    Type = resourceType,
                    Profile = new ReferenceComponent
                    {
                        Reference = $"http://hl7.org/fhir/StructureDefinition/{resourceType}",
                    },
                };

                listedRestComponent.Resource.Add(resourceComponent);
            }

            action(resourceComponent);

            return this;
        }

        public ICapabilityStatementBuilder AddRestInteraction(string resourceType, string interaction)
        {
            EnsureArg.IsNotNullOrEmpty(resourceType, nameof(resourceType));
            EnsureArg.IsNotNullOrEmpty(interaction, nameof(interaction));
            EnsureArg.IsTrue(_modelInfoProvider.IsKnownResource(resourceType), nameof(resourceType), x => GenerateTypeErrorMessage(x, resourceType));

            UpdateRestResourceComponent(resourceType, c =>
            {
                if (!c.Interaction.Where(x => x.Code == interaction).Any())
                {
                    c.Interaction.Add(new ResourceInteractionComponent
                    {
                        Code = interaction,
                    });
                }
            });

            return this;
        }

        private void RemoveRestInteraction(string resourceType, string interaction)
        {
            UpdateRestResourceComponent(resourceType, c =>
            {
                var toRemove = c.Interaction.Where(x => x.Code == interaction).FirstOrDefault();
                if (toRemove != null)
                {
                    c.Interaction.Remove(toRemove);
                }
            });
        }

        public ICapabilityStatementBuilder AddRestInteraction(string systemInteraction)
        {
            EnsureArg.IsNotNullOrEmpty(systemInteraction, nameof(systemInteraction));

            _statement.Rest.Server().Interaction.Add(new ResourceInteractionComponent { Code = systemInteraction });

            return this;
        }

        public ICapabilityStatementBuilder AddSharedSearchParameters()
        {
            _statement.Rest.Server().SearchParam.Add(new SearchParamComponent { Name = SearchParameterNames.ResourceType, Definition = SearchParameterNames.TypeUri, Type = SearchParamType.Token });

            return this;
        }

        private ICapabilityStatementBuilder SyncSearchParams(string resourceType)
        {
            EnsureArg.IsNotNullOrEmpty(resourceType, nameof(resourceType));
            EnsureArg.IsTrue(_modelInfoProvider.IsKnownResource(resourceType), nameof(resourceType), x => GenerateTypeErrorMessage(x, resourceType));

            IEnumerable<SearchParameterInfo> searchParams = _searchParameterDefinitionManager.GetSearchParameters(resourceType);

            if (searchParams.Any())
            {
                UpdateRestResourceComponent(resourceType, c =>
                {
                    c.SearchParam.Clear();
                    foreach (SearchParamComponent searchParam in searchParams.Select(x => new SearchParamComponent
                    {
                        Name = x.Name,
                        Type = x.Type,
                        Definition = x.Url,
                        Documentation = x.Description,
                    }))
                    {
                        // Exclude _type search param under resource
                        if (string.Equals("_type", searchParam.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        c.SearchParam.Add(searchParam);
                    }
                });
                AddRestInteraction(resourceType, TypeRestfulInteraction.SearchType);
            }
            else
            {
                RemoveRestInteraction(resourceType, TypeRestfulInteraction.SearchType);
            }

            return this;
        }

        private ICapabilityStatementBuilder SyncProfile(string resourceType)
        {
            EnsureArg.IsNotNullOrEmpty(resourceType, nameof(resourceType));
            EnsureArg.IsTrue(_modelInfoProvider.IsKnownResource(resourceType), nameof(resourceType), x => GenerateTypeErrorMessage(x, resourceType));

            UpdateRestResourceComponent(resourceType, resourceComponent =>
            {
                var supportedProfiles = _supportedProfiles.GetSupportedProfiles(resourceType);
                if (supportedProfiles != null)
                {
                    if (!_modelInfoProvider.Version.Equals(FhirSpecification.Stu3))
                    {
                        resourceComponent.SupportedProfile.Clear();
                        foreach (var profile in supportedProfiles)
                        {
                            resourceComponent.SupportedProfile.Add(profile);
                        }
                    }
                    else
                    {
                        foreach (var profile in supportedProfiles)
                        {
                            _statement.Profile.Add(new ReferenceComponent
                            {
                                Reference = profile,
                            });
                        }
                    }
                }
            });

            return this;
        }

        public ICapabilityStatementBuilder AddDefaultResourceInteractions()
        {
            foreach (string resource in _modelInfoProvider.GetResourceTypeNames())
            {
                // Parameters is a non-persisted resource used to pass information into and back from an operation.
                if (string.Equals(resource, KnownResourceTypes.Parameters, StringComparison.Ordinal))
                {
                    continue;
                }

                AddRestInteraction(resource, TypeRestfulInteraction.Create);
                AddRestInteraction(resource, TypeRestfulInteraction.Read);
                AddRestInteraction(resource, TypeRestfulInteraction.Vread);
                AddRestInteraction(resource, TypeRestfulInteraction.HistoryType);
                AddRestInteraction(resource, TypeRestfulInteraction.HistoryInstance);

                // AuditEvents should not allow Update or Delete
                if (!string.Equals(resource, KnownResourceTypes.AuditEvent, StringComparison.Ordinal))
                {
                    AddRestInteraction(resource, TypeRestfulInteraction.Update);
                    AddRestInteraction(resource, TypeRestfulInteraction.Delete);
                }

                UpdateRestResourceComponent(resource, component =>
                {
                    component.Versioning.Add(ResourceVersionPolicy.NoVersion);
                    component.Versioning.Add(ResourceVersionPolicy.Versioned);
                    component.Versioning.Add(ResourceVersionPolicy.VersionedUpdate);

                    // Create is added for every resource above.
                    component.ConditionalCreate = true;

                    // AuditEvent don't allow update, so no conditional update as well.
                    if (!string.Equals(resource, KnownResourceTypes.AuditEvent, StringComparison.Ordinal))
                    {
                        component.ConditionalUpdate = true;
                    }

                    component.ReadHistory = true;
                    component.UpdateCreate = true;
                });
            }

            AddRestInteraction(SystemRestfulInteraction.HistorySystem);

            return this;
        }

        public ICapabilityStatementBuilder SyncSearchParameters()
        {
            foreach (string resource in _modelInfoProvider.GetResourceTypeNames())
            {
                // Parameters is a non-persisted resource used to pass information into and back from an operation
                if (string.Equals(resource, KnownResourceTypes.Parameters, StringComparison.Ordinal))
                {
                    continue;
                }

                SyncSearchParams(resource);
            }

            return this;
        }

        public ICapabilityStatementBuilder SyncProfiles()
        {
            foreach (string resource in _modelInfoProvider.GetResourceTypeNames())
            {
                SyncProfile(resource);
            }

            return this;
        }

        public ITypedElement Build()
        {
            // To build a CapabilityStatement we use a custom JsonConverter that serializes
            // the ListedCapabilityStatement into a CapabilityStatement poco

            var json = JsonConvert.SerializeObject(_statement, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new DefaultOptionHashSetJsonConverter(),
                    new EnumLiteralJsonConverter(),
                    new ProfileReferenceConverter(_modelInfoProvider),
                },
                NullValueHandling = NullValueHandling.Ignore,
            });

            ISourceNode jsonStatement = FhirJsonNode.Parse(json);

            // Using a version specific StructureDefinitionSummaryProvider ensures the metadata to be
            // compatible with the current FhirSerializer/output formatter.
            return jsonStatement.ToTypedElement(_modelInfoProvider.StructureDefinitionSummaryProvider);
        }

        private static EnsureOptions GenerateTypeErrorMessage(EnsureOptions options, string resourceType)
        {
            return options.WithMessage($"Unknown resource type {resourceType}");
        }
    }
}
