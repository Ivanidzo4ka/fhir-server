// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Messages.Upsert;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Messages
{
    public abstract class RequestWithResourceForUpsert : IRequest<UpsertResourceResponse>
    {
        public RequestWithResourceForUpsert(ResourceElement resource)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            Resource = resource;
        }

        public ResourceElement Resource { get; }
    }
}
