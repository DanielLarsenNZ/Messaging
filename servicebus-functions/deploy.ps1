$location = 'australiaeast'
$loc = 'aue'
$rg = 'hellomessaging-rg'
$tags = 'project=hello-messaging'
$servicebusNamespace = 'pipeline-bus'
$servicebusAuthRule = 'SenderReceiver1'
$servicebusSku = 'Standard'
$sessionQueues = 'queue3-session', 'queue4-session'
$webjobsStorage = "hellomessaging$loc"

# Create Resource Group
Write-Host "az group create -n $rg" -ForegroundColor Yellow
az group create -n $rg --location $location --tags $tags


# SERVICE BUS
# https://docs.microsoft.com/en-us/cli/azure/servicebus/namespace?view=azure-cli-latest#az-servicebus-namespace-create

# Create namespace, queue and auth rule
Write-Host "az servicebus namespace create --name $servicebusNamespace" -ForegroundColor Yellow
az servicebus namespace create -g $rg --name $servicebusNamespace --location $location --tags $tags --sku $servicebusSku

Write-Host "az servicebus namespace authorization-rule create --name $servicebusAuthRule" -ForegroundColor Yellow
az servicebus namespace authorization-rule create -g $rg --namespace-name $servicebusNamespace --name $servicebusAuthRule --rights Listen Send

foreach ($queue in $sessionQueues) {
    Write-Host "az servicebus queue create --name $queue" -ForegroundColor Yellow
    az servicebus queue create -g $rg --namespace-name $servicebusNamespace --name $queue --enable-session true     #--default-message-time-to-live 'P14D'
}

# Get connection string
Write-Host "az servicebus namespace authorization-rule keys list --name $servicebusAuthRule" -ForegroundColor Yellow
$servicebusConnectionString = ( az servicebus namespace authorization-rule keys list -g $rg --namespace-name $servicebusNamespace --name $servicebusAuthRule | ConvertFrom-Json ).primaryConnectionString
$servicebusConnectionString

Write-Host "az storage account create -n $webjobsStorage" -ForegroundColor Yellow
az storage account create -n $webjobsStorage -g $rg -l $location --tags $tags --sku Standard_LRS

Write-Host "az storage account show-connection-string -n $webjobsStorage" -ForegroundColor Yellow
$webjobsStorageConnection = ( az storage account show-connection-string -g $rg -n $webjobsStorage | ConvertFrom-Json ).connectionString
$webjobsStorageConnection
# Tear down
# az group delete -n $rg --yes
