﻿namespace Khala.EventSourcing
{
    using System;
    using Messaging;
    using Newtonsoft.Json;

    public abstract class DomainEvent : IDomainEvent, IPartitioned
    {
        public Guid SourceId { get; set; }

        public int Version { get; set; }

        public DateTimeOffset RaisedAt { get; set; }

        [JsonIgnore]
        public string PartitionKey => SourceId.ToString();

        public void Raise(IVersionedEntity source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            SourceId = source.Id;
            Version = source.Version + 1;
            RaisedAt = DateTimeOffset.Now;
        }
    }
}
