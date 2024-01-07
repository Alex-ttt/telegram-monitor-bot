using Amazon;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Options;
using TelegramMonitorBot.Configuration.Options;

namespace TelegramMonitorBot.Storage;

public class DynamoClientFactory
{
    private readonly IOptions<AwsOptions> _awsOptions;

    public DynamoClientFactory(IOptions<AwsOptions> awsOptions)
    {
        _awsOptions = awsOptions;
    }

    public AmazonDynamoDBClient GetClient()
    {
        var clientConfig = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(_awsOptions.Value.Region),
        };

        if (_awsOptions.Value.DynamoDb?.ServiceURL is { } serviceUrl)
        {
            clientConfig.ServiceURL = serviceUrl;
        }
        

        return new AmazonDynamoDBClient(clientConfig); 
    }
}
