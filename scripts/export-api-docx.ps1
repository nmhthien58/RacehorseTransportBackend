param(
    [string]$InputPath = (Join-Path $PSScriptRoot '..\docs\API.md'),
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\docs\API_Huong_dan_Frontend.docx')
)

$ErrorActionPreference = 'Stop'
$inputFile = [IO.Path]::GetFullPath($InputPath)
$outputFile = [IO.Path]::GetFullPath($OutputPath)
$tempHtml = Join-Path ([IO.Path]::GetTempPath()) ("racehorse-api-{0}.html" -f [guid]::NewGuid().ToString('N'))

function Convert-InlineMarkdown([string]$text) {
    $value = [Net.WebUtility]::HtmlEncode($text)
    $value = [regex]::Replace($value, '\[([^\]]+)\]\(([^\)]+)\)', '<a href="$2">$1</a>')
    $value = [regex]::Replace($value, '\*\*([^*]+)\*\*', '<strong>$1</strong>')
    $value = [regex]::Replace($value, '`([^`]+)`', '<code>$1</code>')
    return $value
}

$lines = Get-Content -LiteralPath $inputFile -Encoding UTF8
$html = New-Object Text.StringBuilder
[void]$html.AppendLine('<!doctype html><html><head><meta charset="utf-8"><style>')
[void]$html.AppendLine('body{font-family:Arial,sans-serif;font-size:10.5pt;line-height:1.35;color:#202124} h1{font-size:24pt;color:#17365d} h2{font-size:18pt;color:#1f4e78;border-bottom:1px solid #9dc3e6;padding-bottom:4px} h3{font-size:14pt;color:#2f5597} h4{font-size:12pt;color:#2f5597} table{border-collapse:collapse;width:100%;margin:8px 0 14px} th,td{border:1px solid #9e9e9e;padding:5px;vertical-align:top} th{background:#d9eaf7;font-weight:bold} code{font-family:Consolas,monospace;background:#f2f2f2;padding:1px 3px} pre{font-family:Consolas,monospace;font-size:9pt;background:#f6f8fa;border:1px solid #ddd;padding:8px;white-space:pre-wrap} p{margin:5px 0} li{margin:2px 0}</style></head><body>')

$inCode = $false
$inTable = $false
$inList = $false
$listTag = ''
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match '^```') {
        if ($inCode) { [void]$html.AppendLine('</pre>'); $inCode = $false } else { [void]$html.AppendLine('<pre>'); $inCode = $true }
        continue
    }
    if ($inCode) { [void]$html.AppendLine([Net.WebUtility]::HtmlEncode($line)); continue }

    $isTable = $line.Trim().StartsWith('|') -and $line.Trim().EndsWith('|')
    if ($isTable) {
        if ($line -match '^\s*\|(?:\s*:?-+:?\s*\|)+\s*$') { continue }
        if (-not $inTable) { [void]$html.AppendLine('<table>'); $inTable = $true }
        $nextIsSeparator = $i + 1 -lt $lines.Count -and $lines[$i + 1] -match '^\s*\|(?:\s*:?-+:?\s*\|)+\s*$'
        $cellTag = if ($nextIsSeparator) { 'th' } else { 'td' }
        [void]$html.Append('<tr>')
        foreach ($cell in $line.Trim().Trim('|').Split('|')) {
            [void]$html.Append("<$cellTag>$(Convert-InlineMarkdown $cell.Trim())</$cellTag>")
        }
        [void]$html.AppendLine('</tr>')
        continue
    }
    if ($inTable) { [void]$html.AppendLine('</table>'); $inTable = $false }

    $newListTag = if ($line -match '^\s*[-*]\s+') { 'ul' } elseif ($line -match '^\s*\d+\.\s+') { 'ol' } else { '' }
    if ($newListTag) {
        if (-not $inList -or $listTag -ne $newListTag) {
            if ($inList) { [void]$html.AppendLine("</$listTag>") }
            $listTag = $newListTag; $inList = $true; [void]$html.AppendLine("<$listTag>")
        }
        $item = $line -replace '^\s*(?:[-*]|\d+\.)\s+', ''
        [void]$html.AppendLine("<li>$(Convert-InlineMarkdown $item)</li>")
        continue
    }
    if ($inList) { [void]$html.AppendLine("</$listTag>"); $inList = $false; $listTag = '' }

    if ($line -match '^(#{1,4})\s+(.+)$') {
        $level = $Matches[1].Length
        [void]$html.AppendLine("<h$level>$(Convert-InlineMarkdown $Matches[2])</h$level>")
    } elseif ($line -match '^---+$') {
        [void]$html.AppendLine('<hr>')
    } elseif ([string]::IsNullOrWhiteSpace($line)) {
        [void]$html.AppendLine('<p>&nbsp;</p>')
    } else {
        [void]$html.AppendLine("<p>$(Convert-InlineMarkdown $line)</p>")
    }
}
if ($inCode) { [void]$html.AppendLine('</pre>') }
if ($inTable) { [void]$html.AppendLine('</table>') }
if ($inList) { [void]$html.AppendLine("</$listTag>") }
[void]$html.AppendLine('</body></html>')

[IO.File]::WriteAllText($tempHtml, $html.ToString(), [Text.UTF8Encoding]::new($true))
$word = $null
$document = $null
try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $document = $word.Documents.Open($tempHtml)
    $document.SaveAs2($outputFile, 16)
}
finally {
    if ($document) { try { $document.Close($false) } catch { } }
    if ($word) { try { $word.Quit() } catch { } }
    if (Test-Path -LiteralPath $tempHtml) { Remove-Item -LiteralPath $tempHtml -Force }
    if ($document) { [Runtime.InteropServices.Marshal]::ReleaseComObject($document) | Out-Null }
    if ($word) { [Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null }
}

Write-Output $outputFile
