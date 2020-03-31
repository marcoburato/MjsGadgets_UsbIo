A simple command-line utility to control MJS Gadgets USB-I/O-1 units.

The manufacturer seems no longer in business and I could only find a test app in the attached CD.

This project contains a command line utility and a .NET library to control it.

    Usage:
        UsbIoCtrl [--serial-number <serialnumber>] list-devices
        UsbIoCtrl [--serial-number <serialnumber>] get-all-inputs
        UsbIoCtrl [--serial-number <serialnumber>] get-all-relays
        UsbIoCtrl [--serial-number <serialnumber>] get-input <num>
        UsbIoCtrl [--serial-number <serialnumber>] get-relay <num>
        UsbIoCtrl [--serial-number <serialnumber>] set-all-relays <states>
        UsbIoCtrl [--serial-number <serialnumber>] set-relay <num> <state>
    where:
        <serialnumber> is the target device serial number. If not provided, the first device found will be used.
        <num> is the input of relay number 1..4
        <state> is 0 (OFF) or 1 (ON)
        <states> sequence of <state>, one for each relay (4)

For instance, to turn relay 2 ON:

    > UsbIoCtrl set-relay 2 1
