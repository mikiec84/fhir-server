﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Hl7.Fhir.Model;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Persistence
{
    public class ResourceWrapper
    {
        public ResourceWrapper(Resource resource, RawResource rawResource, ResourceRequest request, bool deleted, IReadOnlyCollection<KeyValuePair<string, string>> lastModifiedClaims)
           : this(
                 IsNotNull(resource).Id,
                 resource.VersionId,
                 resource.TypeName,
                 rawResource,
                 request,
                 resource.Meta?.LastUpdated ?? Clock.UtcNow,
                 deleted,
                 lastModifiedClaims)
        {
        }

        public ResourceWrapper(
            string resourceId,
            string versionId,
            string resourceTypeName,
            RawResource rawResource,
            ResourceRequest request,
            DateTimeOffset lastModified,
            bool deleted,
            IReadOnlyCollection<KeyValuePair<string, string>> lastModifiedClaims)
        {
            EnsureArg.IsNotNullOrEmpty(resourceId, nameof(resourceId));
            EnsureArg.IsNotNullOrEmpty(resourceTypeName, nameof(resourceTypeName));
            EnsureArg.IsNotNull(rawResource, nameof(rawResource));

            ResourceId = resourceId;
            Version = versionId;
            ResourceTypeName = resourceTypeName;
            RawResource = rawResource;
            Request = request;
            IsDeleted = deleted;
            LastModified = lastModified;
            LastModifiedClaims = lastModifiedClaims;
        }

        [JsonConstructor]
        protected ResourceWrapper()
        {
        }

        [JsonProperty(KnownResourceWrapperProperties.LastModified)]
        public DateTimeOffset LastModified { get; protected set; }

        [JsonProperty(KnownResourceWrapperProperties.RawResource)]
        public RawResource RawResource { get; protected set; }

        [JsonProperty(KnownResourceWrapperProperties.Request)]
        public ResourceRequest Request { get; protected set; }

        [JsonProperty(KnownResourceWrapperProperties.IsDeleted)]
        public bool IsDeleted { get; protected set; }

        [JsonProperty(KnownResourceWrapperProperties.ResourceId)]
        public string ResourceId { get; protected set; }

        [JsonProperty(KnownResourceWrapperProperties.ResourceTypeName)]
        public string ResourceTypeName { get; protected set; }

        [JsonProperty(KnownResourceWrapperProperties.Version)]
        public virtual string Version { get; protected set; }

        [JsonProperty(KnownResourceWrapperProperties.IsHistory)]
        public virtual bool IsHistory { get; protected set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1822:Mark members as static", Justification = "Used in serialization")]
        [JsonProperty(KnownResourceWrapperProperties.IsSystem)]
        public bool IsSystem => false;

        [JsonProperty(KnownResourceWrapperProperties.LastModifiedClaims)]
        public IReadOnlyCollection<KeyValuePair<string, string>> LastModifiedClaims { get; protected set; }

        public ResourceKey ToResourceKey()
        {
            return new ResourceKey(ResourceTypeName, ResourceId, Version);
        }

        private static Resource IsNotNull(Resource resource)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            return resource;
        }
    }
}