/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System;

namespace Glimpse.Couchbase.Model
{
    public class OperationMetadata
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">ID for this operation</param>
        /// <param name="bucketName">Name of the bucket being used</param>
        public OperationMetadata(
            string id,
            string bucketName)
        {
            Id = id;
            BucketName = bucketName;
        }

        /// <summary>
        /// Gets the id of the operation
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the bucket name related with the operation
        /// </summary>
        public string BucketName { get; private set; }

        /// <summary>
        /// Operation type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Operation key
        /// </summary>
        public string[] Keys { get; set; }

        /// <summary>
        /// True if the key was found
        /// </summary>
        public bool[] KeysFound { get; set; }

        /// <summary>
        /// If the operation wasn't succesful, a message indicating why it was not successful.
        /// </summary>
        public string[] Messages { get; set; }

        /// <summary>
        /// If Success is false and an exception has been caught internally, this field will contain the exception.
        /// </summary>
        public Exception[] Exceptions { get; set; }

        /// <summary>
        /// Gets or sets the time when the operation started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the time when the operation ended.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the duration of how long the operation took to execute.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the time offset of went the operation happened.
        /// </summary>
        public TimeSpan Offset { get; set; }

        /// <summary>
        /// Gets or sets whether the command is a duplicate operation.
        /// </summary>
        public bool IsDuplicate { get; set; }

        /// <summary>
        /// Gets or sets whether the command was executed through the async API.
        /// </summary>
        public bool IsAsync { get; set; }
    }
}