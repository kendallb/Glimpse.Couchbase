/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Glimpse.Couchbase.Model
{
    public class ConnectionMetadata
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bucketName">Name of the bucket this connection is for</param>
        public ConnectionMetadata(
            string bucketName)
        {
            BucketName = bucketName;
            Operations = new Dictionary<string, OperationMetadata>();
        }

        public string BucketName { get; private set; }
        public IDictionary<string, OperationMetadata> Operations { get; private set; }
 
        /// <summary>
        /// Register a new operation against the bucket
        /// </summary>
        /// <param name="operation">Operation to register</param>
        public void RegisterOperation(
            OperationMetadata operation)
        {
            Operations.Add(operation.Id, operation);
        }
    }
}