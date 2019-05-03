// <copyright file="ServiceContextExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.HueBot.Extensions
{
    using System;
    using System.Fabric;
    using System.Threading.Tasks;
    using Microsoft.Graph.Communications.Common;

    /// <summary>
    /// Extensions for Service Context from Service Fabric.
    /// </summary>
    public static class ServiceContextExtensions
    {
        /// <summary>
        /// Loads the partition data.
        /// </summary>
        /// <param name="context">Service context from Service Fabric.</param>
        /// <returns>The task.</returns>
        /// <exception cref="NotSupportedException">
        /// The partition info is incorrect / is in an unexpected format.
        /// </exception>
        public static async Task<(long key, int count)> LoadPartitionDataAsync(this ServiceContext context)
        {
            Validator.NotNull(context, nameof(context), "Cannot load partition data without context.");

            using (var client = new FabricClient())
            {
                var partitionList = await client.QueryManager.GetPartitionListAsync(context.ServiceName).ConfigureAwait(false);

                foreach (var partition in partitionList)
                {
                    if (partition.PartitionInformation is Int64RangePartitionInformation partInfo &&
                        partition.PartitionInformation.Id == context.PartitionId)
                    {
                        if (partInfo.LowKey != partInfo.HighKey)
                        {
                            throw new NotSupportedException($"Load partition failed. High key != Low key. Low Key: {partInfo.LowKey}, High Key: {partInfo.HighKey}");
                        }

                        return (partInfo.LowKey, partitionList.Count);
                    }
                }
            }

            throw new NotSupportedException("Unable to locate partition.");
        }

        /// <summary>
        /// Get the node instance id.
        /// NodeName is of the format _Node_3 where 3 is the instance id.
        /// </summary>
        /// <param name="serviceContext">Azure Service Fabric service context.</param>
        /// <returns>Instance id.</returns>
        public static int NodeInstance(this ServiceContext serviceContext)
        {
            var nodeName = serviceContext.NodeContext.NodeName;
            return int.Parse(nodeName.Substring(nodeName.LastIndexOf('_') + 1));
        }
    }
}
