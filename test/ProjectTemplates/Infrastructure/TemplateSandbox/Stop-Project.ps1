[CmdletBinding(PositionalBinding=$false)]
Param(
  [string]$ProcessId
)

Stop-Process -Id $ProcessId
Write-Output "Process stopped."
