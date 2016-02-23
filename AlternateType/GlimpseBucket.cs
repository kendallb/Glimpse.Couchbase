/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Couchbase.Core.Buckets;
using Couchbase.IO;
using Couchbase.IO.Operations;
using Couchbase.Management;
using Couchbase.N1QL;
using Couchbase.Views;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Framework;
using Glimpse.Core.Message;
using Glimpse.Couchbase.Message;

namespace Glimpse.Couchbase.AlternateType
{
    public class GlimpseBucket : IBucket
    {
        private IMessageBroker _messageBroker;
        private IExecutionTimer _timerStrategy;
        private readonly IBucket _innerBucket;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bucket">Real bucket to wrap</param>
        public GlimpseBucket(
            IBucket bucket)
        {
            _innerBucket = bucket;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bucket">Real bucket to wrap</param>
        /// <param name="messageBroker">Message broker to use</param>
        /// <param name="timerStrategy">Timer strategy to use</param>
        public GlimpseBucket(
            IBucket bucket,
            IMessageBroker messageBroker,
            IExecutionTimer timerStrategy)
            : this(bucket)
        {
            MessageBroker = messageBroker;
            TimerStrategy = timerStrategy;
        }

        /// <summary>
        /// Gets or sets the message broker in use
        /// </summary>
        internal IMessageBroker MessageBroker
        {
            get { return _messageBroker ?? (_messageBroker = GlimpseConfiguration.GetConfiguredMessageBroker()); }
            set { _messageBroker = value; }
        }

        /// <summary>
        /// Gets or sets the timer strategy in use
        /// </summary>
        internal IExecutionTimer TimerStrategy
        {
            get { return _timerStrategy ?? (_timerStrategy = GlimpseConfiguration.GetConfiguredTimerStrategy()()); }
            set { _timerStrategy = value; }
        }

        /// <summary>
        /// Starts the timer for logging operations
        /// </summary>
        /// <returns>TimeSpan that represents this timing instance</returns>
        public TimeSpan StartTimer()
        {
            return TimerStrategy != null ? TimerStrategy.Start() : TimeSpan.Zero;
        }

        /// <summary>
        /// Logs the start of a couchbase operation
        /// </summary>
        /// <param name="operationId">ID of the operation</param>
        /// <param name="timer">Timespan for timing</param>
        /// <param name="keys">Couchbase keys</param>
        /// <param name="type">Name for the type of operation</param>
        /// <param name="checkDupes">True to check for duplicate operations</param>
        /// <param name="isAsync">True if async</param>
        public void LogOperationStart(
            Guid operationId,
            TimeSpan timer,
            string[] keys,
            string type,
            bool checkDupes,
            bool isAsync)
        {
            MessageBroker.Publish(new OperationStartedMessage(_innerBucket.Name, operationId, type, keys, checkDupes, isAsync).AsTimedMessage(timer));
        }

        /// <summary>
        /// Logs the end of a couchbase operation
        /// </summary>
        /// <param name="operationId">ID of the operation</param>
        /// <param name="timer">Timespan for timing</param>
        /// <param name="status">Status of the response</param>
        /// <param name="type">Name for the type of operation</param>
        /// <param name="isAsync">True if async</param>
        public void LogOperationEnd(
            Guid operationId,
            TimeSpan timer,
            ResponseStatus status,
            string type,
            bool isAsync)
        {
            LogOperationEnd(operationId, timer, new [] {status != ResponseStatus.KeyNotFound }, type, isAsync);
        }

        /// <summary>
        /// Logs the end of a couchbase operation
        /// </summary>
        /// <param name="operationId">ID of the operation</param>
        /// <param name="timer">Timespan for timing</param>
        /// <param name="keysFound">Array of key found statuses</param>
        /// <param name="type">Name for the type of operation</param>
        /// <param name="isAsync">True if async</param>
        public void LogOperationEnd(
            Guid operationId,
            TimeSpan timer,
            bool[] keysFound,
            string type,
            bool isAsync)
        {
            MessageBroker.Publish(new OperationCompletedMessage(_innerBucket.Name, operationId, keysFound, isAsync)
                .AsTimedMessage(TimerStrategy.Stop(timer))
                .AsTimelineMessage("Couchbase: " + type, MvcTimelineCategory.Operation));
        }

        /// <summary>
        /// Log errors for couchbase operations
        /// </summary>
        /// <param name="operationId">ID of the operation</param>
        /// <param name="timer">Timespan for timing</param>
        /// <param name="message">Message explaining the error</param>
        /// <param name="exception">Exception that occured if present</param>
        /// <param name="type">Name for the type of operation</param>
        /// <param name="isAsync">True if async</param>
        public void LogOperationError(
            Guid operationId,
            TimeSpan timer,
            string message,
            Exception exception,
            string type,
            bool isAsync)
        {
            LogOperationError(operationId, timer, new[] { message }, new[] { exception }, type, isAsync);
        }

        /// <summary>
        /// Log errors for couchbase operations
        /// </summary>
        /// <param name="operationId">ID of the operation</param>
        /// <param name="timer">Timespan for timing</param>
        /// <param name="messages">Messages explaining the error</param>
        /// <param name="exceptions">Exceptions that occured if present</param>
        /// <param name="type">Name for the type of operation</param>
        /// <param name="isAsync">True if async</param>
        public void LogOperationError(
            Guid operationId,
            TimeSpan timer,
            string[] messages,
            Exception[] exceptions,
            string type,
            bool isAsync)
        {
            MessageBroker.Publish(new OperationErrorMessage(_innerBucket.Name, operationId, messages, exceptions)
                .AsTimedMessage(TimerStrategy.Stop(timer))
                .AsTimelineMessage("Couchbase: Error", MvcTimelineCategory.Operation, type));
        }

        /// <summary>
        /// Do nothing here was we are just wrapping the bucket
        /// </summary>
        public void Dispose()
        {
        }

        public bool Exists(
            string key)
        {
            return _innerBucket.Exists(key);
        }

        public Task<bool> ExistsAsync(
            string key)
        {
            return _innerBucket.ExistsAsync(key);
        }

        public Task<ObserveResponse> ObserveAsync(
            string key,
            ulong cas,
            bool deletion,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.ObserveAsync(key, cas, deletion, replicateTo, persistTo);
        }

        public ObserveResponse Observe(
            string key,
            ulong cas,
            bool deletion,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Observe(key, cas, deletion, replicateTo, persistTo);
        }

        public IDocumentResult<T> Upsert<T>(
            IDocument<T> document)
        {
            return _innerBucket.Upsert(document);
        }

        public Task<IDocumentResult<T>> UpsertAsync<T>(
            IDocument<T> document)
        {
            return _innerBucket.UpsertAsync(document);
        }

        public IDocumentResult<T> Upsert<T>(
            IDocument<T> document,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Upsert(document, replicateTo);
        }

        public Task<IDocumentResult<T>> UpsertAsync<T>(
            IDocument<T> document,
            ReplicateTo replicateTo)
        {
            return _innerBucket.UpsertAsync(document, replicateTo);
        }

        public IDocumentResult<T> Upsert<T>(
            IDocument<T> document,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Upsert(document, replicateTo, persistTo);
        }

        public Task<IDocumentResult<T>> UpsertAsync<T>(
            IDocument<T> document,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.UpsertAsync(document, replicateTo, persistTo);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Upsert", false, false);
                var result = _innerBucket.Upsert(key, value);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return _innerBucket.Upsert(key, value);
        }

        public async Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new[] { key }, "Upsert", false, false);
                var result = await _innerBucket.UpsertAsync(key, value);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return await _innerBucket.UpsertAsync(key, value);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            uint expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new[] { key }, "Upsert", false, false);
                var result = _innerBucket.Upsert(key, value, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return _innerBucket.Upsert(key, value, expiration);
        }

        public async Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            uint expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new[] { key }, "Upsert", false, false);
                var result = await _innerBucket.UpsertAsync(key, value, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return await _innerBucket.UpsertAsync(key, value, expiration);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            TimeSpan expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new[] { key }, "Upsert", false, false);
                var result = _innerBucket.Upsert(key, value, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return _innerBucket.Upsert(key, value, expiration);
        }

        public async Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            TimeSpan expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new[] { key }, "Upsert", false, false);
                var result = await _innerBucket.UpsertAsync(key, value, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return await _innerBucket.UpsertAsync(key, value, expiration);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            ulong cas)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new[] { key }, "Upsert", false, false);
                var result = _innerBucket.Upsert(key, value, cas);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return _innerBucket.Upsert(key, value, cas);
        }

        public async Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            ulong cas)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new[] { key }, "Upsert", false, false);
                var result = await _innerBucket.UpsertAsync(key, value, cas);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return await _innerBucket.UpsertAsync(key, value, cas);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            ulong cas,
            uint expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new[] { key }, "Upsert", false, false);
                var result = _innerBucket.Upsert(key, value, cas, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return _innerBucket.Upsert(key, value, cas, expiration);
        }

        public async Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            ulong cas,
            uint expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new[] { key }, "Upsert", false, false);
                var result = await _innerBucket.UpsertAsync(key, value, cas, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return await _innerBucket.UpsertAsync(key, value, cas, expiration);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            ulong cas,
            TimeSpan expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Upsert", false, false);
                var result = _innerBucket.Upsert(key, value, cas, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return _innerBucket.Upsert(key, value, cas, expiration);
        }

        public async Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            ulong cas,
            TimeSpan expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Upsert", false, false);
                var result = await _innerBucket.UpsertAsync(key, value, cas, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Upsert", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Upsert", false);
                }
                return result;
            }
            return await _innerBucket.UpsertAsync(key, value, cas, expiration);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Upsert(key, value, replicateTo);
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            ReplicateTo replicateTo)
        {
            return _innerBucket.UpsertAsync(key, value, replicateTo);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Upsert(key, value, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.UpsertAsync(key, value, replicateTo, persistTo);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            uint expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Upsert(key, value, expiration, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            uint expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.UpsertAsync(key, value, expiration, replicateTo, persistTo);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            ulong cas,
            uint expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Upsert(key, value, cas, expiration, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            ulong cas,
            uint expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.UpsertAsync(key, value, cas, expiration, replicateTo, persistTo);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            TimeSpan expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Upsert(key, value, expiration, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            TimeSpan expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.UpsertAsync(key, value, expiration, replicateTo, persistTo);
        }

        public IOperationResult<T> Upsert<T>(
            string key,
            T value,
            ulong cas,
            TimeSpan expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Upsert(key, value, cas, expiration, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> UpsertAsync<T>(
            string key,
            T value,
            ulong cas,
            TimeSpan expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.UpsertAsync(key, value, cas, expiration, replicateTo, persistTo);
        }

        public IDictionary<string, IOperationResult<T>> Upsert<T>(
            IDictionary<string, T> items)
        {
            return _innerBucket.Upsert(items);
        }

        public IDictionary<string, IOperationResult<T>> Upsert<T>(
            IDictionary<string, T> items,
            ParallelOptions options)
        {
            return _innerBucket.Upsert(items, options);
        }

        public IDictionary<string, IOperationResult<T>> Upsert<T>(
            IDictionary<string, T> items,
            ParallelOptions options,
            int rangeSize)
        {
            return _innerBucket.Upsert(items, options, rangeSize);
        }

        public IDocumentResult<T> Replace<T>(
            IDocument<T> document)
        {
            return _innerBucket.Replace(document);
        }

        public Task<IDocumentResult<T>> ReplaceAsync<T>(
            IDocument<T> document)
        {
            return _innerBucket.ReplaceAsync(document);
        }

        public IDocumentResult<T> Replace<T>(
            IDocument<T> document,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Replace(document, replicateTo);
        }

        public Task<IDocumentResult<T>> ReplaceAsync<T>(
            IDocument<T> document,
            ReplicateTo replicateTo)
        {
            return _innerBucket.ReplaceAsync(document, replicateTo);
        }

        public IDocumentResult<T> Replace<T>(
            IDocument<T> document,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Replace(document, replicateTo, persistTo);
        }

        public Task<IDocumentResult<T>> ReplaceAsync<T>(
            IDocument<T> document,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.ReplaceAsync(document, replicateTo, persistTo);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value)
        {
            return _innerBucket.Replace(key, value);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value)
        {
            return _innerBucket.ReplaceAsync(key, value);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            uint expiration)
        {
            return _innerBucket.Replace(key, value, expiration);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            uint expiration)
        {
            return _innerBucket.ReplaceAsync(key, value, expiration);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            TimeSpan expiration)
        {
            return _innerBucket.Replace(key, value, expiration);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            TimeSpan expiration)
        {
            return _innerBucket.ReplaceAsync(key, value, expiration);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            ulong cas)
        {
            return _innerBucket.Replace(key, value, cas);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            ulong cas)
        {
            return _innerBucket.ReplaceAsync(key, value, cas);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            ulong cas,
            uint expiration)
        {
            return _innerBucket.Replace(key, value, cas, expiration);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            ulong cas,
            uint expiration)
        {
            return _innerBucket.ReplaceAsync(key, value, cas, expiration);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            ulong cas,
            TimeSpan expiration)
        {
            return _innerBucket.Replace(key, value, cas, expiration);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            ulong cas,
            TimeSpan expiration)
        {
            return _innerBucket.ReplaceAsync(key, value, cas, expiration);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Replace(key, value, replicateTo);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            ReplicateTo replicateTo)
        {
            return _innerBucket.ReplaceAsync(key, value, replicateTo);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            ulong cas,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Replace(key, value, cas, replicateTo);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            ulong cas,
            ReplicateTo replicateTo)
        {
            return _innerBucket.ReplaceAsync(key, value, cas, replicateTo);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Replace(key, value, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.ReplaceAsync(key, value, replicateTo, persistTo);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            ulong cas,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Replace(key, value, cas, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            ulong cas,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.ReplaceAsync(key, value, cas, replicateTo, persistTo);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            ulong cas,
            uint expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Replace(key, value, cas, expiration, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            ulong cas,
            uint expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.ReplaceAsync(key, value, cas, expiration, replicateTo, persistTo);
        }

        public IOperationResult<T> Replace<T>(
            string key,
            T value,
            ulong cas,
            TimeSpan expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Replace(key, value, cas, expiration, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> ReplaceAsync<T>(
            string key,
            T value,
            ulong cas,
            TimeSpan expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.ReplaceAsync(key, value, cas, expiration, replicateTo, persistTo);
        }

        public IDocumentResult<T> Insert<T>(
            IDocument<T> document)
        {
            return _innerBucket.Insert(document);
        }

        public Task<IDocumentResult<T>> InsertAsync<T>(
            IDocument<T> document)
        {
            return _innerBucket.InsertAsync(document);
        }

        public IDocumentResult<T> Insert<T>(
            IDocument<T> document,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Insert(document, replicateTo);
        }

        public Task<IDocumentResult<T>> InsertAsync<T>(
            IDocument<T> document,
            ReplicateTo replicateTo)
        {
            return _innerBucket.InsertAsync(document, replicateTo);
        }

        public IDocumentResult<T> Insert<T>(
            IDocument<T> document,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Insert(document, replicateTo, persistTo);
        }

        public Task<IDocumentResult<T>> InsertAsync<T>(
            IDocument<T> document,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.InsertAsync(document, replicateTo, persistTo);
        }

        public IOperationResult<T> Insert<T>(
            string key,
            T value)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Insert", false, true);
                var result = _innerBucket.Insert(key, value);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Insert", true);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Insert", true);
                }
                return result;
            }
            return _innerBucket.Insert(key, value);
        }

        public async Task<IOperationResult<T>> InsertAsync<T>(
            string key,
            T value)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Insert", false, true);
                var result = await _innerBucket.InsertAsync(key, value);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Insert", true);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Insert", true);
                }
                return result;
            }
            return await _innerBucket.InsertAsync(key, value);
        }

        public IOperationResult<T> Insert<T>(
            string key,
            T value,
            uint expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Insert", false, true);
                var result = _innerBucket.Insert(key, value, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Insert", true);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Insert", true);
                }
                return result;
            }
            return _innerBucket.Insert(key, value, expiration);
        }

        public async Task<IOperationResult<T>> InsertAsync<T>(
            string key,
            T value,
            uint expiration)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Insert", false, true);
                var result = await _innerBucket.InsertAsync(key, value, expiration);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Insert", true);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Insert", true);
                }
                return result;
            }
            return await _innerBucket.InsertAsync(key, value, expiration);
        }

        public IOperationResult<T> Insert<T>(
            string key,
            T value,
            TimeSpan expiration)
        {
            return _innerBucket.Insert(key, value, expiration);
        }

        public Task<IOperationResult<T>> InsertAsync<T>(
            string key,
            T value,
            TimeSpan expiration)
        {
            return _innerBucket.InsertAsync(key, value, expiration);
        }

        public IOperationResult<T> Insert<T>(
            string key,
            T value,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Insert(key, value, replicateTo);
        }

        public Task<IOperationResult<T>> InsertAsync<T>(
            string key,
            T value,
            ReplicateTo replicateTo)
        {
            return _innerBucket.InsertAsync(key, value, replicateTo);
        }

        public IOperationResult<T> Insert<T>(
            string key,
            T value,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Insert(key, value, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> InsertAsync<T>(
            string key,
            T value,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.InsertAsync(key, value, replicateTo, persistTo);
        }

        public IOperationResult<T> Insert<T>(
            string key,
            T value,
            uint expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Insert(key, value, expiration, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> InsertAsync<T>(
            string key,
            T value,
            uint expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.InsertAsync(key, value, expiration, replicateTo, persistTo);
        }

        public IOperationResult<T> Insert<T>(
            string key,
            T value,
            TimeSpan expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Insert(key, value, expiration, replicateTo, persistTo);
        }

        public Task<IOperationResult<T>> InsertAsync<T>(
            string key,
            T value,
            TimeSpan expiration,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.InsertAsync(key, value, expiration, replicateTo, persistTo);
        }

        public IOperationResult Remove<T>(
            IDocument<T> document)
        {
            return _innerBucket.Remove(document);
        }

        public Task<IOperationResult> RemoveAsync<T>(
            IDocument<T> document)
        {
            return _innerBucket.RemoveAsync(document);
        }

        public IOperationResult Remove<T>(
            IDocument<T> document,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Remove(document, replicateTo);
        }

        public Task<IOperationResult> RemoveAsync<T>(
            IDocument<T> document,
            ReplicateTo replicateTo)
        {
            return _innerBucket.RemoveAsync(document, replicateTo);
        }

        public IOperationResult Remove<T>(
            IDocument<T> document,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Remove(document, replicateTo, persistTo);
        }

        public Task<IOperationResult> RemoveAsync<T>(
            IDocument<T> document,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.RemoveAsync(document, replicateTo, persistTo);
        }

        public IOperationResult Remove(
            string key)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Remove", false, false);
                var result = _innerBucket.Remove(key);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Remove", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Remove", false);
                }
                return result;
            }
            return _innerBucket.Remove(key);
        }

        public async Task<IOperationResult> RemoveAsync(
            string key)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Remove", false, true);
                var result = await _innerBucket.RemoveAsync(key);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Remove", true);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Remove", true);
                }
                return result;
            }
            return await _innerBucket.RemoveAsync(key);
        }

        public IOperationResult Remove(
            string key,
            ulong cas)
        {
            return _innerBucket.Remove(key, cas);
        }

        public Task<IOperationResult> RemoveAsync(
            string key,
            ulong cas)
        {
            return _innerBucket.RemoveAsync(key, cas);
        }

        public IOperationResult Remove(
            string key,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Remove(key, replicateTo);
        }

        public Task<IOperationResult> RemoveAsync(
            string key,
            ReplicateTo replicateTo)
        {
            return _innerBucket.RemoveAsync(key, replicateTo);
        }

        public IOperationResult Remove(
            string key,
            ulong cas,
            ReplicateTo replicateTo)
        {
            return _innerBucket.Remove(key, cas, replicateTo);
        }

        public Task<IOperationResult> RemoveAsync(
            string key,
            ulong cas,
            ReplicateTo replicateTo)
        {
            return _innerBucket.RemoveAsync(key, cas, replicateTo);
        }

        public IOperationResult Remove(
            string key,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Remove(key, replicateTo, persistTo);
        }

        public Task<IOperationResult> RemoveAsync(
            string key,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.RemoveAsync(key, replicateTo, persistTo);
        }

        public IOperationResult Remove(
            string key,
            ulong cas,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.Remove(key, cas, replicateTo, persistTo);
        }

        public Task<IOperationResult> RemoveAsync(
            string key,
            ulong cas,
            ReplicateTo replicateTo,
            PersistTo persistTo)
        {
            return _innerBucket.RemoveAsync(key, cas, replicateTo, persistTo);
        }

        public IDictionary<string, IOperationResult> Remove(
            IList<string> keys)
        {
            return _innerBucket.Remove(keys);
        }

        public IDictionary<string, IOperationResult> Remove(
            IList<string> keys,
            ParallelOptions options)
        {
            return _innerBucket.Remove(keys, options);
        }

        public IDictionary<string, IOperationResult> Remove(
            IList<string> keys,
            ParallelOptions options,
            int rangeSize)
        {
            return _innerBucket.Remove(keys, options, rangeSize);
        }

        public IOperationResult Touch(
            string key,
            TimeSpan expiration)
        {
            return _innerBucket.Touch(key, expiration);
        }

        public Task<IOperationResult> TouchAsync(
            string key,
            TimeSpan expiration)
        {
            return _innerBucket.TouchAsync(key, expiration);
        }

        public IOperationResult<T> GetAndTouch<T>(
            string key,
            TimeSpan expiration)
        {
            return _innerBucket.GetAndTouch<T>(key, expiration);
        }

        public Task<IOperationResult<T>> GetAndTouchAsync<T>(
            string key,
            TimeSpan expiration)
        {
            return _innerBucket.GetAndTouchAsync<T>(key, expiration);
        }

        public IDocumentResult<T> GetAndTouchDocument<T>(
            string key,
            TimeSpan expiration)
        {
            return _innerBucket.GetAndTouchDocument<T>(key, expiration);
        }

        public Task<IDocumentResult<T>> GetAndTouchDocumentAsync<T>(
            string key,
            TimeSpan expiration)
        {
            return _innerBucket.GetAndTouchDocumentAsync<T>(key, expiration);
        }

        public IDocumentResult<T> GetDocument<T>(
            string id)
        {
            return _innerBucket.GetDocument<T>(id);
        }

        public Task<IDocumentResult<T>> GetDocumentAsync<T>(
            string id)
        {
            return _innerBucket.GetDocumentAsync<T>(id);
        }

        public IOperationResult<T> Get<T>(
            string key)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Get", true, false);
                var result = _innerBucket.Get<T>(key);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Get", false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Get", false);
                }
                return result;
            }
            return _innerBucket.Get<T>(key);
        }

        public async Task<IOperationResult<T>> GetAsync<T>(
            string key)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                LogOperationStart(operationId, timer, new [] { key }, "Get", true, true);
                var result = await _innerBucket.GetAsync<T>(key);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, "Get", true);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, "Get", true);
                }
                return result;
            }
            return await _innerBucket.GetAsync<T>(key);
        }

        public IOperationResult<T> GetFromReplica<T>(
            string key)
        {
            return _innerBucket.GetFromReplica<T>(key);
        }

        public Task<IOperationResult<T>> GetFromReplicaAsync<T>(
            string key)
        {
            return _innerBucket.GetFromReplicaAsync<T>(key);
        }

        public IDictionary<string, IOperationResult<T>> Get<T>(
            IList<string> keys)
        {
            if (TimerStrategy != null) {
                var timer = StartTimer();
                var operationId = Guid.NewGuid();
                LogOperationStart(operationId, timer, keys.ToArray(), "GetMulti", true, false);
                var results = _innerBucket.Get<T>(keys);
                if (results.All(e => e.Value.Success || e.Value.Status <= ResponseStatus.KeyExists)) {
                    var keysFound = results.Values.Select(e => e.Status != ResponseStatus.KeyNotFound).ToArray();
                    LogOperationEnd(operationId, timer, keysFound, "GetMulti", false);
                } else {
                    var messages = results.Values.Where(e => e.Message != null).Select(m => m.Message).ToArray();
                    var exceptions = results.Values.Where(e => e.Exception != null).Select(m => m.Exception).ToArray();
                    LogOperationError(operationId, timer, messages, exceptions, "GetMulti", false);
                }
                return results;
            }
            return _innerBucket.Get<T>(keys);
        }

        public IDictionary<string, IOperationResult<T>> Get<T>(
            IList<string> keys,
            ParallelOptions options)
        {
            return _innerBucket.Get<T>(keys, options);
        }

        public IDictionary<string, IOperationResult<T>> Get<T>(
            IList<string> keys,
            ParallelOptions options,
            int rangeSize)
        {
            return _innerBucket.Get<T>(keys, options, rangeSize);
        }

        public IOperationResult<T> GetWithLock<T>(
            string key,
            uint expiration)
        {
            return _innerBucket.GetWithLock<T>(key, expiration);
        }

        public Task<IOperationResult<T>> GetWithLockAsync<T>(
            string key,
            uint expiration)
        {
            return _innerBucket.GetWithLockAsync<T>(key, expiration);
        }

        public IOperationResult<T> GetWithLock<T>(
            string key,
            TimeSpan expiration)
        {
            return _innerBucket.GetWithLock<T>(key, expiration);
        }

        public Task<IOperationResult<T>> GetWithLockAsync<T>(
            string key,
            TimeSpan expiration)
        {
            return _innerBucket.GetWithLockAsync<T>(key, expiration);
        }

        public IOperationResult Unlock(
            string key,
            ulong cas)
        {
            return _innerBucket.Unlock(key, cas);
        }

        public Task<IOperationResult> UnlockAsync(
            string key,
            ulong cas)
        {
            return _innerBucket.UnlockAsync(key, cas);
        }

        public IOperationResult<ulong> Increment(
            string key)
        {
            return _innerBucket.Increment(key);
        }

        public Task<IOperationResult<ulong>> IncrementAsync(
            string key)
        {
            return _innerBucket.IncrementAsync(key);
        }

        public IOperationResult<ulong> Increment(
            string key,
            ulong delta)
        {
            return _innerBucket.Increment(key, delta);
        }

        public Task<IOperationResult<ulong>> IncrementAsync(
            string key,
            ulong delta)
        {
            return _innerBucket.IncrementAsync(key, delta);
        }

        public IOperationResult<ulong> Increment(
            string key,
            ulong delta,
            ulong initial)
        {
            if (TimerStrategy != null) {
                var operationId = Guid.NewGuid();
                var timer = StartTimer();
                var type = "Increment, " + delta + ", " + initial;
                LogOperationStart(operationId, timer, new[] { key }, type, false, false);
                var result = _innerBucket.Increment(key, delta, initial);
                if (result.Success || result.Status <= ResponseStatus.KeyExists) {
                    LogOperationEnd(operationId, timer, result.Status, type, false);
                } else {
                    LogOperationError(operationId, timer, result.Message, result.Exception, type, false);
                }
                return result;
            }
            return _innerBucket.Increment(key, delta, initial);
        }

        public Task<IOperationResult<ulong>> IncrementAsync(
            string key,
            ulong delta,
            ulong initial)
        {
            return _innerBucket.IncrementAsync(key, delta, initial);
        }

        public IOperationResult<ulong> Increment(
            string key,
            ulong delta,
            ulong initial,
            uint expiration)
        {
            return _innerBucket.Increment(key, delta, initial, expiration);
        }

        public Task<IOperationResult<ulong>> IncrementAsync(
            string key,
            ulong delta,
            ulong initial,
            uint expiration)
        {
            return _innerBucket.IncrementAsync(key, delta, initial, expiration);
        }

        public IOperationResult<ulong> Increment(
            string key,
            ulong delta,
            ulong initial,
            TimeSpan expiration)
        {
            return _innerBucket.Increment(key, delta, initial, expiration);
        }

        public Task<IOperationResult<ulong>> IncrementAsync(
            string key,
            ulong delta,
            ulong initial,
            TimeSpan expiration)
        {
            return _innerBucket.IncrementAsync(key, delta, initial, expiration);
        }

        public IOperationResult<ulong> Decrement(
            string key)
        {
            return _innerBucket.Decrement(key);
        }

        public Task<IOperationResult<ulong>> DecrementAsync(
            string key)
        {
            return _innerBucket.DecrementAsync(key);
        }

        public IOperationResult<ulong> Decrement(
            string key,
            ulong delta)
        {
            return _innerBucket.Decrement(key, delta);
        }

        public Task<IOperationResult<ulong>> DecrementAsync(
            string key,
            ulong delta)
        {
            return _innerBucket.DecrementAsync(key, delta);
        }

        public IOperationResult<ulong> Decrement(
            string key,
            ulong delta,
            ulong initial)
        {
            return _innerBucket.Decrement(key, delta, initial);
        }

        public Task<IOperationResult<ulong>> DecrementAsync(
            string key,
            ulong delta,
            ulong initial)
        {
            return _innerBucket.DecrementAsync(key, delta, initial);
        }

        public IOperationResult<ulong> Decrement(
            string key,
            ulong delta,
            ulong initial,
            uint expiration)
        {
            return _innerBucket.Decrement(key, delta, initial, expiration);
        }

        public Task<IOperationResult<ulong>> DecrementAsync(
            string key,
            ulong delta,
            ulong initial,
            uint expiration)
        {
            return _innerBucket.DecrementAsync(key, delta, initial, expiration);
        }

        public IOperationResult<ulong> Decrement(
            string key,
            ulong delta,
            ulong initial,
            TimeSpan expiration)
        {
            return _innerBucket.Decrement(key, delta, initial, expiration);
        }

        public Task<IOperationResult<ulong>> DecrementAsync(
            string key,
            ulong delta,
            ulong initial,
            TimeSpan expiration)
        {
            return _innerBucket.DecrementAsync(key, delta, initial, expiration);
        }

        public IOperationResult<string> Append(
            string key,
            string value)
        {
            return _innerBucket.Append(key, value);
        }

        public Task<IOperationResult<string>> AppendAsync(
            string key,
            string value)
        {
            return _innerBucket.AppendAsync(key, value);
        }

        public IOperationResult<byte[]> Append(
            string key,
            byte[] value)
        {
            return _innerBucket.Append(key, value);
        }

        public Task<IOperationResult<byte[]>> AppendAsync(
            string key,
            byte[] value)
        {
            return _innerBucket.AppendAsync(key, value);
        }

        public IOperationResult<string> Prepend(
            string key,
            string value)
        {
            return _innerBucket.Prepend(key, value);
        }

        public Task<IOperationResult<string>> PrependAsync(
            string key,
            string value)
        {
            return _innerBucket.PrependAsync(key, value);
        }

        public IOperationResult<byte[]> Prepend(
            string key,
            byte[] value)
        {
            return _innerBucket.Prepend(key, value);
        }

        public Task<IOperationResult<byte[]>> PrependAsync(
            string key,
            byte[] value)
        {
            return _innerBucket.PrependAsync(key, value);
        }

        public IViewResult<T> Query<T>(
            IViewQueryable query)
        {
            return _innerBucket.Query<T>(query);
        }

        public Task<IViewResult<T>> QueryAsync<T>(
            IViewQueryable query)
        {
            return _innerBucket.QueryAsync<T>(query);
        }

        public IQueryResult<T> Query<T>(
            string query)
        {
            return _innerBucket.Query<T>(query);
        }

        public Task<IQueryResult<T>> QueryAsync<T>(
            string query)
        {
            return _innerBucket.QueryAsync<T>(query);
        }

        public IQueryResult<T> Query<T>(
            IQueryRequest queryRequest)
        {
            return _innerBucket.Query<T>(queryRequest);
        }

        public Task<IQueryResult<T>> QueryAsync<T>(
            IQueryRequest queryRequest)
        {
            return _innerBucket.QueryAsync<T>(queryRequest);
        }

        public IViewQuery CreateQuery(
            string designDoc,
            string view)
        {
            return _innerBucket.CreateQuery(designDoc, view);
        }

        public IViewQuery CreateQuery(
            string designdoc,
            string view,
            bool development)
        {
            return _innerBucket.CreateQuery(designdoc, view, development);
        }

        public IBucketManager CreateManager(
            string username,
            string password)
        {
            return _innerBucket.CreateManager(username, password);
        }

        public string Name { get { return _innerBucket.Name; } }
        public BucketTypeEnum BucketType { get { return _innerBucket.BucketType; } }
        public ICluster Cluster { get { return _innerBucket.Cluster; } }
        public bool IsSecure { get { return _innerBucket.IsSecure; } }
        public bool SupportsEnhancedDurability { get { return _innerBucket.SupportsEnhancedDurability; } }
        public BucketConfiguration Configuration { get { return _innerBucket.Configuration; } }

        /// <summary>
        /// Returns true if we should be wrapping the couchbase bucket
        /// </summary>
        public static bool IsCouchbaseWrappingNeeded()
        {
            return GlimpseConfiguration.GetConfiguredMessageBroker() != null &&
                   GlimpseConfiguration.GetDefaultRuntimePolicy() != RuntimePolicy.Off &&
                   GlimpseConfiguration.GetRuntimePolicyStategy()() != RuntimePolicy.Off;
        }
    }
}
