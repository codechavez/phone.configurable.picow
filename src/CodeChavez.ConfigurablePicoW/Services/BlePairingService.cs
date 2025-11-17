using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Text;
using System.Text.Json;

namespace CodeChavez.ConfigurablePicoW;

public class BlePairingService
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;

    public BlePairingService()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
    }

    public event Action<IDevice>? DeviceDiscovered;


    public void StartDiscovery()
    {
        _adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
        _adapter.StartScanningForDevicesAsync(new[] { BleConstants.PairService });
    }

    public void StopDiscovery()
    {
        try
        {
            _adapter.StopScanningForDevicesAsync().Wait();
        }
        catch { /* ignore */ }
        _adapter.DeviceDiscovered -= Adapter_DeviceDiscovered;
    }

    private void Adapter_DeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
    {
        DeviceDiscovered?.Invoke(e.Device);
    }

    public async Task<bool> SendCredentialsAsync(IDevice device, PairingPayload payload, CancellationToken cancellationToken = default)
    {
        if (device == null) return false;

        try
        {
            // Connect to the device
            await _adapter.ConnectToDeviceAsync(device, cancellationToken: cancellationToken);

            // Get pairing service
            var service = await device.GetServiceAsync(BleConstants.PairService);
            if (service == null) return false;

            // Get RX characteristic (phone -> pico)
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
