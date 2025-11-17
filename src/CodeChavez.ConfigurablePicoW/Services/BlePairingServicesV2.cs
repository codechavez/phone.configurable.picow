using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CodeChavez.ConfigurablePicoW;



public class BlePairingServicesV2
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;

    public BlePairingServicesV2()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
    }

    public event Action<IDevice>? DeviceDiscovered;

    public async Task<IList<IDevice>> StartDiscoveryAsync(TimeSpan scanDuration)
    {
        var devices = new ConcurrentDictionary<string, IDevice>();

        void handler(object? sender, DeviceEventArgs e)
        {
            if (devices.TryAdd(e.Device.Id.ToString(), e.Device))
                DeviceDiscovered?.Invoke(e.Device);
        }

        _adapter.DeviceDiscovered += handler;

        try
        {
            await _adapter.StartScanningForDevicesAsync(new[] { BleConstants.PairService });

            // Wait for the scan duration
            await Task.Delay(scanDuration);

            await _adapter.StopScanningForDevicesAsync();
        }
        finally
        {
            _adapter.DeviceDiscovered -= handler;
        }

        return devices.Values.ToList();
    }

    public async Task<bool> SendCredentialsAsync(IDevice device, PairingPayload payload, CancellationToken cancellationToken = default)
    {
        if (device == null) return false;

        try
        {
            await _adapter.ConnectToDeviceAsync(device, cancellationToken: cancellationToken);

            var service = await device.GetServiceAsync(BleConstants.PairService);
            if (service == null) return false;

            var rx = await service.GetCharacteristicAsync(BleConstants.RxCharacteristic);
            if (rx == null) return false;

            string json = JsonSerializer.Serialize(payload);
            byte[] data = Encoding.UTF8.GetBytes(json);

            await rx.WriteAsync(data);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { await _adapter.DisconnectDeviceAsync(device); } catch { }
        }
    }
}
