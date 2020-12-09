// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Operations.Validate;
using Microsoft.Health.Fhir.Core.Features.Validation.Profiles;

namespace Microsoft.Health.Fhir.Api.Modules
{
    public class ProfileValidationModule : IStartupModule
    {
        public void Load(IServiceCollection services)
        {
            services.Add<ProfileValidator>()
                    .Singleton()
                    .AsService<IProfileValidator>();
        }
    }
}
