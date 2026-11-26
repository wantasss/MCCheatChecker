Get-ChildItem -Path 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker' -Filter '*ViewerForm.cs' | Select-Object -ExpandProperty Name | Sort-Object | Out-File -FilePath 'C:\Users\Wanta\Desktop\MCCheatChecker\viewer_forms.txt' -Encoding utf8
Get-Content 'C:\Users\Wanta\Desktop\MCCheatChecker\viewer_forms.txt' | Select-String -Pattern 'Process|Hacker|Memory|System|Informer|String'
