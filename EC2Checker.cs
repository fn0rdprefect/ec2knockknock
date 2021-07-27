using System;
using System.IO;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.EC2.Util;
using Amazon.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace ec2whitelist {

    public class AWSOptions 
    {
        public const string ConfigSection = "AWSOptions";
        public string ProfileName {get; set;}
        public string Region {get; set;}
    }
    public class EC2CheckerService : IHostedService, IDisposable 
    {
        private readonly string _awsConfigFilePath;
        private readonly ILogger _logger;
        private IOptions<AWSOptions> _appConfig;
        private Timer _timer;
                private Amazon.EC2.AmazonEC2Client _awsClient;
        public EC2CheckerService(ILogger<EC2CheckerService> logger, IOptions<AWSOptions> appConfig)
        {
            if(!System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), ".aws/credentials")))
            {
                _awsConfigFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), ".aws/config");
            }
            
            _logger = logger;
            _appConfig = appConfig;
            
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            
            var credChain = new Amazon.Runtime.CredentialManagement.CredentialProfileStoreChain(_awsConfigFilePath);
            AWSCredentials awsCredentials;
            if (credChain.TryGetAWSCredentials(_appConfig.Value.ProfileName, out awsCredentials))
            {
                var _regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_appConfig.Value.Region); 
                if (_regionEndpoint?.DisplayName == "Unknown)") 
                {
                    _logger.LogWarning($"Region {_appConfig.Value.Region} does not exist. Defaulting to eu-west-1"); _regionEndpoint = Amazon.RegionEndpoint.EUWest1;
                }
                _awsClient = new AmazonEC2Client(awsCredentials, _regionEndpoint);
            }
            _timer = new System.Threading.Timer(
                DoWork,
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        public async void DoWork(object state)
        {
            DescribeInstancesResponse InstanceList = await _awsClient.DescribeInstancesAsync();
            foreach (Reservation _reservation in InstanceList.Reservations)
            {
                _logger.LogInformation($"Reservation ID {_reservation.ReservationId} has the following instances:");
                foreach (Instance _instance in _reservation.Instances)
                {
                    _logger.LogInformation($"Instance ID {_instance.InstanceId} has IP Address {_instance.PublicIpAddress}");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

    }
}