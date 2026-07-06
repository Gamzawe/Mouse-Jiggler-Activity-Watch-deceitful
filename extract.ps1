Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Icon]::ExtractAssociatedIcon('C:\Windows\System32\audiodg.exe')
$fs = [System.IO.File]::OpenWrite('d:\config\app.ico')
$icon.Save($fs)
$fs.Close()
