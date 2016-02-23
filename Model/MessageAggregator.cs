/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System.Collections.Generic;
using System.Linq;
using Glimpse.Couchbase.Message;

namespace Glimpse.Couchbase.Model
{
    public class MessageAggregator
    {
        private readonly IList<CouchbaseMessage> _messages;
        private AggregateMetadata _metadata;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messages">List of messages to aggregate</param>
        public MessageAggregator(
            IList<CouchbaseMessage> messages)
        {
            _messages = messages;
        }

        /// <summary>
        /// Performs the message aggregation and returns the results
        /// </summary>
        /// <returns>Aggregate message metadata</returns>
        public AggregateMetadata Aggregate()
        {
            _metadata = new AggregateMetadata();

            AggregateOperationExecuted();
            AggregateOperationCompleted();
            AggregateOperationErrors();

            return _metadata;
        }

        /// <summary>
        /// Aggregates all operation executed messages
        /// </summary>
        private void AggregateOperationExecuted()
        {
            var dupeTracker = new Dictionary<string, int>();
            var messages = _messages.OfType<OperationStartedMessage>();
            foreach (var message in messages) {
                var command = GetOrCreateCommandFor(message);
                command.Type = message.Type;
                command.Keys = message.Keys;
                command.StartTime = message.StartTime;
                command.Offset = message.Offset;
                command.IsAsync = message.IsAsync;

                // Duplicate tracking
                if (message.CheckDupes) {
                    var dupeCount = 0;
                    var key = message.Type + " " + string.Join(" ", message.Keys);
                    command.IsDuplicate = dupeTracker.TryGetValue(key, out dupeCount);
                    dupeTracker[key] = dupeCount + 1;
                }
            }
        }

        /// <summary>
        /// Aggregates all operation completed messages
        /// </summary>
        private void AggregateOperationCompleted()
        {
            var messages = _messages.OfType<OperationCompletedMessage>();
            foreach (var message in messages) {
                var command = GetOrCreateCommandFor(message);
                command.KeysFound = message.KeysFound;
                command.Duration = message.Duration;
                command.StartTime = message.StartTime;
                command.EndTime = message.StartTime + message.Offset;
                command.Offset = message.Offset;
                command.IsAsync = message.IsAsync;
            }
        }

        /// <summary>
        /// Aggregates all the operation error messages
        /// </summary>
        private void AggregateOperationErrors()
        {
            var messages = _messages.OfType<OperationErrorMessage>();
            foreach (var message in messages) {
                var command = GetOrCreateCommandFor(message);
                command.Duration = message.Duration;
                command.Messages = message.Messages;
                command.Exceptions = message.Exceptions;
                command.StartTime = message.StartTime;
                command.EndTime = message.StartTime + message.Offset;
                command.Offset = message.Offset;
                command.IsAsync = message.IsAsync;
            }
        }

        /// <summary>
        /// Gets connection metadata for a couchbase message
        /// </summary>
        /// <param name="message">Message to inspect</param>
        /// <returns>Connection metadata for this message</returns>
        private ConnectionMetadata GetOrCreateConnectionFor(
            CouchbaseMessage message)
        {
            ConnectionMetadata connection;
            var bucketName = message.BucketName;
            if (!_metadata.Connections.TryGetValue(bucketName, out connection)) {
                connection = new ConnectionMetadata(bucketName);
                _metadata.Connections.Add(bucketName, connection);
            }
            return connection;
        }

        /// <summary>
        /// Gets command metadata for a couchbase message
        /// </summary>
        /// <param name="message">Message to inspect</param>
        /// <returns>Operation metadata for this command</returns>
        private OperationMetadata GetOrCreateCommandFor(
            OperationMessage message)
        {
            OperationMetadata command;
            var bucketName = message.BucketName;
            var operationId = message.OperationId.ToString();
            if (!_metadata.Operations.TryGetValue(operationId, out command)) {
                command = new OperationMetadata(operationId, bucketName);
                _metadata.Operations.Add(operationId, command);
                var connection = GetOrCreateConnectionFor(message);
                connection.RegisterOperation(command);
            }
            return command;
        }
    }
}