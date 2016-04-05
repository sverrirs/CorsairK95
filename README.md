# Corsair K95 Custom Driver and .NET code
A C# library and example project showing how to interface with the Corsair K95 Gaming Keyboard and both read it's custom input buttons (gamer keys and media controls) as well as issue commands to control it's LED light states and configuration of gamer keys.

![Vengeance K95 Fully Mechanical Gaming Keyboard](http://cwsmgmt.corsair.com/media/catalog/product/k/9/k95_11_angle.png "The K95 Keyboard")

[The Corsair K95 Product Website](http://www.corsair.com/en-us/vengeance-k95-fully-mechanical-gaming-keyboard/)

# Driver installer
A downloadable binary for the driver install (both 32 and 64 bit versions) can be found under *WinUsbDriver/binaries* folder. 
Note: These installers have not been tested with Win8 and newer so they might not work (due to the fact that Windows 8+ requires .inf files to be signed as well)

# Dependencies
* Uses the [WinUsb.NET](https://github.com/madwizard-thomas/winusbnet/) project 
* The Corsair driver must be uninstalled and completely removed from the users system before attempting to install the custom driver (if using the installer). Manually updating the driver through Device Manager will allow for both drivers to be present on the user's system.
* Requires the custom driver in the *WinUsbDriver* folder to be installed prior to running any of the code.

# Examples

Cycles through the LED backlighting brightness

```csharp
K95Device usb = new K95Device();
try
{
    usb.Connect();

    LedBrightness[] ledbrightness = {LedBrightness.Off,
                                     LedBrightness.Low, LedBrightness.Medium, LedBrightness.High,
                                     LedBrightness.Medium, LedBrightness.Low };

    // Now cycle through the brightness intensities for the keyboard
    for ( int i = 0; i < 10000; i++)
    {
        usb.SetLedBrightness(ledbrightness[i % ledbrightness.Length]);

        // Short wait to let the hardware get ready again and the user to notice the change
        Thread.Sleep(150);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
    usb.Disconnect();
}
```
