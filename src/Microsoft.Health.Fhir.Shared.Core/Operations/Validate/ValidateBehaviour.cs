// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Validation.Profiles;
using Microsoft.Health.Fhir.Core.Messages;

namespace Microsoft.Health.Fhir.Shared.Core.Operations.Validate
{
    public class ValidateBehaviour<TCreateResourceRequest, TUpsertResourceResponse> : IPipelineBehavior<TCreateResourceRequest, TUpsertResourceResponse>
        where TCreateResourceRequest : RequestWithResourceForUpsert
    {
        private readonly IProfileValidator _profileValidator;

        public ValidateBehaviour(IProfileValidator profileValidator)
        {
            _profileValidator = profileValidator;
        }

        public async Task<TUpsertResourceResponse> Handle(TCreateResourceRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TUpsertResourceResponse> next)
        {
            _profileValidator.Validate(request.Resource.Instance);
            var response = await next();
            return response;
        }
    }
}
