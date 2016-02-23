/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System;
using Glimpse.Core.Message;

namespace Glimpse.Couchbase.Message
{
    public class OperationErrorMessage : OperationMessage, ITimelineMessage
    {
        public OperationErrorMessage(
            string bucketName,
            Guid operationId,
            string[] messages,
            Exception[] exceptions,
            bool isAsync = false)
            : base(bucketName, operationId)
        {
            Messages = messages;
            Exceptions = exceptions;
            IsAsync = isAsync;
        }

        public string[] Messages { get; protected set; }
        public Exception[] Exceptions { get; protected set; }
        public bool IsAsync { get; protected set; }
        public string EventName { get; set; }
        public TimelineCategoryItem EventCategory { get; set; }
        public string EventSubText { get; set; }
    }
}