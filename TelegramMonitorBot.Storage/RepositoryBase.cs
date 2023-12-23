using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace TelegramMonitorBot.Storage;

internal class RepositoryBase
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    public RepositoryBase(DynamoClientFactory clientFactory)
    {
        _dynamoDbClient = clientFactory.GetClient();
    }

    public async Task DoSomething(CancellationToken cancellationToken)
    {
        var queryRequest = new QueryRequest("Music")
        {
            TableName = "Music",
            KeyConditionExpression = "#artist = :value",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                { "#artist", "Artist" }
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":value", new AttributeValue { S = "No One You Know" } }
            },
        };
        
        var result = await _dynamoDbClient.QueryAsync(queryRequest, cancellationToken);
    }
    
}
