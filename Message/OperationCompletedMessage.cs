/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System;
using Glimpse.Core.Message;

namespace Glimpse.Couchbase.Message
{
    public class OperationCompletedMessage : OperationMessage, ITimelineMessage
    {
        public OperationCompletedMessage(
            string bucketName,
            Guid operationId,
            bool[] keysFound,
            bool isAsync = false)
            : base(bucketName, operationId)
        {
            KeysFound = keysFound;
            IsAsync = isAsync;
        }

        public bool[] KeysFound { get; set; }
        public bool IsAsync { get; private set; }
        public string EventName { get; set; }
        public TimelineCategoryItem EventCategory { get; set; }
        public string EventSubText { get; set; }
    }
}