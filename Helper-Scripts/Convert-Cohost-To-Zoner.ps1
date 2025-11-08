$json = cat post.json | ConvertFrom-Json;
$date = ([datetime]$json.publishedAt).ToString('yyyy-MM-dd');

$content = "";

$json.tags | % { 
    $content += "<meta property=`"article:tag`" content=`"$_`"/>`n";
}

$content += "`n*This post was originally made on [Cohost]($($json.singlePostPageUrl)).*`n`n";

if ($json.cws.length -gt 0) {
    $content += "<details><summary>CW: " + (@($json.cws) -join ", ") + ".</summary>`n`n";
}

$json.blocks | % {
    if ($_.type -eq "attachment") {
        $path = Split-Path -Path $_.attachment.fileURL -Leaf;
        $content += "<img src=`"./images/$path`" alt=`"$($_.attachment.altText)`">`n`n"

    } elseif ($_.type -eq "markdown") {
        $content += $_.markdown.content + "`n`n";
    } elseif ($_.type -eq "ask") {
        $asker = "Anonymous";
        if ($_.ask.anon -eq $false) {
            $asker = $_.ask.askingProject.displayName;
        }
        $readDate = ([datetime]$_.ask.sentAt).ToString('d MMM yyyy');
        $content += "<figure class=`"ask`"><blockquote>$($_.ask.content)</blockquote><figcaption>&mdash; $asker at <time datetime=`"$($_.ask.sentAt)`">$readDate</time></figcaption></figure>`n`n"
    }
}

if ($json.cws.length -gt 0) {
    $content += "</details>`n`n";
}

$headline = $json.headline -replace "\?","&#63;"

$content > "./$($date)-$($json.postId)_$headline.md"