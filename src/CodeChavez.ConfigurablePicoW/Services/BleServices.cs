using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CodeChavez.ConfigurablePicoW;

public class BleServices
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;

    public BleServices()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;

        _ble.StateChanged += (s, e) => Debug.WriteLine($"Bluetooth state changed to: {e.NewState}");
    }

}
