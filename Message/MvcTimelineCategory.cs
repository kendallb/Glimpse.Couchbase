/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using Glimpse.Core.Message;

namespace Glimpse.Couchbase.Message
{
    public class MvcTimelineCategory : TimelineCategory
    {
        private static readonly TimelineCategoryItem _operation = new TimelineCategoryItem("Couchbase", "#2320FF", "#DD31DA");
         
        /// <summary>
        /// Gets the timeline category for a couchbase operation.
        /// </summary>
        public static TimelineCategoryItem Operation
        {
            get { return _operation; }
        }
    }
}
