using System;
using System.Collections.Generic;
using System.Text;

namespace CodeChavez.ConfigurablePicoW;

public record PairingPayload(
    string wifi_ssid,
    string wifi_password,
    string mqtt_broker,
    int mqtt_port,
    string mqtt_topic
);