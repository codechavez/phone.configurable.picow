using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CodeChavez.ConfigurablePicoW;

public partial class MainViewModel : ObservableObject
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;

    public ObservableCollection<IDevice> Devices { get; } = new();

    [ObservableProperty]
    private string wifiSsid = string.Empty;

    [ObservableProperty]
    private string wifiPassword = string.Empty;

    [ObservableProperty]
    private string mqttBroker = string.Empty;

    [ObservableProperty]
    private int mqttPort = 1883;

    [ObservableProperty]
    private string mqttTopic = "devices/ble/detections";

    [ObservableProperty]
    private IDevice? selectedDevice;

    [ObservableProperty]
    private bool isScanning;

    public MainViewModel()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;

        _ble.StateChanged += (s, e) => Debug.WriteLine($"Bluetooth state changed to: {e.NewState}");
    }

    public async Task<bool> EnsureBlePermissionsAsync()
    {
        var scan = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
        var connect = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
        var location = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (scan != PermissionStatus.Granted ||
            connect != PermissionStatus.Granted)
        {
            scan = await Permissions.RequestAsync<Permissions.Bluetooth>();
            connect = await Permissions.RequestAsync<Permissions.Bluetooth>();
        }

        // Some BLE libraries still require location for scan results
        if (location != PermissionStatus.Granted)
        {
            location = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        return scan == PermissionStatus.Granted &&
               connect == PermissionStatus.Granted;
    }

    async Task WriteJsonChunked(ICharacteristic characteristic, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);

        const int MTU = 20; // safe value on iOS w/o MTU negotiation
        int offset = 0;

        while (offset < bytes.Length)
        {
            int len = Math.Min(MTU, bytes.Length - offset);
            var chunk = bytes.Skip(offset).Take(len).ToArray();

            Debug.WriteLine($"Writing chunk: {Encoding.UTF8.GetString(chunk)}");
            await characteristic.WriteAsync(chunk);

            offset += len;
            await Task.Delay(300); // small delay helps on iOS
        }
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        Devices.Clear();
        IsScanning = true;

        try
        {
            if (!await EnsureBlePermissionsAsync())
                return;

            _adapter.DeviceDiscovered += (s, a) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!Devices.Any(d => d.Id == a.Device.Id))
                        Devices.Add(a.Device);
                });
            };

            _adapter.ScanTimeout = 50000;
            await _adapter.StartScanningForDevicesAsync([BleConstants.PairService]);
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task PairAsync()
    {
        try
        {
            if (SelectedDevice == null) return;

            var payload = new PairingPayload(wifiSsid, wifiPassword, mqttBroker, mqttPort, mqttTopic);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(180));
            await _adapter.ConnectToDeviceAsync(SelectedDevice, cancellationToken: cts.Token);

            var service = await SelectedDevice.GetServiceAsync(BleConstants.PairService);
            if (service == null)
                return;

            var rx = await service.GetCharacteristicAsync(BleConstants.RxCharacteristic);
            if (rx == null)
                return;

            var json = JsonSerializer.Serialize(payload);
            
            await WriteJsonChunked(rx, json);
            
            await Shell.Current.DisplayAlertAsync("Success", "Credentials sent.", "OK");
            await Task.Delay(10000);
            return;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Failed to send credentials.", "OK");
            Debug.WriteLine(ex);
        }
        finally
        {
            await _adapter.DisconnectDeviceAsync(SelectedDevice!);
        }
    }
}
