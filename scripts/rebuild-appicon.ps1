param([string]$IconPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Assets\appicon.ico'))

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$source = [IO.File]::ReadAllBytes($IconPath)
$count = [BitConverter]::ToUInt16($source, 4)
$frames = @()

for ($i = 0; $i -lt $count; $i++) {
    $entry = 6 + 16 * $i
    $width = if ($source[$entry] -eq 0) { 256 } else { [int]$source[$entry] }
    $height = if ($source[$entry + 1] -eq 0) { 256 } else { [int]$source[$entry + 1] }
    $length = [BitConverter]::ToInt32($source, $entry + 8)
    $offset = [BitConverter]::ToInt32($source, $entry + 12)
    $data = [byte[]]::new($length)
    [Array]::Copy($source, $offset, $data, 0, $length)

    if ($width -lt 256 -and $data.Length -ge 8 -and $data[1] -eq 80 -and $data[2] -eq 78 -and $data[3] -eq 71) {
        $inputStream = [IO.MemoryStream]::new($data, $false)
        $bitmap = [Drawing.Bitmap]::new($inputStream)
        $outputStream = [IO.MemoryStream]::new()
        $writer = [IO.BinaryWriter]::new($outputStream)
        try {
            $maskStride = [int]([Math]::Ceiling($width / 32.0) * 4)
            $writer.Write([uint32]40)
            $writer.Write([int32]$width)
            $writer.Write([int32]($height * 2))
            $writer.Write([uint16]1)
            $writer.Write([uint16]32)
            $writer.Write([uint32]0)
            $writer.Write([uint32](($width * 4 + $maskStride) * $height))
            $writer.Write([int32]0)
            $writer.Write([int32]0)
            $writer.Write([uint32]0)
            $writer.Write([uint32]0)

            for ($y = $height - 1; $y -ge 0; $y--) {
                for ($x = 0; $x -lt $width; $x++) {
                    $pixel = $bitmap.GetPixel($x, $y)
                    $writer.Write([byte]$pixel.B)
                    $writer.Write([byte]$pixel.G)
                    $writer.Write([byte]$pixel.R)
                    $writer.Write([byte]$pixel.A)
                }
            }

            for ($y = $height - 1; $y -ge 0; $y--) {
                $mask = [byte[]]::new($maskStride)
                for ($x = 0; $x -lt $width; $x++) {
                    if ($bitmap.GetPixel($x, $y).A -eq 0) {
                        $mask[[int][Math]::Floor($x / 8)] = $mask[[int][Math]::Floor($x / 8)] -bor (1 -shl (7 - ($x % 8)))
                    }
                }
                $writer.Write($mask)
            }

            $writer.Flush()
            $data = $outputStream.ToArray()
        }
        finally {
            $writer.Dispose()
            $outputStream.Dispose()
            $bitmap.Dispose()
            $inputStream.Dispose()
        }
    }

    $frames += ,([pscustomobject]@{ Width = $width; Height = $height; Data = $data })
}

$output = [IO.MemoryStream]::new()
$iconWriter = [IO.BinaryWriter]::new($output)
try {
    $iconWriter.Write([uint16]0)
    $iconWriter.Write([uint16]1)
    $iconWriter.Write([uint16]$frames.Count)
    $nextOffset = 6 + 16 * $frames.Count
    foreach ($frame in $frames) {
        $iconWriter.Write([byte]($frame.Width % 256))
        $iconWriter.Write([byte]($frame.Height % 256))
        $iconWriter.Write([byte]0)
        $iconWriter.Write([byte]0)
        $iconWriter.Write([uint16]1)
        $iconWriter.Write([uint16]32)
        $iconWriter.Write([uint32]$frame.Data.Length)
        $iconWriter.Write([uint32]$nextOffset)
        $nextOffset += $frame.Data.Length
    }
    foreach ($frame in $frames) {
        $iconWriter.Write([byte[]]$frame.Data)
    }
    $iconWriter.Flush()
    [IO.File]::WriteAllBytes($IconPath, $output.ToArray())
}
finally {
    $iconWriter.Dispose()
    $output.Dispose()
}
