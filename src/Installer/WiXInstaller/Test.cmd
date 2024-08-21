dir bin\Setup\
rem pause

set release=Chem4Word-Setup.3.2.20.Release.16.msi

del setup.log
del remove.log

msiexec /i bin\Setup\%release% /l*v setup.log

pause

msiexec /uninstall bin\Setup\%release% /l*v remove.log

pause

rem find "Property(" setup.log > properties.log
rem search logs for "Calling custom action WiX.CustomAction"
