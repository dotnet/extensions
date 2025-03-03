# Example:
#    .\eng\scripts\Get-RepoDigest -ghToken <GitHub token>
#

[CmdletBinding()]
Param(
    [Parameter(Mandatory = $True)]
    [string] $ghToken
)

$baseUri = "https://api.github.com/repos/dotnet/extensions";
$baseLabelUri = "https://github.com/dotnet/extensions";

function Format-Avatar {
    [CmdletBinding()]
    Param(
        $user
    )

    return "<img src='$($user.avatar_url)' class='avatar avatar-user mb-1 mr-1 rgh-small-user-avatars' width='16' height='16' loading='lazy' style='margin-left: 1px;'> <a class='user-mention notranslate' href='https://github.com/$($user.login)'>@$($user.login)</a>";
}

function Format-IssueByArea {
    [CmdletBinding()]
    Param(
        $issues,
        [string] $columnHeader
    )

    if (!$issues) {
        return '';
    }

    $issueLinks = @();
    $issueLinks += '<table><thead><tr>';
    $issueLinks += "<th>$columnHeader</th>";
    $issueLinks += "<th>Open for (days)</th>";
    $issueLinks += "<th>Assignee</th>";
    $issueLinks += "</tr></thead><tbody>";

    $issues | Sort-Object created_at | ForEach-Object {
        $issue = $_;
        if ($issue.html_url.Contains('/pull/')) {
            return;
        }

        $staleDays = Get-IssueStaleDays -issue $issue;
        $assignees = Get-IssueAssignees -issue $issue;
        $author = Format-Avatar -user $issue.user;

        $issueLinks += '<tr>';
        $issueLinks += "<td class='col-700'><strong><a href='$($issue.html_url)'>#$($issue.number) $($issue.title)</strong></a><br />by $author on $($issue.created_at.ToString("d MMM yyyy"))</td>"
        $issueLinks += "<td class='col-70' align='right'><strong>$staleDays</strong></td>";
        $issueLinks += "<td class='col-150'>$assignees</td>";
        $issueLinks += "</tr>";
    }

    $issueLinks += "</tbody></table><br />";

    return $issueLinks;
}

function Format-IssueLabel {
    [CmdletBinding()]
    Param(
        $label
    )

    $color = $label.color;
        $r = [Convert]::ToInt32($color.Substring(0, 2), 16)
        $g = [Convert]::ToInt32($color.Substring(2, 2), 16)
        $b = [Convert]::ToInt32($color.Substring(4, 2), 16)

    return "<a href='$baseLabelUri/labels/$([uri]::EscapeDataString($label.name))' style='--label-r:$r;--label-g:$g;--label-b:$b;--label-h:38;--label-s:92;--label-l:50;' class='IssueLabel hx_IssueLabel '>$($label.name)</a>";
}

function Format-UntriagedIssue {
    [CmdletBinding()]
    Param(
        $issues,
        [string] $columnHeader
    )

    $issueLinks = @();
    $issueLinks += '<table><thead><tr>';
    $issueLinks += "<th>$columnHeader</th>";
    $issueLinks += "<th>Open for (days)</th>";
    $issueLinks += "<th>Assignee</th>";
    $issueLinks += "<th>Labels</th>";
    $issueLinks += "</tr></thead><tbody>";

    $issues | Sort-Object created_at | ForEach-Object {
        $issue = $_;
        if ($issue.html_url.Contains('/pull/')) {
            return;
        }

        $issueLabels = Get-IssueLabels -issue $issue;
        $staleDays = Get-IssueStaleDays -issue $issue;
        $assignees = Get-IssueAssignees -issue $issue;
        $author = Format-Avatar -user $issue.user;

        $issueLinks += '<tr>';
        $issueLinks += "<td class='col-700'><strong><a href='$($issue.html_url)'>#$($issue.number) $($issue.title)</strong></a><br />by $author on $($issue.created_at.ToString("d MMM yyyy"))</td>"
        $issueLinks += "<td class='col-70' align='right'><strong>$staleDays</strong></td>";
        $issueLinks += "<td class='col-150'>$assignees</td>";
        $issueLinks += "<td>$issueLabels</td>";
        $issueLinks += "</tr>";
    }

    $issueLinks += "</tbody></table><br />";

    return $issueLinks;
}

function Get-AreaLabels {
    [CmdletBinding()]
    Param(
    )

    $labels = @();
    $nextPattern = "(?<=<)([\S]*)(?=>; rel=`"next`")";

    $headers = @{
        Authorization = "token $ghToken"
    }
    $url = "$baseUri/labels?page=1&per_page=100";
    Write-Verbose "Next URL: $url"
    do {
        $response = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -ResponseHeadersVariable responseHeaders #-Verbose
        $labels += $response;

        $url = $null;

        # See https://docs.github.com/rest/using-the-rest-api/using-pagination-in-the-rest-api#using-link-headers
        $linkHeader = $responseHeaders["link"];
        if ($linkHeader -and ($linkHeader -match $nextPattern) -eq $true) {
            $url = $Matches[0];
            Write-Verbose "Next URL: $url"
        }
    } while ($url)

    $areaLabels = @();
    $labels | Sort-Object created_at | ForEach-Object {
        $label = $_;
        if ($label.name.StartsWith('area-')) {
            $areaLabels += $label;
        }
    }

    return $areaLabels | Sort-Object name;
}

function Get-Discussions {
    [CmdletBinding()]
    Param(
        [string] $labels
    )

    $discussions = @();
    $nextPattern = "(?<=<)([\S]*)(?=>; rel=`"next`")";

    $headers = @{
        Authorization = "token $ghToken"
    }
    $url = "$baseUri/discussions?page=1&per_page=500"
    Write-Verbose "Next URL: $url"
    do {
        $response = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -ResponseHeadersVariable responseHeaders #-Verbose

        $response | ForEach-Object {
            $discussion = $_;
            if ($discussion.state -ne 'open') {
                return;
            }

            $discussion.labels | ForEach-Object {
                if ($_.name -eq $labels) {
                    $discussions += $discussion;
                }
            }
        }

        $url = $null;

        # See https://docs.github.com/rest/using-the-rest-api/using-pagination-in-the-rest-api#using-link-headers
        $linkHeader = $responseHeaders["link"];
        if ($linkHeader -and ($linkHeader -match $nextPattern) -eq $true) {
            $url = $Matches[0];
            Write-Verbose "Next URL: $url"
        }
    } while ($url)

    return $discussions;
}

function Get-Issues {
    [CmdletBinding()]
    Param(
        [string] $labels,
        [bool] $noMilestone = $false
    )

    $issues = @();
    $nextPattern = "(?<=<)([\S]*)(?=>; rel=`"next`")";

    $headers = @{
        Authorization = "token $ghToken"
    }
    $urlSuffix = if ($noMilestone) { '&milestone=none' } else { '' }
    $url = "$baseUri/issues?page=1&per_page=100&labels=$labels&state=open$urlSuffix"
    Write-Verbose "Next URL: $url"
    do {
        $response = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -ResponseHeadersVariable responseHeaders #-Verbose
        $issues += $response;

        $url = $null;

        # See https://docs.github.com/rest/using-the-rest-api/using-pagination-in-the-rest-api#using-link-headers
        $linkHeader = $responseHeaders["link"];
        if ($linkHeader -and ($linkHeader -match $nextPattern) -eq $true) {
            $url = $Matches[0];
            Write-Verbose "Next URL: $url"
        }
    } while ($url)

    return $issues;
}

function Get-IssuePerArea {
    [CmdletBinding()]
    Param(
    )

    $issues = @();

    $areaLabels = Get-AreaLabels
    $areaLabels | ForEach-Object {
        $areaLabel = $_.name;

        $header = "Issues for $(Format-IssueLabel -label $_)";
        $issuesPerLabel = Get-Issues -labels $areaLabel -noMilestone $true;
        $issues += (Format-IssueByArea -issues $issuesPerLabel -columnHeader $header);
        $issues += '';
    }

    return $issues;
}


function Get-IssueAssignees {
    [CmdletBinding()]
    Param(
        $issue
    )

    $assignees = '';
    $issue.assignees | ForEach-Object {
        if ($_ -eq $null) {
            return;
        }

        $login = Format-Avatar -user $_;
        $assignees += " <div><strong>$login</strong></div>";
    }

    return $assignees;
}

function Get-IssueLabels {
    [CmdletBinding()]
    Param(
        $issue
    )

    $issueLabels = '';
    $issue.labels | ForEach-Object {
        $labelName = Format-IssueLabel -label $_
        $issueLabels += " $labelName";
    }

    return $issueLabels;
}

function Get-IssueStaleDays {
    [CmdletBinding()]
    Param(
        $issue
    )

    $staleDays = (New-TimeSpan -Start $issue.created_at -End $(Get-Date)).Days;

    if ($staleDays -gt 28) {
        $staleDays = "<g-emoji class='g-emoji'>❗</g-emoji> $staleDays"
    }
    elseif ($staleDays -gt 14) {
        $staleDays = "<g-emoji class='g-emoji'>⚠️</g-emoji> $staleDays";
    }

    return $staleDays;
}

Push-Location $PSScriptRoot

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    # A list of comma separated label names. Example: bug,ui,@high
    # See https://docs.github.com/rest/issues/issues
    $untriaged = @();
    $untriaged += (Get-Issues -labels 'untriaged');
    $untriaged += (Get-Discussions -labels 'untriaged');
    $untriagedIssues = Format-UntriagedIssue -issues $untriaged -columnHeader 'Untriaged issues'

    # A list of comma separated label names. Example: bug,ui,@high
    # See https://docs.github.com/rest/issues/issues
    $issuesPerArea = Get-IssuePerArea

    $template = Get-Content 'repo-digest-template.html';
    $template = $template.Replace('##ISSUES-UNTRIAGED##', $untriagedIssues);
    $template = $template.Replace('##ISSUES-BY-AREA##', $issuesPerArea);
    $template = $template.Replace('##DATE##', $((Get-Date).ToString("d MMM yyyy")));
    $template | Out-File 'repo-digest.html' -Encoding utf8
}
catch {
    Write-Error $_;
    Exit -1;
}
finally {
    Pop-Location
}
