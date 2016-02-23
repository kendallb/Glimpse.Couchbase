/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System;

namespace Glimpse.Couchbase.Message
{
    public class OperationStartedMessage : OperationMessage
    {
        public OperationStartedMessage(
            string bucketName,
            Guid operationId,
            string type,
            string[] keys,
            bool checkDupes,
            bool isAsync)
            : base(bucketName, operationId)
        {
            Type = type;
            Keys = keys;
            CheckDupes = checkDupes;
            IsAsync = isAsync;
        }

        public string Type { get; protected set; }
        public string[] Keys { get; protected set; }
        public bool CheckDupes { get; protected set; }
        public bool IsAsync { get; protected set; }
    }
}