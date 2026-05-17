$files = Get-ChildItem -Path "c:\Users\Genita\source\repos\FiberHelp\FiberHelp\Components\Pages" -Filter "*.razor" -Recurse
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

foreach ($file in $files) {
    # Read raw bytes to avoid powershell BOM issues
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    
    # Check for UTF-8 BOM
    $start = 0
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $start = 3
    }
    
    # Try parsing as UTF-8 first
    $content = [System.Text.Encoding]::UTF8.GetString($bytes, $start, $bytes.Length - $start)
    
    # If it contains null characters, it might be UTF-16 LE
    if ($content.Contains([char]0)) {
        $start = 0
        if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
            $start = 2
        }
        $content = [System.Text.Encoding]::Unicode.GetString($bytes, $start, $bytes.Length - $start)
    }

    $original = $content
    
    # Replace any remaining light theme backgrounds
    $content = $content -replace 'background:\s*#(?:fff|ffffff|FFF|FFFFFF|f8fafc|F8FAFC|f1f5f9|F1F5F9)(?![\w\d])', 'background:var(--card-bg)'
    $content = $content -replace 'background:\s*white(?![\w\d])', 'background:var(--card-bg)'
    $content = $content -replace 'background-color:\s*#(?:fff|ffffff|FFF|FFFFFF|f8fafc|F8FAFC|f1f5f9|F1F5F9)(?![\w\d])', 'background-color:var(--card-bg)'
    $content = $content -replace 'background-color:\s*white(?![\w\d])', 'background-color:var(--card-bg)'
    
    # Replace light theme borders
    $content = $content -replace 'border:\s*1px solid #(?:e5e7eb|e2e8f0|e6edf3|eef2f7)', 'border:1px solid var(--border-color)'
    $content = $content -replace 'border-bottom:\s*1px solid #(?:e5e7eb|e2e8f0|e6edf3|eef2f7)', 'border-bottom:1px solid var(--border-color)'
    
    # Replace light theme text colors
    $content = $content -replace 'color:\s*#(?:475569|64748b)', 'color:var(--text-muted)'
    $content = $content -replace 'color:\s*#(?:0f172a|1e293b|374151)', 'color:var(--text-main)'

    # Replace any white text (which is now invisible against white cards) with text-main
    $content = $content -replace 'color:\s*#(?:fff|ffffff|FFF|FFFFFF)(?![\w\d])', 'color:var(--text-main)'
    $content = $content -replace 'color:\s*white(?![\w\d])', 'color:var(--text-main)'

    if ($content -cne $original) {
        [System.IO.File]::WriteAllText($file.FullName, $content, $utf8NoBom)
        Write-Host "Fixed UI and Encoding for: $($file.Name)"
    }
}
Write-Host "All UI inconsistencies fixed successfully!"
