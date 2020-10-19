del setup.log
del remove.log

msiexec /i Chem4Word-Setup.3.1.16.Release.6.msi /l*v setup.log

rem pause

msiexec /uninstall Chem4Word-Setup.3.1.16.Release.6.msi /l*v remove.log

pause

rem find "Property(" setup.log > properties.log
