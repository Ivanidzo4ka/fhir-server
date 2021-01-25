// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Messages;
using Microsoft.Health.Fhir.Core.Messages.Create;
using Microsoft.Health.Fhir.Core.Messages.Upsert;
using Microsoft.Health.Fhir.Shared.Core.Operations.Validate;

namespace Microsoft.Health.Fhir.Api.Modules.FeatureFlags.Validate
{
    public class ValidateFeatureModule : IStartupModule
    {
        public void Load(IServiceCollection services)
        {
            services.Add<ValidatePostConfigureOptions>()
                .Singleton()
                .AsSelf()
                .AsService<IPostConfigureOptions<MvcOptions>>();

            services.AddTransient(typeof(IPipelineBehavior<CreateResourceRequest, UpsertResourceResponse>), typeof(ValidateBehaviour<RequestWithResourceForUpsert, UpsertResourceResponse>));
            services.AddTransient(typeof(IPipelineBehavior<UpsertResourceRequest, UpsertResourceResponse>), typeof(ValidateBehaviour<RequestWithResourceForUpsert, UpsertResourceResponse>));
            services.AddTransient(typeof(IPipelineBehavior<ConditionalCreateResourceRequest, UpsertResourceResponse>), typeof(ValidateBehaviour<RequestWithResourceForUpsert, UpsertResourceResponse>));
            services.AddTransient(typeof(IPipelineBehavior<ConditionalUpsertResourceRequest, UpsertResourceResponse>), typeof(ValidateBehaviour<RequestWithResourceForUpsert, UpsertResourceResponse>));
        }
    }
}
