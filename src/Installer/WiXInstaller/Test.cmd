dir bin\Setup\
rem pause

del setup.log
del remove.log

msiexec /i bin\Setup\Chem4Word-Setup.3.1.16.Release.6.msi /l*v setup.log

rem pause

msiexec /uninstall bin\Setup\Chem4Word-Setup.3.1.16.Release.6.msi /l*v remove.log

rem pause

rem find "Property(" setup.log > properties.log
rem search logs for "Calling custom action WiX.CustomAction"
