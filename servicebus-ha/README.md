# High Availability with Azure Service Bus

## Getting started

Deploy the Service Bus namespaces and alias

    ./deploy.ps1 -Mode Deploy

Make a `local.settings.json` file

    cd servicebus-functions/Examples.Pipeline.ServiceBusFunctions
    copy local.settings.template.json local.settings.json

Copy the Service Bus Alias Connection String into `local.settings.json` and start the function.

    func start

Perform failover / failback

    cd servicebus-ha
    ./deploy.ps1 -Mode Failover
    ./deploy.ps1 -Mode Status 	# Wait for pairing status = Succeeded
    ./deploy.ps1 -Mode Failback
    ./deploy.ps1 -Mode Status	# Wait for pairing status = Succeeded

You can failover and failback as many times as you like. Note that the DNS TTL on the alias appears to be about 60 seconds, so wait at least that long between Failover / Failback to avoid an interruption to clients.

## Outage vs Disaster

<https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-geo-dr#outages-and-disasters><br/>
An outage is the temporary unavailability of Azure Service Bus, and can affect some components of the
service, such as a messaging store, or even the entire datacenter. However, after the problem is fixed, 
Service Bus becomes available again. Typically, an outage does not cause the loss of messages or other 
data. An example of such an outage might be a power failure in the datacenter. Some outages are only 
short connection losses due to transient or network issues.

## Premium DR

<https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-geo-dr#basic-concepts-and-terms><br/>
_Metadata entities such as queues, topics, and subscriptions; and their properties of the service that 
are associated with the namespace. Note that only entities and their settings are replicated automatically. 
**Messages are not replicated**._

## Client side queue

<https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-outages-disasters#protecting-against-outages-and-disasters---service-bus-standard><br/>
If the application does not require permanent sender-to-receiver communication, the application can 
implement a durable client-side queue to prevent message loss and to shield the sender from any 
transient Service Bus errors.

## Auto-forward

<https://github.com/Azure/azure-service-bus/tree/master/samples/DotNet/Microsoft.ServiceBus.Messaging/AutoForward>

## Duplicate detection

<https://github.com/Azure/azure-service-bus/tree/master/samples/DotNet/Microsoft.ServiceBus.Messaging/DuplicateDetection>

## Geo-replication

<https://github.com/Azure/azure-service-bus/tree/master/samples/DotNet/Microsoft.ServiceBus.Messaging/GeoReplication>

## References

Best practices for insulating applications against Service Bus outages and disasters: <https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-outages-disasters>

Azure Service Bus Geo-disaster recovery: <https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-geo-dr>

<https://twitter.com/clemensv/status/1182280928867098626>

<https://rajbos.github.io/blog/2019/07/12/Azure-CLI-PowerShell>