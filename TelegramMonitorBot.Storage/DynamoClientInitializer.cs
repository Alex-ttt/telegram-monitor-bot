using Amazon;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Options;
using TelegramMonitorBot.Configuration.Options;

namespace TelegramMonitorBot.Storage;

internal class DynamoClientInitializer
{
    private readonly IOptions<AwsOptions> _awsOptions;

    internal DynamoClientInitializer(IOptions<AwsOptions> awsOptions)
    {
        _awsOptions = awsOptions;
    }

    public AmazonDynamoDBClient GetClient()
    {
        var clientConfig = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(_awsOptions.Value.Region)
        };
        

        return new AmazonDynamoDBClient(clientConfig); 
    }
}
