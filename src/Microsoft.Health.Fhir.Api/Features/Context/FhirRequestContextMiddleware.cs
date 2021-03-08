﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Health.Fhir.Core.Features;
using Microsoft.Health.Fhir.Core.Features.Context;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Api.Features.Context
{
    public class FhirRequestContextMiddleware
    {
        private readonly RequestDelegate _next;

        public FhirRequestContextMiddleware(RequestDelegate next)
        {
            EnsureArg.IsNotNull(next, nameof(next));

            _next = next;
        }

        public async Task Invoke(HttpContext context, IFhirRequestContextAccessor fhirRequestContextAccessor, CorrelationIdProvider correlationIdProvider)
        {
            HttpRequest request = context.Request;

            string baseUriInString = UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase);

            string uriInString = UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase,
                request.Path,
                request.QueryString);

            string correlationId = correlationIdProvider.Invoke();
            var key = context.Request.Headers.Keys.FirstOrDefault(h => h.Equals(KnownHeaders.RequestId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(key))
            {
                context.Response.Headers[KnownHeaders.CorrelationId] = (string)context.Request.Headers[key];
            }

            var fhirRequestContext = new FhirRequestContext(
                method: request.Method,
                uriString: uriInString,
                baseUriString: baseUriInString,
                correlationId: correlationId,
                requestHeaders: context.Request.Headers,
                responseHeaders: context.Response.Headers);

            context.Response.Headers[KnownHeaders.RequestId] = correlationId;

            fhirRequestContextAccessor.FhirRequestContext = fhirRequestContext;

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}
