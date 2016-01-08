# EPD
RePaper on Raspberry Pi 2 with Windows 10 IoT Core

I took the demo sketch (Sketches/demo/demo.ino) from the RePaper github (https://github.com/repaper/gratis) and put it in an Arduino Wiring for Windows 10 IoT Core project. To get this to work I grabbed the required files from the library (Sketches/libraries) and manually added them to the solution.

There was some fixup required to get the files to compile under VC++ 2015 (related to the differences between GCC and VC++). Some of these fixes were effectively hard coded (like commenting out Serial calls, setting the temp specifically at 25 degrees, changing SPI calls to use initializer lists instead of macros, etc). In most cases I left a \\\TODO comment near them to indicate the chnage and the fact that they need to be wrapped properly to enable it to work in either GCC or VC++).

The other big change I made was to stop the code throwing away the SPI object. You can see the difference in performance in this video:

[![IMAGE ALT TEXT](http://img.youtube.com/vi/XWkJ7O5SSpg/0.jpg)](https://www.youtube.com/watch?v=XWkJ7O5SSpg "SPI bus begin and end costs when moving Arduino Wiring code to Windows 10 IoT Core")
