/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System;

namespace Glimpse.Couchbase.Message
{
    public abstract class OperationMessage : CouchbaseMessage
    {
        protected OperationMessage(
            string bucketName,
            Guid operationId)
            : base(bucketName)
        {
            OperationId = operationId;
        }

        public Guid OperationId { get; protected set; }
    }
}