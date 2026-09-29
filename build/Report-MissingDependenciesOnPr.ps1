# Posts a resolvable inline PR review comment for each Uno.* dependency that a PR references but
# that is not yet published to nuget.org. Non-blocking by design: the pre-build "report" pass
# collects the missing (dev) dependencies, and this script surfaces them as review-thread comments
# (which carry the "Resolve conversation" button) instead of failing the PR. The build/tests are
# unaffected — they restore from the internal feed. The hard gate lives only on the publish paths.
#
# Idempotent: a hidden marker per (version, line) means re-runs on the same PR do not duplicate a
# comment. Never throws — any API hiccup is logged and skipped so the job stays green.
#
# Requires GH_TOKEN / GITHUB_TOKEN in the environment (the workflow provides the job's GITHUB_TOKEN
# with `pull-requests: write`).

param(
	[Parameter(Mandatory = $true)]
	[string]$Repository,          # owner/repo, e.g. unoplatform/uno.templates
	[Parameter(Mandatory = $true)]
	[int]$PullNumber,
	[Parameter(Mandatory = $true)]
	[string]$HeadSha,             # PR head commit the review comment is anchored to
	[Parameter(Mandatory = $true)]
	[string]$ReportPath,          # JSON produced by Verify-NuGetDependenciesOnNuGetOrg.ps1 -ReportOnly
	[string]$ManifestPath = "src/Uno.Sdk/packages.json",
	# Path used in the review comment; must be the repo-relative path with forward slashes.
	[string]$CommentPath = "src/Uno.Sdk/packages.json",
	# Log the intended API calls instead of posting (local validation).
	[switch]$DryRun
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ReportPath)) {
	Write-Host "No report file at '$ReportPath'; nothing to comment."
	return
}

$missing = @(Get-Content -Path $ReportPath -Raw | ConvertFrom-Json)
if ($missing.Count -eq 0) {
	Write-Host "No missing dependencies reported; nothing to comment."
	return
}

if (-not (Test-Path $ManifestPath)) {
	Write-Warning "Manifest '$ManifestPath' not found; cannot anchor comments."
	return
}

# Fetch existing review comments once for idempotency (match on the hidden marker in the body).
$existingBodies = @()
try {
	$existingBodies = @(gh api --paginate "repos/$Repository/pulls/$PullNumber/comments" --jq '.[].body')
}
catch {
	Write-Warning "Could not list existing review comments: $($_.Exception.Message)"
}

# Group the missing coordinates by version, so one comment per manifest line covers every package
# that shares that (missing) version instead of one comment per package id.
$byVersion = $missing | Group-Object -Property Version

foreach ($group in $byVersion) {
	$version = [string]$group.Name
	$ids = @($group.Group | ForEach-Object { [string]$_.Id } | Sort-Object -Unique)

	# Anchor on the manifest line(s) that carry this version, e.g. `"version": "7.1.0-dev.1",`.
	$pattern = '"version"\s*:\s*"' + [regex]::Escape($version) + '"'
	$versionLineMatches = @(Select-String -Path $ManifestPath -Pattern $pattern)
	if ($versionLineMatches.Count -eq 0) {
		# Fallback: any line mentioning the version string (e.g. a versionOverride).
		$versionLineMatches = @(Select-String -Path $ManifestPath -Pattern ([regex]::Escape($version)))
	}

	if ($versionLineMatches.Count -eq 0) {
		Write-Warning "Could not locate a manifest line for version '$version'; skipping inline comment for: $($ids -join ', ')."
		continue
	}

	foreach ($versionMatch in $versionLineMatches) {
		$line = [int]$versionMatch.LineNumber
		$marker = "<!-- nuget-dep-report:v=${version}:L$line -->"

		if (($existingBodies | Where-Object { $_ -and $_.Contains($marker) }).Count -gt 0) {
			Write-Host "Comment already present for version '$version' at line $line; skipping."
			continue
		}

		$idList = ($ids | ForEach-Object { "``$_``" }) -join ', '
		$body = @"
$marker
⚠️ **Dependency not yet on nuget.org**

$idList at version ``$version`` is referenced here but is **not published to nuget.org** yet.

This does **not** block this PR — the build restores from the internal feed. The version must be public before the **dev/prod publish** gate will pass. **Resolve this conversation** once it is published (or if it is intentionally internal-only).
"@

		if ($DryRun) {
			Write-Host "[DryRun] Would post inline comment -> path='$CommentPath' line=$line commit=$HeadSha version='$version' ids=($($ids -join ', '))"
			Write-Host "[DryRun] marker: $marker"
			continue
		}

		try {
			gh api --method POST "repos/$Repository/pulls/$PullNumber/comments" `
				-f body="$body" `
				-f commit_id="$HeadSha" `
				-f path="$CommentPath" `
				-F line=$line `
				-f side="RIGHT" | Out-Null
			Write-Host "Posted inline comment for version '$version' at line $line ($($ids -join ', '))."
		}
		catch {
			# A 422 usually means the line is not part of the PR diff; log and continue (non-blocking).
			Write-Warning "Could not post inline comment for version '$version' at line ${line}: $($_.Exception.Message)"
		}
	}
}
