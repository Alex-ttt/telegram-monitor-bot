using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace TelegramMonitorBot.DynamoDBMigrator;

internal static class IAmazonDynamoDBExtensions
{
    internal static Task ExecuteRequest(
        this IAmazonDynamoDB client, 
        AmazonDynamoDBRequest request,
        CancellationToken cancellationToken)
    {
        Task result = request switch
        {
            BatchExecuteStatementRequest r => client.BatchExecuteStatementAsync(r, cancellationToken),
            BatchGetItemRequest r => client.BatchGetItemAsync(r, cancellationToken),
            BatchWriteItemRequest r => client.BatchWriteItemAsync(r, cancellationToken),
            CreateBackupRequest r => client.CreateBackupAsync(r, cancellationToken),
            CreateGlobalTableRequest r => client.CreateGlobalTableAsync(r, cancellationToken),
            CreateTableRequest r => client.CreateTableAsync(r, cancellationToken),
            DeleteBackupRequest r => client.DeleteBackupAsync(r, cancellationToken),
            DeleteItemRequest r => client.DeleteItemAsync(r, cancellationToken),
            DeleteTableRequest r => client.DeleteTableAsync(r, cancellationToken),
            DescribeBackupRequest r => client.DescribeBackupAsync(r, cancellationToken),
            DescribeContinuousBackupsRequest r => client.DescribeContinuousBackupsAsync(r, cancellationToken),
            DescribeContributorInsightsRequest r => client.DescribeContributorInsightsAsync(r, cancellationToken),
            DescribeEndpointsRequest r => client.DescribeEndpointsAsync(r, cancellationToken),
            DescribeExportRequest r => client.DescribeExportAsync(r, cancellationToken),
            DescribeGlobalTableRequest r => client.DescribeGlobalTableAsync(r, cancellationToken),
            DescribeGlobalTableSettingsRequest r => client.DescribeGlobalTableSettingsAsync(r, cancellationToken),
            DescribeImportRequest r => client.DescribeImportAsync(r, cancellationToken),
            DescribeKinesisStreamingDestinationRequest r => client.DescribeKinesisStreamingDestinationAsync(r, cancellationToken),
            DescribeLimitsRequest r => client.DescribeLimitsAsync(r, cancellationToken),
            DescribeTableReplicaAutoScalingRequest r => client.DescribeTableReplicaAutoScalingAsync(r, cancellationToken),
            DescribeTableRequest r => client.DescribeTableAsync(r, cancellationToken),
            DescribeTimeToLiveRequest r => client.DescribeTimeToLiveAsync(r, cancellationToken),
            DisableKinesisStreamingDestinationRequest r => client.DisableKinesisStreamingDestinationAsync(r, cancellationToken),
            EnableKinesisStreamingDestinationRequest r => client.EnableKinesisStreamingDestinationAsync(r, cancellationToken),
            ExecuteStatementRequest r => client.ExecuteStatementAsync(r, cancellationToken),
            ExecuteTransactionRequest r => client.ExecuteTransactionAsync(r, cancellationToken),
            ExportTableToPointInTimeRequest r => client.ExportTableToPointInTimeAsync(r, cancellationToken),
            GetItemRequest r => client.GetItemAsync(r, cancellationToken),
            ImportTableRequest r => client.ImportTableAsync(r, cancellationToken),
            ListBackupsRequest r => client.ListBackupsAsync(r, cancellationToken),
            ListContributorInsightsRequest r => client.ListContributorInsightsAsync(r, cancellationToken),
            ListExportsRequest r => client.ListExportsAsync(r, cancellationToken),
            ListGlobalTablesRequest r => client.ListGlobalTablesAsync(r, cancellationToken),
            ListImportsRequest r => client.ListImportsAsync(r, cancellationToken),
            ListTablesRequest r => client.ListTablesAsync(r, cancellationToken),
            ListTagsOfResourceRequest r => client.ListTagsOfResourceAsync(r, cancellationToken),
            PutItemRequest r => client.PutItemAsync(r, cancellationToken),
            QueryRequest r => client.QueryAsync(r, cancellationToken),
            RestoreTableFromBackupRequest r => client.RestoreTableFromBackupAsync(r, cancellationToken),
            RestoreTableToPointInTimeRequest r => client.RestoreTableToPointInTimeAsync(r, cancellationToken),
            ScanRequest r => client.ScanAsync(r, cancellationToken),
            TagResourceRequest r => client.TagResourceAsync(r, cancellationToken),
            TransactGetItemsRequest r => client.TransactGetItemsAsync(r, cancellationToken),
            TransactWriteItemsRequest r => client.TransactWriteItemsAsync(r, cancellationToken),
            UntagResourceRequest r => client.UntagResourceAsync(r, cancellationToken),
            UpdateContinuousBackupsRequest r => client.UpdateContinuousBackupsAsync(r, cancellationToken),
            UpdateContributorInsightsRequest r => client.UpdateContributorInsightsAsync(r, cancellationToken),
            UpdateGlobalTableSettingsRequest r => client.UpdateGlobalTableSettingsAsync(r, cancellationToken),
            UpdateItemRequest r => client.UpdateItemAsync(r, cancellationToken),
            UpdateTableReplicaAutoScalingRequest r => client.UpdateTableReplicaAutoScalingAsync(r, cancellationToken),
            UpdateTableRequest r => client.UpdateTableAsync(r, cancellationToken),
            UpdateTimeToLiveRequest r => client.UpdateTimeToLiveAsync(r, cancellationToken)
        };

        return result;
    }
}
