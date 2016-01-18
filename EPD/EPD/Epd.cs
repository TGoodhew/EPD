using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Devices.Gpio;
using Microsoft.IoT.Lightning.Providers;
using Windows.Devices;

namespace EPD
{
    public sealed class Epd
    {
        private string spiBusName;
        private int spiChipSelect;
        private SpiDevice spiDevice = null;
        private GpioController gpioController = null;

        // Additional GPIO pins required by the EPD
        private GpioPin pinReset = null;
        private GpioPin pinPanel = null;
        private GpioPin pinDischarge = null;
        private GpioPin pinBorder = null;
        private GpioPin pinBusy = null;

        // GPIO Pin numbers - These should be changed to reflect how the display is actually connected
        private readonly int RESET_PIN = 13;
        private readonly int PANEL_PIN = 19;
        private readonly int DISCHARGE_PIN = 26;
        private readonly int BORDER_PIN = 12;
        private readonly int BUSY_PIN = 5;
        private readonly int SPI_CS_PIN = 8;

        // SPI Bus pins - These are used in the power off to take the pins low prior to discharge
        private readonly int SPI_CLK = 11;
        private readonly int SPI_MOSI = 10;

        public Epd(string spiBusName, int spiChipSelect)
        {
            this.spiBusName = spiBusName;
            this.spiChipSelect = spiChipSelect;
        }

        public IAsyncOperation<int> BeginAsync()
        {
            return this.BeginAsyncHelper().AsAsyncOperation<int>();
        }

        private async Task<int> BeginAsyncHelper()
        {
            try
            {
                InitLightningProvider();
                await InitGPIO();
                await PowerOnCOGDriver();
                await InitSPI();
            }
            catch (Exception ex)
            {
                throw new Exception("Begin setup failed", ex);
            }

            //TODO: This is in the sample code - Not sure why - Write 2 zeros to the SPI bus
            spiDevice.Write(new byte[] { 0x00, 0x00 });

            await InitCOGDriver();

            await PowerOffCOGDriver();

            return 0;
        }

        private async Task PowerOffCOGDriver()
        {
            pinReset.Write(GpioPinValue.Low);
            pinPanel.Write(GpioPinValue.Low);
            pinBorder.Write(GpioPinValue.Low);

            spiDevice.Dispose();

            CreateWritePin(SPI_CLK);
            CreateWritePin(SPI_MOSI);
            CreateWritePin(SPI_CS_PIN);
            await Task.Delay(150);

            pinDischarge.Write(GpioPinValue.High);
            await Task.Delay(150);

            pinDischarge.Write(GpioPinValue.Low);
            await Task.Delay(10);

            pinDischarge.Write(GpioPinValue.High);
            await Task.Delay(150);

            pinDischarge.Write(GpioPinValue.Low);
            await Task.Delay(10);
        }

        private async Task InitCOGDriver()
        {
            byte[] cogData = new byte[2];

            // Wait if the driver is busy
            while (GpioPinValue.High == pinBusy.Read())
                await Task.Delay(1);

            // Check the COG Driver ID
            spiDevice.TransferFullDuplex(new byte[] { 0x71, 0x00 }, cogData);
        }

        private async Task PowerOnCOGDriver()
        {
            // Grab the CS pin, send it low and wait 5ms
            var pinCS = gpioController.OpenPin(SPI_CS_PIN, GpioSharingMode.Exclusive);
            pinCS.SetDriveMode(GpioPinDriveMode.Output);
            pinCS.Write(GpioPinValue.Low);
            await Task.Delay(5);

            // Turn the panel on and wait 10ms
            pinPanel.Write(GpioPinValue.High);
            await Task.Delay(10);

            // Set Reset, Border & CS high then wait 5ms
            pinReset.Write(GpioPinValue.High);
            pinBorder.Write(GpioPinValue.High);
            pinCS.Write(GpioPinValue.High);
            await Task.Delay(5);

            // Set Reset low and wait 5ms
            pinReset.Write(GpioPinValue.Low);
            await Task.Delay(5);

            // Set Reset high and wait 5ms
            pinReset.Write(GpioPinValue.High);
            await Task.Delay(5);

            // Release the CS Pin
            pinCS.Dispose();
            pinCS = null;
        }

        private async Task InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(spiChipSelect);
                settings.ClockFrequency = 4000000;
                settings.Mode = SpiMode.Mode0;

                SpiController controller = await SpiController.GetDefaultAsync();
                spiDevice = controller.GetDevice(settings);
            }
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed.", ex);
            }
        }

        private async Task InitGPIO()
        {
            // Get the GPIO controller
            try
            {
                gpioController = await GpioController.GetDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("GPIO Initialization failed.", ex);
            }

            // Setup the GPIO pins for the display
            pinReset = CreateWritePin(RESET_PIN);
            pinPanel = CreateWritePin(PANEL_PIN);
            pinDischarge = CreateWritePin(DISCHARGE_PIN);
            pinBorder = CreateWritePin(BORDER_PIN);

            // Setup the Busy pin as a read
            pinBusy = gpioController.OpenPin(BUSY_PIN, GpioSharingMode.Exclusive);
            pinBusy.SetDriveMode(GpioPinDriveMode.Input);
        }

        private GpioPin CreateWritePin(int pinNumber)
        {
            var pin = gpioController.OpenPin(pinNumber, GpioSharingMode.Exclusive);
            pin.SetDriveMode(GpioPinDriveMode.Output);
            pin.Write(GpioPinValue.Low);

            return pin;
        }

        private void InitLightningProvider()
        {
            //Set the Lightning Provider as the default if Lightning driver is enabled on the target device
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }
        }
    }
}
