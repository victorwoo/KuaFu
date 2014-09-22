$DebugPreference = 'SilentlyContinue'  # 不显示调试信息
#$DebugPreference = 'Continue'         # 要显示调试信息
$GenerateSentenceOnly = $false         # 仅仅生成查询语句，并不进行在线查询。
$DaysCount = 365                       # 循环测试天数。
$OpenResult = $false                   # 自动打开结果文件
$TotalHandlingCharge = 0.003           # 手续费率总和
$querySentenceTemplate = @'
T-2涨跌幅<3%

T-1涨跌幅<-1%

T+0高开
T+0涨跌幅>5%
T+0涨跌幅<9%
T+0换手率>7%

T+1涨跌幅
'@                                     # 查询语句，可在 http://www.iwencai.com 事先测试。

function Update-IwencaiItem {
    param (
        [Parameter(ValueFromPipeline=$true)]
        $IwencaiItem,
        $Replacements
    )

    process {
        if (!$Replacements) {
            return $IwencaiItem
        }

        $IwencaiItem | Get-Member -MemberType NoteProperty | ForEach-Object {
            [string]$name = $_.Name
            $newName = $name
            $Replacements.Keys | ForEach-Object {
                $newName = $newName.Replace($Replacements[$_], $_)
            }
            $newName = $newName -creplace '(?<NAME>.*)\((?<UNIT>%\))<br>(?<DATE>.*)', '${DATE}${NAME}'

            if ($newName -ne $name) {
                $IwencaiItem | Add-Member -MemberType NoteProperty -Name $newName -Value $IwencaiItem.$name
            }
        }
        return $IwencaiItem
    }
}

function get-standarddeviation {            
    [CmdletBinding()]            
    param (            
      [double[]]$numbers            
    )            
            
    $avg = $numbers | Measure-Object -Average | select Count, Average            
            
    $popdev = 0            
            
    foreach ($number in $numbers){            
      $popdev +=  [math]::pow(($number - $avg.Average), 2)            
    }            
            
    $sd = [math]::sqrt($popdev / ($avg.Count-1))            
    $sd            
}

<# 根据自然语言的问句，从 http://www.iwencai.com/ 获取数据列表 #>
function Get-ListFromIwencai {
    Param(
        [string] $QueryWord
    )
    
    Write-Debug $queryWord

    $url = 'http://www.iwencai.com/stockpick/search?preParams=&ts=1&f=1&qs=1&selfsectsn=&querytype=&searchfilter=&tid=stockpick&w={0}' -f $QueryWord
    $url = [System.Uri]::EscapeUriString($url)

    $success = $false
    while (-not $success) {
        try {
            #Write-Debug $url
            $content = Invoke-RestMethod $url -TimeoutSec 15
        } catch {}

        if ($content -cmatch '(?mn)^var allResult = (?<ALL_RESULT>.*?);$') {
            $allResult = $Matches['ALL_RESULT']
        }

        $allResult = [regex]::Unescape($allResult) -creplace '(?m)<a href=".*?">(.*?)</a>', '${1}' | ConvertFrom-Json
        $total = $allResult.total
        Write-Debug "total = $total"
        $token = $allResult.token
        Write-Debug "token = $token"

        if ($total -ne 0) {
#            $success = $true
        }

        $success = $true
    }

    # 此处可以从页面数据中，分析出本页的数据（每页缺省为 30 条）
    # 由于下一步将模拟页面中的 ajax 请求，一次性获取所有页的数据，所以无需一页一页分析。
    # 请求第一页只是为了获得 token 数据，供 ajax 请求用。
    # 一个 token 对应了搜索条件等信息，在服务器会有缓存。
    # 所以进行 ajax 请求的时候无需传递搜索条件，只需要传递 token 即可。
    
    <#
    $perpage = $allResult.perpage
    Write-Debug "perpage = $perpage"

    $pageCount = [math]::Ceiling($total / $perpage)
    $fieldTypes = $allResult.fieldType
    $allResult.result | ForEach-Object {
        $result = New-Object -TypeName PSObject
        $parts = $_ -csplit "`t"
        for ($i = 0; $i -lt $Properties.Length; $i++) {
            $value = $parts[$i]
            switch ($fieldTypes[$i]) {
                'STR' {
                    $value = $value
                };
                'DOUBLE' {
                    $value = [double]$value
                }
            }
            $result | Add-Member -MemberType NoteProperty -Name $Properties[$i] -Value $value
        }

        $result
    }
    #>

    $url = 'http://www.iwencai.com/stockpick/cache?token={0}&p={1}&perpage={2}' -f $token, 1, $total
    $success = $false
    while (-not $success) {
        try {
            Write-Debug $url
            $restResult = Invoke-RestMethod $url -TimeoutSec 10
            #$restResult = ([regex]::Unescape($restResult) | ConvertTo-Json) -creplace '(?m)<a href=".*?">(.*?)</a>', '${1}' | ConverFrom-Json
            $success = $true
        } catch {}
    }
    #Write-Debug ($restResult | ConvertTo-Json)

    $fieldTypes = $allResult.fieldType
    $restResult.list | ForEach-Object {
        $listItem = $_
        $result = New-Object -TypeName PSObject
        for ($i = 0; $i -lt $listItem.Length; $i++) {
            $value = $listItem[$i]
            $value = [regex]::Unescape($value) -creplace '(?m)<a href=".*?">(.*?)</a>', '${1}'
            switch ($fieldTypes[$i]) {
                'STR' {
                    # $value = $value3
                };
                'DOUBLE' {
                    $x = $null
                    $null = [double]::TryParse($value, [ref]$x)
                    $value = $x
                }
            }
            $result | Add-Member -MemberType NoteProperty -Name $allResult.title[$i] -Value $value
        }

        $result
    }
}

function IsWorkDay {
    Param(
        [datetime]$t0
    )

    $dayOfWeek = $t0.DayOfWeek.value__
    return $dayOfWeek -ge 1 -and $dayOfWeek -le 5
}

<# 获取下 N 个或前 N 个交易日。 #>
function Get-OpenDate {
    Param (
        [datetime]$t0,
        [int]$Offset
    )

    if ($Offset -gt 0) {
        $step = 1
    } elseif ($Offset -eq 0) {
        $step = 0
    } else {
        $step = -1
    }

    for ($i = 0; $i -lt [math]::Abs($Offset); $i++) {
        do {
            $t0 = $t0.AddDays($step)
        } until (IsWorkDay $t0)
    }

    return $t0
}

$warnings = @()
$changes = @()
$todayAverages = @()

$tradeDates = 0..($DaysCount - 1) | ForEach-Object {
    $t0 = (Get-Date).AddDays(-$_)
    return $t0
}

$tradeDates = $tradeDates | Sort-Object
$changeRate = 1
for ($i = 0; $i -lt $tradeDates.Length; $i++) {
    $t0 = $tradeDates[$i]
    Write-Progress -Activity 模拟运算 -PercentComplete ($i / $tradeDates.Length * 100) -CurrentOperation "$($t0.ToShortDateString()) [$i/$($tradeDates.Count)]"
    #$t0 = [datetime]"9月16日"
    if (!(IsWorkDay $t0)) {
        continue
    }
    $p1 = Get-OpenDate $t0 -1
    $p2 = Get-OpenDate $t0 -2
    $p3 = Get-OpenDate $t0 -3
    $n1 = Get-OpenDate $t0 1
    $n2 = Get-OpenDate $t0 2
    $n3 = Get-OpenDate $t0 3

    if ($n1.Date -ge (Get-Date).Date) {
        continue
    }

    echo ('T = {0:M月d日}' -f $t0)
   
    $querySentence = $querySentenceTemplate
    $querySentence = $querySentence.Split("`r`n")
    $querySentence = $querySentence | Where-Object { ![string]::IsNullOrWhiteSpace($_) -and !$_.StartsWith('#') -and !$_.StartsWith('//') }
    $querySentence = $querySentence -join "，"

    $querySentence = $querySentence -replace 'T\-3', '{0:M月d日}'
    $querySentence = $querySentence -replace 'T\-2', '{1:M月d日}'
    $querySentence = $querySentence -replace 'T\-1', '{2:M月d日}'
    $querySentence = $querySentence -replace 'T\+0', '{3:M月d日}'
    $querySentence = $querySentence -replace 'T\+1', '{4:M月d日}'
    $querySentence = $querySentence -replace 'T\+2', '{5:M月d日}'
    $querySentence = $querySentence -replace 'T\+3', '{6:M月d日}'
    $querySentence = $querySentence -f $p3, $p2, $p1, $t0, $n1, $n2, $n3

    if ($GenerateSentenceOnly) {
        echo $querySentence
        continue
    }

    $uriTemplate = 'http://www.iwencai.com/stockpick/search?preParams=&ts=1&f=1&qs=1&selfsectsn=&querytype=&searchfilter=&tid=stockpick&w={0}'
    $uri = $uriTemplate -f $querySentence
    $list = Get-ListFromIwencai $querySentence
    $list = $list | Update-IwencaiItem -Replacements @{
        'T-2' = '{0:yyyy.MM.dd}' -f $p2;
        'T-1' = '{0:yyyy.MM.dd}' -f $p1;
        'T+0' = '{0:yyyy.MM.dd}' -f $t0;
        'T+1' = '{0:yyyy.MM.dd}' -f $n1;
        'Name' = '股票代码';
        'Symbol' = '股票简称';
    } | Sort-Object 'T+1涨跌幅' -Descending

    if ($list -and $list.Count -gt 0 -and $list[0].'T+1涨跌幅') {
        Write-Debug ($list | Select-Object Name, @{N='T+1'; E={'{0:P}' -f ($_.'T+1涨跌幅' / 100)}}, Symbol | Format-Table -AutoSize | Out-String)

        $list | 
            Select-Object -ExpandProperty 'T+1涨跌幅' |
            ForEach-Object {
                $changes += $_
            }
        
        $todayAverage = ($list |
            Select-Object -ExpandProperty 'T+1涨跌幅' |
            Measure-Object -Average).Average
        $todayAverages += [pscustomobject][ordered]@{
            Date = '{0:M月d日}' -f $t0;
            AverageN1ChangeInPercent = $todayAverage
        }
        echo ('本日平均涨跌幅：{0:P}' -f ($todayAverage / 100))
        $changeRate *= (1 + $todayAverage / 100 - $TotalHandlingCharge)
        "算数平均值：{0:P}，标准差：{1:N}，总资产：{2:P}" -f
            (($changes | Measure-Object -Average).Average / 100), (get-standarddeviation $changes), $changeRate |
            Tee-Object -Variable log
    }
}

echo ''
echo 模拟结束
if ($GenerateSentenceOnly) { exit }

echo $log
echo "$($querySentenceTemplate.Replace("`r`n`r`n", "`r`n").Replace("`r`n", "，"))" >> Get-Stock.log
echo $log >> Get-Stock.log

"自然日数：{0:D}，有效日数：{1:D}，统计股数：{2:D}，年增长率：{3:P}" -f 
    $tradeDates.Length, 
    $todayAverages.Length, 
    $changes.Length, 
    (($changeRate - 1) * 365 / $tradeDates.Length) | 
        Tee-Object -Variable log
echo $log >> Get-Stock.log
echo '' >> Get-Stock.log

$csvName = "N1ChangeInPercent_{0:hhmmss}.csv" -f (Get-Date)
$changes | ForEach-Object {
    echo $_ >> $csvName
}
if ($OpenResult) { Start-Process $csvName }

$csvName = "AverageN1ChangeInPercent_{0:hhmmss}.csv" -f (Get-Date)
$todayAverages | Export-Csv -Path $csvName -Encoding UTF8 -NoTypeInformation
if ($OpenResult) { Start-Process $csvName }