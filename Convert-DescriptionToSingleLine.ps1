# 脚本用途：将XML文件中的多行<description>标签转换为单行
# 使用方法：.\Convert-DescriptionToSingleLine.ps1

param(
  [string]$FilePath = 'd:\steam\steamapps\common\RimWorld\Mods\Rakkle-RV2\Defs\Backstory\BackstoryDef.xml'
)

# 检查文件是否存在
if (-not (Test-Path $FilePath)) {
  Write-Host "错误: 文件不存在: $FilePath" -ForegroundColor Red
  exit 1
}

Write-Host "处理文件: $FilePath" -ForegroundColor Cyan

# 读取文件内容
$content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::UTF8)

# 使用.NET的正则表达式替换多行description为单行
$regex = [System.Text.RegularExpressions.Regex]::new(
  '<description>(.+?)</description>', 
  [System.Text.RegularExpressions.RegexOptions]::Singleline
)

$newContent = $regex.Replace($content, {
  param($match)
  $text = $match.Groups[1].Value
  # 把所有的空白字符（包括换行）替换为单个空格
  $text = [System.Text.RegularExpressions.Regex]::Replace($text, '\s+', ' ')
  $text = $text.Trim()
  "<description>$text</description>"
})

# 写入修改后的内容
[System.IO.File]::WriteAllText($FilePath, $newContent, [System.Text.Encoding]::UTF8)

Write-Host "✓ 转换完成！所有多行<description>已转换为单行。" -ForegroundColor Green
