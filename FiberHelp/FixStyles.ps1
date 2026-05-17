$files = Get-ChildItem -Path "c:\Users\Genita\source\repos\FiberHelp\FiberHelp\Components\Pages" -Filter "*.razor" -Recurse
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $original = $content
    
    $content = $content -replace 'background:\s*#(?:fff|ffffff|FFF|FFFFFF)(?![\w\d])', 'background:var(--card-bg)'
    $content = $content -replace 'background:\s*white(?![\w\d])', 'background:var(--card-bg)'
    $content = $content -replace 'background-color:\s*#(?:fff|ffffff|FFF|FFFFFF)(?![\w\d])', 'background-color:var(--card-bg)'
    $content = $content -replace 'background-color:\s*white(?![\w\d])', 'background-color:var(--card-bg)'
    $content = $content -replace 'border:\s*1px solid #(?:e5e7eb|e2e8f0|e6edf3|eef2f7)', 'border:1px solid var(--border-color)'
    $content = $content -replace 'border-bottom:\s*1px solid #(?:e5e7eb|e2e8f0|e6edf3|eef2f7)', 'border-bottom:1px solid var(--border-color)'
    
    # Text colors
    $content = $content -replace 'color:\s*#475569', 'color:var(--text-muted)'
    $content = $content -replace 'color:\s*#64748b', 'color:var(--text-muted)'
    $content = $content -replace 'color:\s*#0f172a', 'color:var(--text-main)'
    $content = $content -replace 'color:\s*#1e293b', 'color:var(--text-main)'
    $content = $content -replace 'color:\s*#374151', 'color:var(--text-main)'

    # White font colors to text-main
    $content = $content -replace 'color:\s*#(?:fff|ffffff|FFF|FFFFFF)(?![\w\d])', 'color:var(--text-main)'
    $content = $content -replace 'color:\s*white(?![\w\d])', 'color:var(--text-main)'

    if ($content -cne $original) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Updated: $($file.Name)"
    }
}
