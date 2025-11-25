// ==========================================================================
// T028: CosmosDbSchema - Cosmos DB Schema Configuration
// ==========================================================================
// Defines partition keys, indexing policies, and TTL configuration
// ==========================================================================

using System.Collections.ObjectModel;
using Microsoft.Azure.Cosmos;

namespace SCIMGateway.Core.Data;

/// <summary>
/// Container configuration for Cosmos DB.
/// </summary>
public class ContainerConfiguration
{
    /// <summary>
    /// Partition key path (e.g., /tenantId).
    /// </summary>
    public string PartitionKeyPath { get; set; } = "/tenantId";

    /// <summary>
    /// Default time to live in seconds.
    /// </summary>
    public int? DefaultTimeToLive { get; set; }

    /// <summary>
    /// Property path for TTL.
    /// </summary>
    public string? TimeToLivePropertyPath { get; set; }

    /// <summary>
    /// Whether per-item TTL is enabled.
    /// </summary>
    public bool EnablePerItemTtl { get; set; }
}

/// <summary>
/// Indexing policy configuration.
/// </summary>
public class IndexingPolicyConfiguration
{
    /// <summary>
    /// Included paths for indexing.
    /// </summary>
    public List<string> IncludedPaths { get; set; } = [];

    /// <summary>
    /// Excluded paths from indexing.
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = [];

    /// <summary>
    /// Composite indexes for efficient queries.
    /// </summary>
    public List<List<string>> CompositeIndexes { get; set; } = [];
}

/// <summary>
/// Unique key policy configuration.
/// </summary>
public class UniqueKeyPolicyConfiguration
{
    /// <summary>
    /// Paths that form unique keys.
    /// </summary>
    public List<string> UniqueKeyPaths { get; set; } = [];
}

/// <summary>
/// Cosmos DB schema manager for creating and configuring containers.
/// </summary>
public class CosmosDbSchema
{
    /// <summary>
    /// TTL for audit logs: 90 days in seconds.
    /// </summary>
    public const int AuditLogsTtlSeconds = 90 * 24 * 60 * 60; // 7,776,000 seconds

    /// <summary>
    /// Gets container configuration for a given container name.
    /// </summary>
    public static ContainerConfiguration GetContainerConfiguration(string containerName)
    {
        return containerName.ToLowerInvariant() switch
        {
            "users" => new ContainerConfiguration
            {
                PartitionKeyPath = "/tenantId",
                DefaultTimeToLive = null,
                EnablePerItemTtl = false
            },
            "groups" => new ContainerConfiguration
            {
                PartitionKeyPath = "/tenantId",
                DefaultTimeToLive = null,
                EnablePerItemTtl = false
            },
            "sync-state" => new ContainerConfiguration
            {
                PartitionKeyPath = "/tenantId",
                DefaultTimeToLive = null,
                EnablePerItemTtl = false
            },
            "transformation-rules" => new ContainerConfiguration
            {
                PartitionKeyPath = "/tenantId",
                DefaultTimeToLive = null,
                EnablePerItemTtl = false
            },
            "audit-logs" => new ContainerConfiguration
            {
                PartitionKeyPath = "/tenantId",
                DefaultTimeToLive = AuditLogsTtlSeconds,
                TimeToLivePropertyPath = "/ttl",
                EnablePerItemTtl = true
            },
            _ => throw new ArgumentException($"Unknown container: {containerName}")
        };
    }

    /// <summary>
    /// Gets indexing policy for a given container name.
    /// </summary>
    public static IndexingPolicyConfiguration GetIndexingPolicy(string containerName)
    {
        return containerName.ToLowerInvariant() switch
        {
            "users" => new IndexingPolicyConfiguration
            {
                IncludedPaths =
                [
                    "/tenantId/?",
                    "/userName/?",
                    "/externalId/?",
                    "/emails/*",
                    "/active/?",
                    "/displayName/?",
                    "/meta/created/?",
                    "/meta/lastModified/?"
                ],
                ExcludedPaths =
                [
                    "/password/*",
                    "/_etag/?"
                ],
                CompositeIndexes =
                [
                    ["/tenantId", "/userName"],
                    ["/tenantId", "/meta/lastModified desc"]
                ]
            },
            "groups" => new IndexingPolicyConfiguration
            {
                IncludedPaths =
                [
                    "/tenantId/?",
                    "/displayName/?",
                    "/externalId/?",
                    "/members/*",
                    "/meta/created/?",
                    "/meta/lastModified/?"
                ],
                ExcludedPaths =
                [
                    "/_etag/?"
                ],
                CompositeIndexes =
                [
                    ["/tenantId", "/displayName"],
                    ["/tenantId", "/meta/lastModified desc"]
                ]
            },
            "sync-state" => new IndexingPolicyConfiguration
            {
                IncludedPaths =
                [
                    "/tenantId/?",
                    "/adapterId/?",
                    "/lastSyncTime/?",
                    "/status/?"
                ],
                ExcludedPaths =
                [
                    "/_etag/?"
                ],
                CompositeIndexes = []
            },
            "transformation-rules" => new IndexingPolicyConfiguration
            {
                IncludedPaths =
                [
                    "/tenantId/?",
                    "/ruleId/?",
                    "/sourceAttribute/?",
                    "/targetAttribute/?",
                    "/enabled/?"
                ],
                ExcludedPaths =
                [
                    "/_etag/?"
                ],
                CompositeIndexes = []
            },
            "audit-logs" => new IndexingPolicyConfiguration
            {
                IncludedPaths =
                [
                    "/tenantId/?",
                    "/timestamp/?",
                    "/operation/?",
                    "/resourceType/?",
                    "/resourceId/?",
                    "/actorId/?",
                    "/statusCode/?"
                ],
                ExcludedPaths =
                [
                    "/requestBody/*",
                    "/responseBody/*",
                    "/_etag/?"
                ],
                CompositeIndexes =
                [
                    ["/tenantId", "/timestamp desc"],
                    ["/tenantId", "/operation", "/timestamp desc"],
                    ["/tenantId", "/resourceType", "/timestamp desc"]
                ]
            },
            _ => throw new ArgumentException($"Unknown container: {containerName}")
        };
    }

    /// <summary>
    /// Gets unique key policy for a given container name.
    /// </summary>
    public static UniqueKeyPolicyConfiguration GetUniqueKeyPolicy(string containerName)
    {
        return containerName.ToLowerInvariant() switch
        {
            "users" => new UniqueKeyPolicyConfiguration
            {
                UniqueKeyPaths = ["/userName"]
            },
            "groups" => new UniqueKeyPolicyConfiguration
            {
                UniqueKeyPaths = ["/displayName"]
            },
            "sync-state" => new UniqueKeyPolicyConfiguration
            {
                UniqueKeyPaths = ["/adapterId"]
            },
            "transformation-rules" => new UniqueKeyPolicyConfiguration
            {
                UniqueKeyPaths = ["/ruleId"]
            },
            "audit-logs" => new UniqueKeyPolicyConfiguration
            {
                // Audit logs don't need unique keys (logs are append-only)
                UniqueKeyPaths = []
            },
            _ => throw new ArgumentException($"Unknown container: {containerName}")
        };
    }

    /// <summary>
    /// Creates container properties for Cosmos DB.
    /// </summary>
    public static ContainerProperties CreateContainerProperties(string containerName)
    {
        var config = GetContainerConfiguration(containerName);
        var indexingPolicy = GetIndexingPolicy(containerName);
        var uniqueKeyPolicy = GetUniqueKeyPolicy(containerName);

        var properties = new ContainerProperties(containerName, config.PartitionKeyPath);

        // Set TTL
        if (config.DefaultTimeToLive.HasValue)
        {
            properties.DefaultTimeToLive = config.DefaultTimeToLive.Value;
        }

        // Set indexing policy
        properties.IndexingPolicy = new IndexingPolicy
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true
        };

        // Add included paths
        foreach (var path in indexingPolicy.IncludedPaths)
        {
            properties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = path });
        }

        // Add excluded paths
        foreach (var path in indexingPolicy.ExcludedPaths)
        {
            properties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = path });
        }

        // Add composite indexes
        foreach (var composite in indexingPolicy.CompositeIndexes)
        {
            var compositeIndex = new Collection<CompositePath>();
            foreach (var path in composite)
            {
                var isDesc = path.EndsWith(" desc", StringComparison.OrdinalIgnoreCase);
                var cleanPath = isDesc ? path[..^5].Trim() : path;
                compositeIndex.Add(new CompositePath
                {
                    Path = cleanPath,
                    Order = isDesc ? CompositePathSortOrder.Descending : CompositePathSortOrder.Ascending
                });
            }
            properties.IndexingPolicy.CompositeIndexes.Add(compositeIndex);
        }

        // Set unique key policy
        if (uniqueKeyPolicy.UniqueKeyPaths.Count > 0)
        {
            properties.UniqueKeyPolicy = new UniqueKeyPolicy();
            var uniqueKey = new UniqueKey();
            foreach (var path in uniqueKeyPolicy.UniqueKeyPaths)
            {
                uniqueKey.Paths.Add(path);
            }
            properties.UniqueKeyPolicy.UniqueKeys.Add(uniqueKey);
        }

        return properties;
    }

    /// <summary>
    /// Creates all containers in the database.
    /// </summary>
    public static async Task CreateAllContainersAsync(Database database, int throughput = 400)
    {
        var containerNames = new[]
        {
            CosmosContainerNames.Users,
            CosmosContainerNames.Groups,
            CosmosContainerNames.SyncState,
            CosmosContainerNames.TransformationRules,
            CosmosContainerNames.AuditLogs
        };

        foreach (var containerName in containerNames)
        {
            var properties = CreateContainerProperties(containerName);
            await database.CreateContainerIfNotExistsAsync(properties, throughput);
        }
    }
}
