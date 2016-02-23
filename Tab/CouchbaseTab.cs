/*
 * Copyright (C) 2004-2016 AMain.com, Inc.
 * All Rights Reserved
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Glimpse.Couchbase.Model;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;
using Glimpse.Core.Tab.Assist;
using Glimpse.Couchbase.Message;
// ReSharper disable ConvertIfStatementToNullCoalescingExpression

namespace Glimpse.Couchbase.Tab
{
    public class CouchbaseTab : TabBase, ITabSetup, IKey, ITabLayout, IDocumentation, ILayoutControl
    {
        private static object _layout;

        /// <summary>
        /// Name of our tab
        /// </summary>
        public override string Name { get { return "Couchbase"; } }

        /// <summary>
        /// Unique Javsacript key identifier for our tab
        /// </summary>
        public string Key { get { return "glimpse_couchbase"; } }

        /// <summary>
        /// URL to our documentation (null if none)
        /// </summary>
        public string DocumentationUri { get { return null; } }

        /// <summary>
        /// True to display key headings
        /// </summary>
        public bool KeysHeadings { get { return true; } }

        /// <summary>
        /// Called to setup the tab
        /// </summary>
        /// <param name="context">Tab setup context</param>
        public void Setup(
            ITabSetupContext context)
        {
            context.PersistMessages<CouchbaseMessage>();
        }

        /// <summary>
        /// Gets the layout for the tab
        /// </summary>
        /// <returns>Tab layout</returns>
        public object GetLayout()
        {
            if (_layout == null) {
                _layout = TabLayout.Create()
                                   .Cell(
                                       "Couchbase Statistics",
                                       TabLayout.Create().Row(r => {
                                           r.Cell("bucketCount").WidthInPixels(150).WithTitle("# Buckets");
                                           r.Cell("operationCount").WidthInPixels(150).WithTitle("# Operations");
                                           r.Cell("executionTime").WidthInPixels(250).Suffix(" ms").Class("mono").WithTitle("Total execution time");
                                       }))
                                   .Cell(
                                       "Operations",
                                       TabLayout.Create().Row(r => {
                                           r.Cell(0).DisablePreview().SetLayout(TabLayout.Create().Row(x => {
                                               x.Cell(0).WidthInPixels(55);
                                               x.Cell(1).WidthInPixels(55);
                                               x.Cell(2).DisablePreview();
                                               x.Cell(3).WidthInPixels(45);
                                               x.Cell(4).WidthInPixels(85).Suffix(" ms").Class("mono");
                                               x.Cell(5).WidthInPixels(95).Prefix("T+ ").Suffix(" ms").Class("mono");
                                               x.Cell(6).WidthInPixels(45);
                                           }).Row(x => x.Cell(7).SpanColumns(7).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y => {
                                               y.Cell(0).WidthInPercent(20);
                                               y.Cell(1).Class("mono").DisablePreview();
                                           }))));
                                       }))
                                   .Build();
            }
            return _layout;
        }

        /// <summary>
        /// Builds the error header rows
        /// </summary>
        /// <returns>Error header rows</returns>
        private static List<object[]> BuildErrorHeader()
        {
            return new List<object[]> { new object[] { "Error", "Stack" } };
        }

        /// <summary>
        /// Gets the data for the tab
        /// </summary>
        /// <param name="context">Tab context</param>
        /// <returns>Data for the tab</returns>
        public override object GetData(
            ITabContext context)
        {
            var messages = context.GetMessages<CouchbaseMessage>().ToList();
            var aggregator = new MessageAggregator(messages);
            var metadata = aggregator.Aggregate();
            if (metadata == null) {
                return null;
            }

            var operations = new List<object[]> { new object[] { "Operations per Connection" } };

            foreach (var connection in metadata.Connections.Values) {
                if (connection.Operations.Count == 0) {
                    continue;
                }

                var commands = new List<object[]> { new object[] { "Ordinal", "Type", "Keys", "Found", "Duration", "Offset", "Async", "Errors" } };
                var commandCount = 1;
                foreach (var command in connection.Operations.Values) {
                    // Message errors
                    List<object[]> errors = null;
                    if (command.Messages != null) {
                        errors = BuildErrorHeader();
                        errors.AddRange(command.Messages.Select(message => new object[] { message }));
                    }

                    // Exception errors
                    if (command.Exceptions != null) {
                        if (errors == null) {
                            errors = BuildErrorHeader();
                        }
                        errors.AddRange(from ex in command.Exceptions
                                        let exception = ex.GetBaseException()
                                        let exceptionName = ex != exception ? ex.Message + ": " + exception.Message : exception.Message
                                        select new object[] { exceptionName, exception.StackTrace });
                    }

                    // Build the operation line
                    var status = errors != null ? "error" : (command.IsDuplicate ? "warn" : string.Empty);
                    commands.Add(new object[] {
                        commandCount++,
                        command.Type,
                        command.Keys != null ? string.Join("\n", command.Keys) : "",
                        command.KeysFound != null ? string.Join("\n", command.KeysFound) : "",
                        command.Duration,
                        command.Offset,
                        command.IsAsync,
                        errors,
                        status,
                    });
                }

                operations.Add(new[] { commands });
            }

            if (operations.Count > 1) {
                // Build overall statistics
                var executionTime = new TimeSpan();
                executionTime = metadata.Operations.Aggregate(executionTime, (totalDuration, command) => totalDuration + command.Value.Duration);

                return new Dictionary<string, object> {
                    {
                        "Couchbase Statistics", new object[] {
                            new {
                                BucketCount = metadata.Connections.Count,
                                OperationCount = metadata.Operations.Count,
                                ExecutionTime = executionTime,
                            }
                        }
                    },
                    { "Operations", operations }
                };
            }
            return null;
        }
    }
}