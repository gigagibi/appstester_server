using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpAdbClient;

namespace AppsTester.Checker.Android.Adb
{
    internal interface IAdbClientProvider : IDisposable
    {
        IAdbClient GetAdbClient();
    }

    internal class AdbClientProvider : IAdbClientProvider
    {
        private readonly IAdbClient _adbClient;
        private readonly IDeviceMonitor _deviceMonitor;

        public AdbClientProvider(IConfiguration configuration, ILogger<AdbClientProvider> logger)
        {
            var dnsEndPoint = new DnsEndPoint(configuration["Adb:Host"], port: 5037);
            _adbClient = new AdbClient(dnsEndPoint, adbSocketFactory: Factories.AdbSocketFactory);

            logger.LogInformation($"Connecting to ADB server at {configuration["Adb:Host"]}:5037.");

            var version = _adbClient.GetAdbVersion();

            logger.LogInformation(
                $"Successfully connected to ADB server at {configuration["Adb:Host"]}:5037. Version is {version}.");

            _deviceMonitor = new DeviceMonitor(new AdbSocket(dnsEndPoint));

            _deviceMonitor.DeviceConnected += (_, args) =>
                logger.LogInformation("Connected device with serial {Serial}", args.Device.Serial);

            _deviceMonitor.DeviceDisconnected += (_, args) =>
                logger.LogInformation("Disconnected device with serial {Serial}", args.Device.Serial);

            _deviceMonitor.DeviceChanged += (_, args) =>
                logger.LogInformation("Changed device with serial {Serial}", args.Device.Serial);

            _deviceMonitor.Start();
        }

        public IAdbClient GetAdbClient() => _adbClient;

        public void Dispose()
        {
            _deviceMonitor.Dispose();
        }
    }
}