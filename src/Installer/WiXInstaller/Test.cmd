dir bin\x86\Setup\en-US\
pause

set release=Chem4Word-Setup.3.2.24.Release.20.msi

del setup.log
del remove.log

msiexec /i bin\x86\Setup\en-US\%release% /l*v setup.log

pause

rem msiexec /uninstall bin\x86\Setup\%release% /l*v remove.log

pause

rem find "Property(" setup.log > properties.log
rem search logs for "Calling custom action WiX.CustomAction"
