/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System;
using Glimpse.Core.Message;

namespace Glimpse.Couchbase.Message
{
    public abstract class CouchbaseMessage : ITimedMessage
    {
        protected CouchbaseMessage(
            string bucketName)
        {
            Id = Guid.NewGuid();
            BucketName = bucketName;
            StartTime = DateTime.Now;
        }

        public Guid Id { get; private set; }
        public string BucketName { get; private set; }
        public TimeSpan Offset { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
    }
}