Bivrost Logger
==============

Robust threaded logging facility.


Usage
-----

First, register LogListeners. A log listener is an object that receives all logs sent to the logger.

  Logger.Register(logwriter);

Available listeners: 

* windows event system (WindowsEventLogListener)
* text log appender (TextLogListener)
* console log (TraceLogListener)
* a window with logs (LogWindow)

You can easily add your own by implementing the LogListener interface.


Afterwards, there are Logger.Info, Logger.Error and Logger.Fatal static methods.