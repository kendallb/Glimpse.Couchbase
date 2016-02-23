/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System.Collections.Generic;

namespace Glimpse.Couchbase.Model
{
    public class AggregateMetadata
    {
        public AggregateMetadata()
        {
            Connections = new Dictionary<string, ConnectionMetadata>();
            Operations = new Dictionary<string, OperationMetadata>();
        }

        public IDictionary<string, ConnectionMetadata> Connections { get; private set; }
        public IDictionary<string, OperationMetadata> Operations { get; private set; }
    }
}
