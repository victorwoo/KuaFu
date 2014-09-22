#$DebugPreference = 'SilentlyContinue'
$DebugPreference = 'Continue'
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
        [string] $QueryWord,
        [string[]] $Properties
    )
    
    Write-Debug $queryWord

    $url = 'http://www.iwencai.com/stockpick/search?preParams=&ts=1&f=1&qs=1&selfsectsn=&querytype=&searchfilter=&tid=stockpick&w={0}' -f $QueryWord
    $url = [System.Uri]::EscapeUriString($url)

    $success = $false
    while (-not $success) {
        try {
            Write-Debug $url
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

    #Start-Sleep -Seconds 5

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
        $result = New-Object -TypeName PSObject
        for ($i = 0; $i -lt $Properties.Length; $i++) {
            $value = $_[$i]
            $value = [regex]::Unescape($value) -creplace '(?m)<a href=".*?">(.*?)</a>', '${1}'
            switch ($fieldTypes[$i]) {
                'STR' {
                    # $value = $value
                };
                'DOUBLE' {
                    $x = $null
                    $null = [double]::TryParse($value, [ref]$x)
                    $value = $x
                }
            }
            $result | Add-Member -MemberType NoteProperty -Name $Properties[$i] -Value $value
        }

        $result
    }
}

function IsWorkDay {
    Param(
        [datetime]$Date
    )

    $dayOfWeek = $Date.DayOfWeek.value__
    return $dayOfWeek -ge 1 -and $dayOfWeek -le 5
}

<# 获取下 N 个或前 N 个交易日。 #>
function Get-OpenDate {
    Param (
        [datetime]$Date,
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
            $Date = $Date.AddDays($step)
        } until (IsWorkDay $Date)
    }

    return $Date
}

$warnings = @()
$changes = @()
$todayAverages = @()

$tradeDates = 0..99 | ForEach-Object {
    $date = (Get-Date).AddDays(-$_)
    return $date
}

for ($i = 0; $i -lt $tradeDates.Length; $i++) {
    $date = $tradeDates[$i]
    Write-Progress -Activity $date.ToShortDateString() -PercentComplete ($i / $tradeDates.Length * 100) -CurrentOperation $i
    #$date = [datetime]"9月16日"
    if (!(IsWorkDay $date)) {
        continue
    }
    $p1 = Get-OpenDate $date -1
    $p2 = Get-OpenDate $date -2
    $n1 = Get-OpenDate $date 1
    if ($n1.Date -ge (Get-Date).Date) {
        continue
    }

    echo ('T = {0:M月d日}' -f $date)
    <#
    $querySentenceTemplate = 'T-1涨停，T+0高开，T+0涨跌幅<9.5，T+0换手率<5%，T+1涨跌幅'
    $querySentenceTemplate = $querySentenceTemplate -replace 'T\-2', '{0:M月d日}'
    $querySentenceTemplate = $querySentenceTemplate -replace 'T\-1', '{1:M月d日}'
    $querySentenceTemplate = $querySentenceTemplate -replace 'T\+0', '{2:M月d日}'
    $querySentenceTemplate = $querySentenceTemplate -replace 'T\+1', '{3:M月d日}'
    $querySentence = $querySentenceTemplate -f $p2, $p1, $date, $n1
    echo $querySentence
    $uriTemplate = 'http://www.iwencai.com/stockpick/search?preParams=&ts=1&f=1&qs=1&selfsectsn=&querytype=&searchfilter=&tid=stockpick&w={0}'
    $uri = $uriTemplate -f $querySentence

    Start-Process $uri
    continue
    #>
    
    $querySentenceTemplate = 'T-1涨跌幅>3%，T-1涨跌幅<5%，T+0高开，T+0涨跌幅>2%，T+0涨跌幅<9.5%，T+0换手率>3%，T+1涨跌幅'
    $querySentenceTemplate = $querySentenceTemplate -replace 'T\-2', '{0:M月d日}'
    $querySentenceTemplate = $querySentenceTemplate -replace 'T\-1', '{1:M月d日}'
    $querySentenceTemplate = $querySentenceTemplate -replace 'T\+0', '{2:M月d日}'
    $querySentenceTemplate = $querySentenceTemplate -replace 'T\+1', '{3:M月d日}'
    $querySentence = $querySentenceTemplate -f $p2, $p1, $date, $n1

    #$querySentenceTemplate = '{0:M月d日}涨停，涨跌原因类别非新股，{1:M月d日}高开，{1:M月d日}涨跌幅>0%，{1:M月d日}涨跌幅<9.9，{2:M月d日}涨跌幅'
    #$querySentence = $querySentenceTemplate -f $p1, $date, $n1

    #http://www.iwencai.com/stockpick/search?preParams=&ts=1&f=1&qs=1&selfsectsn=&querytype=&searchfilter=&tid=stockpick&w=9%E6%9C%8818%E6%97%A5%E6%B6%A8%E5%81%9C%EF%BC%8C%E6%B6%A8%E8%B7%8C%E5%8E%9F%E5%9B%A0%E7%B1%BB%E5%88%AB%E9%9D%9E%E6%96%B0%E8%82%A1%EF%BC%8C9%E6%9C%8819%E6%97%A5%E9%AB%98%E5%BC%80%EF%BC%8C9%E6%9C%8819%E6%97%A5%E6%B6%A8%E5%B9%85%E5%A4%A7%E4%BA%8E1%EF%BC%8C9%E6%9C%8820%E6%97%A5%E6%B6%A8%E5%B9%85
    $uriTemplate = 'http://www.iwencai.com/stockpick/search?preParams=&ts=1&f=1&qs=1&selfsectsn=&querytype=&searchfilter=&tid=stockpick&w={0}'
    $uri = $uriTemplate -f $querySentence

    # Start-Process $uri
    $properties = 'Name', # 股票代码
        'Symbol', # 股票简称
        #'ChangeInPercentRealtime', # 最新涨跌幅%
        #'LastTradePriceOnly', # 最新价(元)

        #'P1Limit', # p1涨跌停
        'P1ChangeInPercent', # p1涨跌幅(%)
        'T0ChangeInPercent', # t0涨跌幅(%)
        'T0TurnOverRate', # t0换手率(%)
        'N1ChangeInPercent' # n1涨跌幅(%)

        #'P1Change', # p1涨跌(元)
        #'SSTS', # 上市天数
        
        #'LimitType', # 涨跌原因类别
        #'LimitReason' # 涨跌原因

    $list = Get-ListFromIwencai $querySentence $properties |
        Sort-Object N1ChangeInPercent -Descending
    #$list = $list |
    #    Where-Object LimitType -NE '其他'
    if ($list -and $list[0] -and [double]::TryParse($list[0].N1ChangeInPercent, [ref]$null)) {
        Write-Debug ($list | Select-Object Name, @{N='T+1'; E={'{0:P}' -f ($_.N1ChangeInPercent / 100)}}, Symbol | Format-Table -AutoSize | Out-String)
        #$list | ConvertTo-Csv -NoTypeInformation | Out-File -Encoding utf8 -FilePath 'output.csv'
        $list |
            Where-Object { $_.N1ChangeInPercent -lt 0.3 } |
            ForEach-Object {
                $warnings += $_
                #Write-Warning ('{0} {1:P}' -f $_.Name, ($_.N1ChangeInPercent / 100))
            }
        $list | 
            Select-Object -ExpandProperty N1ChangeInPercent |
            ForEach-Object {
                $changes += $_
            }
        
        $todayAverage = ($list |
            Select-Object -ExpandProperty N1ChangeInPercent |
            Measure-Object -Average).Average
        $todayAverages += [pscustomobject][ordered]@{
            Date = '{0:M月d日}' -f $date;
            AverageN1ChangeInPercent = $todayAverage
        }
        echo ("算数平均值：{0:P} 标准差：{1:N}" -f (($changes | Measure-Object -Average).Average / 100), (get-standarddeviation $changes))
    }
}

echo ("自然日数：{0:D}，有效日数：{1:D}，统计股数：{2:D}" -f $tradeDates.Length, $todayAverages.Length, $changes.Length)
#exit

if (Test-Path output.csv) {
    del output.csv
}

$csvName = "N1ChangeInPercent_{0:hhmmss}.csv" -f (Get-Date)
$changes | ForEach-Object {
    echo $_ >> $csvName
}
Start-Process $csvName

$csvName = "AverageN1ChangeInPercent_{0:hhmmss}.csv" -f (Get-Date)
$todayAverages | Export-Csv -Path $csvName -Encoding UTF8 -NoTypeInformation
Start-Process $csvName

#$warnings | Out-GridView