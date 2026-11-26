Get-CimInstance -ClassName Win32_Service -Filter "Name = 'DPS' or Name = 'PcaSvc' or Name = 'DcomLaunch' or Name = 'PlugPlay'" | Select-Object Name, ProcessId, State, DisplayName | Format-Table
Get-Process -Name 'javaw', 'explorer', 'csrss' -ErrorAction SilentlyContinue | Select-Object Name, Id, WorkingSet64 | Format-Table
