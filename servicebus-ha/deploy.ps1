# This script deploys dual (east-west) Service Bus Premium namespaces, and pairs them
param (
    [Parameter(Mandatory = $true)] [ValidateSet('Deploy', 'Teardown', 'Failover', 'Failback', 'Status')] [string] $Mode
)

$ErrorActionPreference = 'Stop'

$primaryLocation = 'australiaeast'
$priLoc = 'aue'
$secondaryLocation = 'australiasoutheast'
$secLoc = 'ase'
$rg = 'servicebusha-rg'
$tags = 'project=servicebus-ha'
$primaryNamespace = "servicebusha-$priLoc"
$secondaryNamespace = "servicebusha-$secLoc"
$alias = 'servicebusha'
$queue = 'queue101'

switch ($Mode) {
    'Teardown' {
        # TEAR DOWN
        
        # Break pairing
        az servicebus georecovery-alias break-pair -g $rg --namespace-name $primaryNamespace --alias $alias
        
        # Delete RG
        az group delete -n $rg --yes
    
    }
    'Deploy' {
        # DEPLOY

        Write-Host "create resource group" -ForegroundColor Cyan
        az group create -n $rg --location $primaryLocation --tags $tags

        Write-Host "create $primaryLocation namespace" -ForegroundColor Cyan
        az servicebus namespace create -g $rg -n $primaryNamespace --location $primaryLocation --tags $tags --sku Premium --capacity 1
        az servicebus queue create -g $rg --namespace-name $primaryNamespace -n $queue

        Write-Host "create $secondaryLocation namespace" -ForegroundColor Cyan
        az servicebus namespace create -g $rg -n $secondaryNamespace --location $secondaryLocation --tags $tags --sku Premium --capacity 1

        Write-Host "set alias and create pair" -ForegroundColor Cyan
        az servicebus georecovery-alias set -g $rg --namespace-name $primaryNamespace --alias $alias --partner-namespace $secondaryNamespace
    }
    'Failover' {
        az servicebus georecovery-alias fail-over -g $rg --alias $alias --namespace-name $secondaryNamespace
    }
    'Failback' {
        if ((az servicebus georecovery-alias show -g $rg --alias $alias --namespace-name $primaryNamespace | ConvertFrom-Json)) {
            throw "$primaryLocation namespace is paired. cannot failback to a paired namespace"
        }

        Write-Host "clear out $primaryLocation namespace" -ForegroundColor Cyan
        $primaryQueueStatus = ( az servicebus queue show -g $rg --namespace-name $primaryNamespace -n $queue | ConvertFrom-Json )
        if ($primaryQueueStatus.countDetails.activeMessageCount -gt 0) { throw "$primaryLocation $queue is not empty" }
        az servicebus queue delete -g $rg --namespace-name $primaryNamespace -n $queue

        Write-Host "set alias and pairing $secondaryLocation -> $primaryLocation" -ForegroundColor Cyan
        az servicebus georecovery-alias set -g $rg --namespace-name $secondaryNamespace --alias $alias --partner-namespace $primaryNamespace
        if (!$?) {throw}

        Write-Host "wait for alias provisioning succeeded and replicated" -ForegroundColor Cyan
        while ($true) {
            $primaryStatus = (az servicebus georecovery-alias show -g $rg --alias $alias --namespace-name $primaryNamespace | ConvertFrom-Json)
            $secondaryStatus = (az servicebus georecovery-alias show -g $rg --alias $alias --namespace-name $secondaryNamespace | ConvertFrom-Json)
            
            $primaryStatus
            $secondaryStatus

            if ($primaryStatus.provisioningState -eq 'Succeeded' `
                -and $secondaryStatus.provisioningState -eq 'Succeeded' `
                -and $secondaryStatus.pendingReplicationOperationsCount -eq 0) {
                break
            }

            Write-Host "waiting 10 seconds" -ForegroundColor Cyan
            Start-Sleep -Seconds 10
        }

        Write-Host "failover to $primaryLocation" -ForegroundColor Cyan
        az servicebus georecovery-alias fail-over -g $rg --alias $alias --namespace-name $primaryNamespace
        if (!$?) {throw}

        Write-Host "wait for $secondaryLocation namespace to un-pair" -ForegroundColor Cyan
        while ($true) {
            $secondaryStatus = (az servicebus georecovery-alias show -g $rg --alias $alias --namespace-name $secondaryNamespace | ConvertFrom-Json)            
            if (!$secondaryStatus) { break }
            $secondaryStatus

            Write-Host "waiting 10 seconds" -ForegroundColor Cyan
            Start-Sleep -Seconds 10
        }

        # Wait 60 seconds for DNS TTL
        Write-Host "waiting 60 seconds for DNS TTL" -ForegroundColor Cyan
        Start-Sleep -Seconds 60

        # Already paired guard
        if ((az servicebus georecovery-alias show -g $rg --alias $alias --namespace-name $secondaryNamespace | ConvertFrom-Json)) {
            throw "$secondaryLocation namespace is paired. cannot re-pair to an already paired namespace"
        }

        Write-Host "clear out $secondaryLocation namespace" -ForegroundColor Cyan
        $secondaryQueueStatus = ( az servicebus queue show -g $rg --namespace-name $secondaryNamespace -n $queue | ConvertFrom-Json )
        if ($secondaryQueueStatus.countDetails.activeMessageCount -gt 0) { throw "$secondaryLocation $queue is not empty" }        
        az servicebus queue delete -g $rg --namespace-name $secondaryNamespace -n $queue

        Write-Host "set alias and pairing $primaryLocation -> $secondaryLocation" -ForegroundColor Cyan
        az servicebus georecovery-alias set -g $rg --namespace-name $primaryNamespace --alias $alias --partner-namespace $secondaryNamespace
    }
    'Status' {
        Write-Host "$primaryLocation" -ForegroundColor Cyan
        az servicebus namespace show -g $rg -n $primaryNamespace
        az servicebus queue show -g $rg --namespace-name $primaryNamespace -n $queue
        $primaryStatus = ( az servicebus georecovery-alias show -g $rg --alias $alias --namespace-name $primaryNamespace | ConvertFrom-Json )
        $primaryStatus

        Write-Host "$secondaryLocation" -ForegroundColor Cyan
        az servicebus namespace show -g $rg -n $secondaryNamespace
        az servicebus queue show -g $rg --namespace-name $secondaryNamespace -n $queue
        $secondaryStatus = ( az servicebus georecovery-alias show -g $rg --alias $alias --namespace-name $secondaryNamespace | ConvertFrom-Json )
        $secondaryStatus

        if ($primaryStatus -ne $null) {
            Write-Host "$primaryLocation is $($primaryStatus.role). pairing status $($primaryStatus.provisioningState)" -ForegroundColor Cyan
        }

        if ($secondaryStatus -ne $null) {
            Write-Host "$secondaryLocation is $($secondaryStatus.role). pairing status $($secondaryStatus.provisioningState)" -ForegroundColor Cyan
        }
    }
}
