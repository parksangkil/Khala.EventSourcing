﻿using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Khala.Messaging;
using TodoList.Domain.Events;

namespace TodoList.ReadModel
{
    public class ReadModelGenerator :
        InterfaceAwareHandler,
        IHandles<TodoItemCreated>,
        IHandles<TodoItemUpdated>,
        IHandles<TodoItemDeleted>
    {
        private readonly Func<ReadModelDbContext> _dbContextFactory;

        public ReadModelGenerator(Func<ReadModelDbContext> dbContextFactory)
        {
            if (dbContextFactory == null)
                throw new ArgumentNullException(nameof(dbContextFactory));

            _dbContextFactory = dbContextFactory;
        }

        public async Task Handle(
            Envelope<TodoItemCreated> envelope,
            CancellationToken cancellationToken)
        {
            TodoItemCreated domainEvent = envelope.Message;

            using (ReadModelDbContext db = _dbContextFactory.Invoke())
            {
                var todoItem = new TodoItem
                {
                    Id = domainEvent.SourceId,
                    CreatedAt = domainEvent.RaisedAt,
                    Description = domainEvent.Description
                };

                db.TodoItems.Add(todoItem);

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task Handle(
            Envelope<TodoItemUpdated> envelope,
            CancellationToken cancellationToken)
        {
            TodoItemUpdated domainEvent = envelope.Message;

            using (ReadModelDbContext db = _dbContextFactory.Invoke())
            {
                TodoItem todoItem = await db
                    .TodoItems
                    .Where(e => e.Id == domainEvent.SourceId)
                    .SingleAsync(cancellationToken);

                todoItem.Description = domainEvent.Description;

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task Handle(
            Envelope<TodoItemDeleted> envelope,
            CancellationToken cancellationToken)
        {
            TodoItemDeleted domainEvent = envelope.Message;

            using (ReadModelDbContext db = _dbContextFactory.Invoke())
            {
                TodoItem todoItem = await db
                    .TodoItems
                    .Where(e => e.Id == domainEvent.SourceId)
                    .SingleAsync(cancellationToken);

                db.TodoItems.Remove(todoItem);

                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
