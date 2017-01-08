﻿namespace ReactiveArchitecture.EventSourcing
{
    using System;
    using System.Collections.Generic;

    public abstract class EventSourced : IEventSourced
    {
        private readonly Guid _id;
        private readonly Dictionary<Type, Action<IDomainEvent>> _eventHandlers;
        private readonly List<IDomainEvent> _pendingEvents;
        private int _version;

        protected EventSourced(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    $"{nameof(id)} cannot be empty", nameof(id));
            }

            _id = id;
            _eventHandlers = new Dictionary<Type, Action<IDomainEvent>>();
            _pendingEvents = new List<IDomainEvent>();
            _version = 0;
        }

        protected delegate void DomainEventHandler<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent;

        public Guid Id => _id;

        public int Version => _version;

        public IEnumerable<IDomainEvent> PendingEvents => _pendingEvents;

        protected void SetEventHandler<TEvent>(
            DomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _eventHandlers.Add(typeof(TEvent), e => handler.Invoke((TEvent)e));
        }

        protected void HandlePastEvents(IEnumerable<IDomainEvent> pastEvents)
        {
            if (pastEvents == null)
            {
                throw new ArgumentNullException(nameof(pastEvents));
            }

            foreach (IDomainEvent domainEvent in pastEvents)
            {
                if (domainEvent == null)
                {
                    throw new ArgumentException(
                        $"{nameof(pastEvents)} cannot contain null.",
                        nameof(pastEvents));
                }

                try
                {
                    HandleEvent(domainEvent);
                }
                catch (ArgumentException exception)
                {
                    var message =
                        $"Could not handle {nameof(pastEvents)} successfully." +
                        " See the inner exception for details.";
                    throw new ArgumentException(
                        message, nameof(pastEvents), exception);
                }
            }
        }

        protected void RaiseEvent<TEvent>(TEvent domainEvent)
            where TEvent : IDomainEvent
        {
            if (domainEvent == null)
            {
                throw new ArgumentNullException(nameof(domainEvent));
            }

            domainEvent.Raise(this);
            HandleEvent(domainEvent);
            _pendingEvents.Add(domainEvent);
        }

        private void HandleEvent(IDomainEvent domainEvent)
        {
            if (domainEvent.SourceId != _id)
            {
                var message = $"{nameof(domainEvent.SourceId)} is invalid.";
                throw new ArgumentException(message, nameof(domainEvent));
            }

            if (domainEvent.Version != _version + 1)
            {
                var message = $"{nameof(domainEvent.Version)} is invalid.";
                throw new ArgumentException(message, nameof(domainEvent));
            }

            Type eventType = domainEvent.GetType();
            Action<IDomainEvent> handler;
            if (_eventHandlers.TryGetValue(eventType, out handler))
            {
                handler.Invoke(domainEvent);
                _version = domainEvent.Version;
            }
            else
            {
                var message = $"Cannot handle event of type {eventType}.";
                throw new InvalidOperationException(message);
            }
        }
    }
}