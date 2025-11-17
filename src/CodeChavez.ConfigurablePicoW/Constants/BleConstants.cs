namespace CodeChavez.ConfigurablePicoW;

public static class BleConstants
{
    public static readonly Guid PairService = Guid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
    public static readonly Guid RxCharacteristic = Guid.Parse("6E400002-B5A3-F393-E0A9-E50E24DCCA9E");
    public static readonly Guid TxCharacteristic = Guid.Parse("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");
}
