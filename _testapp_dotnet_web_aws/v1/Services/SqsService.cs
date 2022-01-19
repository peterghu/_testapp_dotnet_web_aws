using _testapp_dotnet_web_aws.v1.Models;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace _testapp_dotnet_web_aws.Clients
{
    /// <summary>
    /// Based off of https://github.com/llatinov/aws.examples.csharp
    /// </summary>
    public interface ISqsService
    {
        Task CreateQueueAsync();

        Task<SqsStatus> GetQueueStatusAsync();

        Task<List<Message>> GetMessagesAsync(CancellationToken cancellationToken = default);

        Task PostMessageAsync<T>(T message);
    }

    public class SqsService : ISqsService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger<SqsService> _logger;
        private readonly ConcurrentDictionary<string, string> _queueUrlCache;
        private IConfiguration _configuration;

        private string _awsRegion;
        private string _queueName;
        private string _queueNameDeadLetter;
        private string _queueLongPollTime;

        public SqsService(IAmazonSQS sqsClient, ILogger<SqsService> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _sqsClient = sqsClient;
            _logger = logger;

            _awsRegion = _configuration["AWS_REGION"];

            _queueUrlCache = new ConcurrentDictionary<string, string>();
            _queueName = _configuration["AWS_SQS_QUEUE_NAME"];

            _queueNameDeadLetter = _configuration["AWS_SQS_QUEUE_NAME"] + "-exceptions";
            _queueLongPollTime = _configuration["AWS_SQS_LONG_POLL_TIME"];
        }

        public async Task CreateQueueAsync()
        {
            const string arnAttribute = "QueueArn";

            try
            {
                var createQueueRequest = new CreateQueueRequest();

                //if (_appConfig.AwsQueueIsFifo)
                //{
                //    createQueueRequest.Attributes.Add("FifoQueue", "true");
                //}

                createQueueRequest.QueueName = _queueName;
                var createQueueResponse = await _sqsClient.CreateQueueAsync(createQueueRequest);
                createQueueRequest.QueueName = _queueNameDeadLetter;
                var createDeadLetterQueueResponse = await _sqsClient.CreateQueueAsync(createQueueRequest);

                // Get the the ARN of dead letter queue and configure main queue to deliver messages to it
                var attributes = await _sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = createDeadLetterQueueResponse.QueueUrl,
                    AttributeNames = new List<string> { arnAttribute }
                });
                var deadLetterQueueArn = attributes.Attributes[arnAttribute];

                // RedrivePolicy on main queue to deliver messages to dead letter queue if they fail processing after 3 times
                var redrivePolicy = new
                {
                    maxReceiveCount = "3",
                    deadLetterTargetArn = deadLetterQueueArn
                };
                await _sqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
                {
                    QueueUrl = createQueueResponse.QueueUrl,
                    Attributes = new Dictionary<string, string>
                    {
                        {"RedrivePolicy", JsonConvert.SerializeObject(redrivePolicy)},
                        // Enable Long polling
                        {"ReceiveMessageWaitTimeSeconds", _queueLongPollTime}
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when creating SQS queue {_queueName} and {_queueNameDeadLetter}");
            }
        }

        public async Task<SqsStatus> GetQueueStatusAsync()
        {
            var queueUrl = await GetQueueUrl(_queueName);

            try
            {
                var attributes = new List<string> { "ApproximateNumberOfMessages", "ApproximateNumberOfMessagesNotVisible", "LastModifiedTimestamp" };
                var response = await _sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest(queueUrl, attributes));

                return new SqsStatus
                {
                    IsHealthy = response.HttpStatusCode == HttpStatusCode.OK,
                    Region = _awsRegion,
                    QueueName = _queueName,
                    LongPollTimeSeconds = Int32.Parse(_queueLongPollTime),
                    ApproximateNumberOfMessages = response.ApproximateNumberOfMessages,
                    ApproximateNumberOfMessagesNotVisible = response.ApproximateNumberOfMessagesNotVisible,
                    LastModifiedTimestamp = response.LastModifiedTimestamp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to GetNumberOfMessages for queue {_queueName}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Message>> GetMessagesAsync(string queueName, CancellationToken cancellationToken = default)
        {
            var queueUrl = await GetQueueUrl(queueName);

            try
            {
                var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    WaitTimeSeconds = Int32.Parse(_queueLongPollTime),
                    AttributeNames = new List<string> { "ApproximateReceiveCount" },
                    MessageAttributeNames = new List<string> { "*" }
                }, cancellationToken);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new AmazonSQSException($"Failed to GetMessagesAsync for queue {queueName}. Response: {response.HttpStatusCode}");
                }

                // remove message when done. ReceiveMessagesAsync only returns one message per call?
                response.Messages.ForEach(async message =>
                {
                    var messageType = message.MessageAttributes.GetMessageTypeAttributeValue();
                    if (messageType != null)
                    {
                        //await PostMessageAsync(message.Body, messageType);
                        await DeleteMessageAsync(queueName, message.ReceiptHandle);
                    }
                });

                return response.Messages;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning($"Failed to GetMessagesAsync for queue {queueName} because the task was canceled");
                return new List<Message>();
            }
            catch (Exception)
            {
                _logger.LogError($"Failed to GetMessagesAsync for queue {queueName}");
                throw;
            }
        }

        public async Task<List<Message>> GetMessagesAsync(CancellationToken cancellationToken = default)
        {
            return await GetMessagesAsync(_queueName, cancellationToken);
        }

        public async Task PostMessageAsync<T>(T message)
        {
            await PostMessageAsync(_configuration["AWS_SQS_QUEUE_NAME"], message);
        }

        public async Task PostMessageAsync<T>(string queueName, T message)
        {
            var queueUrl = await GetQueueUrl(queueName);

            try
            {
                var sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = queueUrl,
                    MessageBody = JsonConvert.SerializeObject(message),
                    MessageAttributes = SqsMessageTypeAttribute.CreateAttributes<T>()
                };

                //if (_appConfig.AwsQueueIsFifo)
                //{
                //    sendMessageRequest.MessageGroupId = messageType;
                //    sendMessageRequest.MessageDeduplicationId = Guid.NewGuid().ToString();
                //}

                await _sqsClient.SendMessageAsync(sendMessageRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to PostMessagesAsync to queue '{queueName}'. Exception: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteMessageAsync(string queueName, string receiptHandle)
        {
            var queueUrl = await GetQueueUrl(queueName);

            try
            {
                var response = await _sqsClient.DeleteMessageAsync(queueUrl, receiptHandle);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new AmazonSQSException($"Failed to DeleteMessageAsync with for [{receiptHandle}] from queue '{queueName}'. Response: {response.HttpStatusCode}");
                }
            }
            catch (Exception)
            {
                _logger.LogError($"Failed to DeleteMessageAsync from queue {queueName}");
                throw;
            }
        }

        private async Task<string> GetQueueUrl(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentException("Queue name should not be blank.");
            }

            if (_queueUrlCache.TryGetValue(queueName, out var result))
            {
                return result;
            }

            try
            {
                var response = await _sqsClient.GetQueueUrlAsync(queueName);
                return _queueUrlCache.AddOrUpdate(queueName, response.QueueUrl, (q, url) => url);
            }
            catch (QueueDoesNotExistException ex)
            {
                throw new InvalidOperationException($"Could not retrieve the URL for the queue '{queueName}' as it does not exist or you do not have access to it.", ex);
            }
        }


    }
}